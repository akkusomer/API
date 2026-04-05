using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksUrunService
{
    Task<IReadOnlyList<HksUrunKayitDto>> GetCurrentTenantProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksUrunKayitDto>> SyncCurrentTenantProductsAsync(CancellationToken cancellationToken = default);
}
