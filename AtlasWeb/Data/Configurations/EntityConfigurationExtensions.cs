using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AtlasWeb.Models;

namespace AtlasWeb.Data.Configurations
{
    public static class EntityConfigurationExtensions
    {
        public static void ConfigureSaaSEntity<T>(this EntityTypeBuilder<T> builder)
            where T : class, ITenantEntity, ISoftDelete, IAuditEntity
        {
            builder.HasIndex(x => new { x.MusteriId, x.AktifMi, x.KayitTarihi })
                   .HasDatabaseName($"IX_{typeof(T).Name}_SaaS_Performance");
        }
    }
}