using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksUrunCinsiService
{
    Task<IReadOnlyList<HksUrunCinsiKayitDto>> GetCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksUrunCinsiKayitDto>> SyncCurrentTenantProductKindsAsync(int? urunId = null, CancellationToken cancellationToken = default);
}
