using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksUretimSekliService
{
    Task<IReadOnlyList<HksUretimSekliKayitDto>> GetCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksUretimSekliKayitDto>> SyncCurrentTenantProductionShapesAsync(CancellationToken cancellationToken = default);
}
