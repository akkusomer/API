using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksIsletmeTuruService
{
    Task<IReadOnlyList<HksIsletmeTuruKayitDto>> GetCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HksIsletmeTuruKayitDto>> SyncCurrentTenantBusinessTypesAsync(CancellationToken cancellationToken = default);
}
