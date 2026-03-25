using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AtlasWeb.Models;

namespace AtlasWeb.Data
{
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// SaaS tabloları için bileşik indeks (MusteriId, AktifMi, KayitTarihi).
        /// </summary>
        /// <typeparam name="T">ITenantEntity, ISoftDelete ve IAuditEntity uygulayan sınıflar</typeparam>
        public static void ConfigureSaaSEntity<T>(this EntityTypeBuilder<T> builder)
            where T : class, ITenantEntity, ISoftDelete, IAuditEntity // 🛡️ KRİTİK: KayitTarihi hatasını bu satır çözer!
        {
            // Performans için MusteriId, AktifMi ve KayitTarihi üzerinden Composite Index
            builder.HasIndex(x => new { x.MusteriId, x.AktifMi, x.KayitTarihi })
                   .HasDatabaseName($"IX_{typeof(T).Name}_SaaS_Performance");
        }
    }
}