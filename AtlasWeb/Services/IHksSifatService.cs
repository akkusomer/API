using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksSifatService
{
    Task<IReadOnlyList<HksSifatKayitDto>> GetCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksSifatKayitDto>> SyncCurrentTenantSifatlarAsync(CancellationToken cancellationToken = default);
}
