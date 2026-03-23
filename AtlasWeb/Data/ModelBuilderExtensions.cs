using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AtlasWeb.Models;

namespace AtlasWeb.Data
{
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// 🛡️ Global Query Filter Uygulayıcı (Tenant & Soft Delete)
        /// </summary>
        public static void ApplyGlobalFilters<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> expression)
        {
            var entities = modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(TInterface).IsAssignableFrom(e.ClrType));

            foreach (var entity in entities)
            {
                var newParam = Expression.Parameter(entity.ClrType);
                var newBody = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), newParam, expression.Body);
                modelBuilder.Entity(entity.ClrType).HasQueryFilter(Expression.Lambda(newBody, newParam));
            }
        }

        /// <summary>
        /// ⚡ V7.0 Resilient: SaaS Tablo Yapılandırması ve Kompozit İndeksleme
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