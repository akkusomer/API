using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksService
{
    Task<IReadOnlyList<HksSelectOptionDto>> GetSifatlarAsync(CancellationToken cancellationToken = default);

    Task<HksKayitliKisiSorguDto?> GetKayitliKisiSorguAsync(
        string tcKimlikVergiNo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksHalIciIsyeriDto>> GetHalIciIsyerleriAsync(
        string tcKimlikVergiNo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HksHalIciIsyeriDto>>([]);

    Task<IReadOnlyList<HksSelectOptionDto>> GetIllerAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetIllerForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerAsync(int ilId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetIlcelerForTenantAsync(Guid tenantId, int ilId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerAsync(int ilceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetBeldelerForTenantAsync(Guid tenantId, int ilceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetUrunlerAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetUrunBirimleriAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetIsletmeTurleriAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetUretimSekilleriAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksUrunCinsiDto>> GetUrunCinsleriAsync(int urunId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSelectOptionDto>> GetBildirimTurleriAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

    Task<IReadOnlyList<HksSelectOptionDto>> GetBelgeTipleriAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

    Task<IReadOnlyList<HksSelectOptionDto>> GetMalinNitelikleriAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<HksSelectOptionDto>>([]);

    Task<HksBildirimKayitResponseDto> SaveBildirimKayitAsync(
        IReadOnlyList<HksBildirimKayitItemDto> request,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    Task<HksReferansKunyeResponseDto> GetReferansKunyelerAsync(
        HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HksReferansKunyeResponseDto> GetReferansKunyelerForTenantAsync(
        Guid tenantId,
        HksReferansKunyeRequestDto request,
        Func<HksSearchProgressDto, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
