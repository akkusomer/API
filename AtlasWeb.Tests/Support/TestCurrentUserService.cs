using AtlasWeb.Data;
using AtlasWeb.Services;

namespace AtlasWeb.Tests.Support;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? MusteriId { get; set; } = AtlasDbContext.SystemMusteriId;
    public string? EPosta { get; set; } = "tests@atlasweb.local";
    public bool IsAdmin { get; set; }
    public bool IsSystemAdmin => IsAdmin && MusteriId == AtlasDbContext.SystemMusteriId;
}
