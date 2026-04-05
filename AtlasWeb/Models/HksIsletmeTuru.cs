using System.ComponentModel.DataAnnotations;
using AtlasWeb.Services;

namespace AtlasWeb.Models;

public sealed class HksIsletmeTuru : ISoftDelete, IAuditEntity
{
    [Key]
    public Guid Id { get; set; } = IdGenerator.CreateV7();

    public int HksIsletmeTuruId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public bool AktifMi { get; set; } = true;

    public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

    public string? OlusturanKullanici { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public string? GuncelleyenKullanici { get; set; }

    public DateTime? SilinmeTarihi { get; set; }

    public string? SilenKullanici { get; set; }

    public AuditSource Source { get; set; } = AuditSource.System;
}
