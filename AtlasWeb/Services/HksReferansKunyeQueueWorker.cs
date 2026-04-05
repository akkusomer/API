using System.Text.Json;
using AtlasWeb.Data;
using AtlasWeb.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Services;

public sealed class HksReferansKunyeQueueWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HksReferansKunyeQueueWorker> _logger;

    public HksReferansKunyeQueueWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HksReferansKunyeQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HKS queue worker beklenmeyen hata aldi.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        var hksService = scope.ServiceProvider.GetRequiredService<IHksService>();

        var job = await dbContext.HksReferansKunyeKayitlari
            .IgnoreQueryFilters()
            .Where(x => x.AktifMi
                && (x.Durum == HksReferansKunyeDurum.Kuyrukta || x.Durum == HksReferansKunyeDurum.Isleniyor))
            .OrderBy(x => x.GuncellemeTarihi ?? x.KayitTarihi)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return false;
        }

        job.Durum = HksReferansKunyeDurum.Isleniyor;
        job.ProgressPercent = Math.Max(job.ProgressPercent, 1);
        job.ProgressLabel = "HKS sorgusu baslatildi";
        job.Hata = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var lastPersistedPercent = job.ProgressPercent;
            var lastPersistedAt = DateTime.UtcNow;

            var result = await hksService.GetReferansKunyelerForTenantAsync(
                job.MusteriId,
                new HksReferansKunyeRequestDto
                {
                    BaslangicTarihi = job.BaslangicTarihi,
                    BitisTarihi = job.BitisTarihi,
                    KalanMiktariSifirdanBuyukOlanlar = true
                },
                async progress =>
                {
                    var shouldPersist =
                        progress.ProgressPercent >= 100
                        || progress.ProgressPercent >= lastPersistedPercent + 2
                        || DateTime.UtcNow - lastPersistedAt >= TimeSpan.FromSeconds(2);

                    if (!shouldPersist)
                    {
                        return;
                    }

                    lastPersistedPercent = progress.ProgressPercent;
                    lastPersistedAt = DateTime.UtcNow;
                    job.ProgressPercent = Math.Clamp(progress.ProgressPercent, 1, 99);
                    job.ProgressLabel = progress.Label;
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);

            job.IslemKodu = result.IslemKodu;
            job.Mesaj = result.Mesaj;
            job.KayitSayisi = result.ReferansKunyeler.Count;
            job.ReferansKunyelerJson = JsonSerializer.Serialize(result.ReferansKunyeler, JsonOptions);
            job.Durum = HksReferansKunyeDurum.Tamamlandi;
            job.ProgressPercent = 100;
            job.ProgressLabel = "HKS sorgusu tamamlandi";
            job.Hata = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HksIntegrationException ex)
        {
            job.Durum = HksReferansKunyeDurum.Hatali;
            job.ProgressLabel = "HKS sorgusu basarisiz oldu";
            job.Hata = ex.Message;
            job.IslemKodu = ex.IslemKodu;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HKS queue worker sorgu islerken hata aldi.");
            job.Durum = HksReferansKunyeDurum.Hatali;
            job.ProgressLabel = "HKS sorgusu basarisiz oldu";
            job.Hata = ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
