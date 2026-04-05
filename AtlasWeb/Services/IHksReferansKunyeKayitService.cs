using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public interface IHksReferansKunyeKayitService
{
    Task<HksReferansKunyeKayitDto?> GetCurrentTenantSnapshotAsync(CancellationToken cancellationToken = default);

    Task<HksReferansKunyeKayitDto> QueueCurrentTenantSearchAsync(
        HksReferansKunyeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<HksReferansKunyeKayitDto> SaveCurrentTenantSnapshotAsync(
        HksReferansKunyeKaydetDto request,
        CancellationToken cancellationToken = default);
}
