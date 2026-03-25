using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AtlasWeb.Services;

namespace AtlasWeb.Data
{
    /// <summary>
    /// EF Core CLI (dotnet ef migrations) için tasarım zamanı DbContext; HTTP olmadan derlenir.
    /// </summary>
    public class AtlasDbContextFactory : IDesignTimeDbContextFactory<AtlasDbContext>
    {
        public AtlasDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AtlasDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5432;Database=atlasweb_db;Username=postgres;Password=",
                    n => n.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options;

            return new AtlasDbContext(options, new DesignTimeCurrentUserService());
        }

        private sealed class DesignTimeCurrentUserService : ICurrentUserService
        {
            public Guid? MusteriId => null;
            public string? EPosta => "design-time";
            public bool IsAdmin => true;
        }
    }
}
