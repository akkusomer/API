using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksIlService
{
    Task<IReadOnlyList<HksIlKayitDto>> GetCurrentTenantCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksIlKayitDto>> SyncCurrentTenantCitiesAsync(CancellationToken cancellationToken = default);

    Task<HksIlTopluSenkronSonucDto> SyncCitiesForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default);
}
