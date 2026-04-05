using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksIlceService
{
    Task<IReadOnlyList<HksIlceKayitDto>> GetCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksIlceKayitDto>> SyncCurrentTenantDistrictsAsync(int? ilId = null, CancellationToken cancellationToken = default);

    Task<HksIlceTopluSenkronSonucDto> SyncDistrictsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default);
}
