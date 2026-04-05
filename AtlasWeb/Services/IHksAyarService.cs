using AtlasWeb.DTOs;

namespace AtlasWeb.Services;

public sealed class HksCredentialSet
{
    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ServicePassword { get; init; } = string.Empty;
}

public interface IHksAyarService
{
    Task<HksAyarDto> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<HksAyarDto> GetCurrentTenantSettingsAsync(CancellationToken cancellationToken = default);

    Task<HksAyarDto> SaveCurrentTenantSettingsAsync(
        HksAyarKaydetDto request,
        CancellationToken cancellationToken = default);

    Task<HksCredentialSet> GetCurrentTenantCredentialsAsync(CancellationToken cancellationToken = default);

    Task<HksCredentialSet> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
