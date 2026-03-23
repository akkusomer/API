namespace AtlasWeb.Models
{
    public interface ITenantEntity { Guid MusteriId { get; set; } }
    public interface ISoftDelete { bool AktifMi { get; set; } }
    public interface IAuditEntity { DateTime KayitTarihi { get; set; } }
    public enum AuditSource { Web = 1, System = 2, Security = 3 }
}