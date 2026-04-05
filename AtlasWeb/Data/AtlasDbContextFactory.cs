using AtlasWeb.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace AtlasWeb.Data
{
    public class AtlasDbContextFactory : IDesignTimeDbContextFactory<AtlasDbContext>
    {
        public AtlasDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? BuildFallbackConnectionString();

            var options = new DbContextOptionsBuilder<AtlasDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options;

            return new AtlasDbContext(options, new DesignTimeCurrentUserService());
        }

        private static string BuildFallbackConnectionString()
        {
            var password = Environment.GetEnvironmentVariable("ATLASWEB_DB_PASSWORD")
                ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? "placeholder";

            return new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Database = "atlasweb_db",
                Username = "postgres",
                Password = password
            }.ConnectionString;
        }

        private sealed class DesignTimeCurrentUserService : ICurrentUserService
        {
            public Guid? MusteriId => AtlasDbContext.SystemMusteriId;
            public string? EPosta => "design-time";
            public bool IsAdmin => true;
            public bool IsSystemAdmin => true;
        }
    }
}
