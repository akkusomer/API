using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksUrunBirimService
{
    Task<IReadOnlyList<HksUrunBirimKayitDto>> GetCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksUrunBirimKayitDto>> SyncCurrentTenantProductUnitsAsync(CancellationToken cancellationToken = default);
}
