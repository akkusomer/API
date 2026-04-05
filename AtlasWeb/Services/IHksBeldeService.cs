using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksBeldeService
{
    Task<IReadOnlyList<HksBeldeKayitDto>> GetCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksBeldeKayitDto>> SyncCurrentTenantTownsAsync(int? ilceId = null, CancellationToken cancellationToken = default);

    Task<HksBeldeTopluSenkronSonucDto> SyncTownsForAllTenantsAsync(Guid sourceTenantId, CancellationToken cancellationToken = default);
}
