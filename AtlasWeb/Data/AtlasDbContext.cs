using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Data
{
    public class AtlasDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public static readonly Guid SystemMusteriId = new("e06c1341-3b74-4b8c-8c6e-984bb646e297");

        public AtlasDbContext(DbContextOptions<AtlasDbContext> options, ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public bool IsAdminForQueryFilter => _currentUserService.IsSystemAdmin;

        public Guid CurrentTenantId => _currentUserService.MusteriId ?? SystemMusteriId;

        public DbSet<Musteri> Musteriler { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Fatura> Faturalar { get; set; }
        public DbSet<FaturaDetay> FaturaDetaylari { get; set; }
        public DbSet<KasaFis> KasaFisleri { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<Birim> Birimler { get; set; }
        public DbSet<Stok> Stoklar { get; set; }
        public DbSet<CariTip> CariTipler { get; set; }
        public DbSet<CariKart> CariKartlar { get; set; }
        public DbSet<HksAyar> HksAyarlari { get; set; }
        public DbSet<HksSifat> HksSifatlar { get; set; }
        public DbSet<HksIl> HksIller { get; set; }
        public DbSet<HksIlce> HksIlceler { get; set; }
        public DbSet<HksBelde> HksBeldeler { get; set; }
        public DbSet<HksUrun> HksUrunler { get; set; }
        public DbSet<HksUrunBirim> HksUrunBirimleri { get; set; }
        public DbSet<HksIsletmeTuru> HksIsletmeTurleri { get; set; }
        public DbSet<HksUretimSekli> HksUretimSekilleri { get; set; }
        public DbSet<HksUrunCinsi> HksUrunCinsleri { get; set; }
        public DbSet<HksReferansKunyeKayit> HksReferansKunyeKayitlari { get; set; }
        public DbSet<KullaniciToken> KullaniciTokenler { get; set; }
        public DbSet<KullaniciSifreSifirlamaToken> KullaniciSifreSifirlamaTokenler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Kullanici>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Stok>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Fatura>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);
            modelBuilder.Entity<FaturaDetay>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);
            modelBuilder.Entity<KasaFis>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Musteri>().HasQueryFilter(m =>
                (IsAdminForQueryFilter || m.Id == CurrentTenantId) && m.AktifMi);

            modelBuilder.Entity<Birim>().HasQueryFilter(b =>
                (IsAdminForQueryFilter || b.MusteriId == CurrentTenantId) && b.AktifMi);
            modelBuilder.Entity<CariTip>().HasQueryFilter(ct =>
                (IsAdminForQueryFilter || ct.MusteriId == CurrentTenantId) && ct.AktifMi);
            modelBuilder.Entity<CariKart>().HasQueryFilter(ck =>
                (IsAdminForQueryFilter || ck.MusteriId == CurrentTenantId) && ck.AktifMi);
            modelBuilder.Entity<HksAyar>().HasQueryFilter(ha =>
                (IsAdminForQueryFilter || ha.MusteriId == CurrentTenantId) && ha.AktifMi);
            modelBuilder.Entity<HksSifat>().HasQueryFilter(hs => hs.AktifMi);
            modelBuilder.Entity<HksIl>().HasQueryFilter(hi => hi.AktifMi);
            modelBuilder.Entity<HksIlce>().HasQueryFilter(hic => hic.AktifMi);
            modelBuilder.Entity<HksBelde>().HasQueryFilter(hb => hb.AktifMi);
            modelBuilder.Entity<HksUrun>().HasQueryFilter(hu => hu.AktifMi);
            modelBuilder.Entity<HksUrunBirim>().HasQueryFilter(hub => hub.AktifMi);
            modelBuilder.Entity<HksIsletmeTuru>().HasQueryFilter(hit => hit.AktifMi);
            modelBuilder.Entity<HksUretimSekli>().HasQueryFilter(hus => hus.AktifMi);
            modelBuilder.Entity<HksUrunCinsi>().HasQueryFilter(huc => huc.AktifMi);
            modelBuilder.Entity<HksReferansKunyeKayit>().HasQueryFilter(hk =>
                (IsAdminForQueryFilter || hk.MusteriId == CurrentTenantId) && hk.AktifMi);

            modelBuilder.Entity<Fatura>().ConfigureSaaSEntity();
            modelBuilder.Entity<FaturaDetay>().ConfigureSaaSEntity();
            modelBuilder.Entity<KasaFis>().ConfigureSaaSEntity();
            modelBuilder.Entity<Stok>().ConfigureSaaSEntity();
            modelBuilder.Entity<Kullanici>().ConfigureSaaSEntity();
            modelBuilder.Entity<CariKart>().ConfigureSaaSEntity();
            modelBuilder.Entity<Birim>().ConfigureSaaSEntity();
            modelBuilder.Entity<CariTip>().ConfigureSaaSEntity();
            modelBuilder.Entity<HksAyar>().ConfigureSaaSEntity();
            modelBuilder.Entity<HksReferansKunyeKayit>().ConfigureSaaSEntity();

            modelBuilder.Entity<Fatura>()
                .HasIndex(f => new { f.MusteriId, f.FaturaNo })
                .IsUnique()
                .HasDatabaseName("IX_Faturalar_MusteriId_FaturaNo_Unique");

            modelBuilder.Entity<Fatura>()
                .Property(f => f.Tutar)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Fatura>()
                .Property(f => f.TahsilEdilenTutar)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FaturaDetay>()
                .Property(fd => fd.Miktar)
                .HasPrecision(18, 3);

            modelBuilder.Entity<FaturaDetay>()
                .Property(fd => fd.BirimFiyat)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FaturaDetay>()
                .Property(fd => fd.SatirToplami)
                .HasPrecision(18, 2);

            modelBuilder.Entity<KasaFis>()
                .Property(kf => kf.Tutar)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.EPosta)
                .IsUnique()
                .HasDatabaseName("IX_Kullanicilar_EPosta_Unique");

            modelBuilder.Entity<Musteri>()
                .HasIndex(m => m.MusteriKodu)
                .IsUnique()
                .HasDatabaseName("IX_Musteriler_MusteriKodu_Unique");

            modelBuilder.Entity<Birim>()
                .HasIndex(b => new { b.MusteriId, b.Ad })
                .IsUnique()
                .HasDatabaseName("IX_Birimler_MusteriId_Ad_Unique");

            modelBuilder.Entity<Birim>()
                .HasIndex(b => new { b.MusteriId, b.Sembol })
                .IsUnique()
                .HasDatabaseName("IX_Birimler_MusteriId_Sembol_Unique");

            modelBuilder.Entity<CariTip>()
                .HasIndex(ct => new { ct.MusteriId, ct.Adi })
                .IsUnique()
                .HasDatabaseName("IX_CariTipler_MusteriId_Adi_Unique");

            modelBuilder.Entity<HksAyar>()
                .HasIndex(ha => ha.MusteriId)
                .IsUnique()
                .HasDatabaseName("IX_HksAyarlari_MusteriId_Unique");

            modelBuilder.Entity<HksSifat>()
                .HasIndex(hs => hs.HksSifatId)
                .IsUnique()
                .HasDatabaseName("IX_HksSifatlar_HksSifatId_Unique");

            modelBuilder.Entity<HksSifat>()
                .HasIndex(hs => new { hs.AktifMi, hs.KayitTarihi })
                .HasDatabaseName("IX_HksSifatlar_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksIl>()
                .HasIndex(hi => hi.HksIlId)
                .IsUnique()
                .HasDatabaseName("IX_HksIller_HksIlId_Unique");

            modelBuilder.Entity<HksIl>()
                .HasIndex(hi => new { hi.AktifMi, hi.KayitTarihi })
                .HasDatabaseName("IX_HksIller_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksIlce>()
                .HasIndex(hic => hic.HksIlceId)
                .IsUnique()
                .HasDatabaseName("IX_HksIlceler_HksIlceId_Unique");

            modelBuilder.Entity<HksIlce>()
                .HasIndex(hic => new { hic.HksIlId, hic.AktifMi, hic.KayitTarihi })
                .HasDatabaseName("IX_HksIlceler_HksIlId_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksBelde>()
                .HasIndex(hb => hb.HksBeldeId)
                .IsUnique()
                .HasDatabaseName("IX_HksBeldeler_HksBeldeId_Unique");

            modelBuilder.Entity<HksBelde>()
                .HasIndex(hb => new { hb.HksIlceId, hb.AktifMi, hb.KayitTarihi })
                .HasDatabaseName("IX_HksBeldeler_HksIlceId_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksUrun>()
                .HasIndex(hu => hu.HksUrunId)
                .IsUnique()
                .HasDatabaseName("IX_HksUrunler_HksUrunId_Unique");

            modelBuilder.Entity<HksUrun>()
                .HasIndex(hu => new { hu.AktifMi, hu.KayitTarihi })
                .HasDatabaseName("IX_HksUrunler_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksUrunBirim>()
                .HasIndex(hub => hub.HksUrunBirimId)
                .IsUnique()
                .HasDatabaseName("IX_HksUrunBirimleri_HksUrunBirimId_Unique");

            modelBuilder.Entity<HksUrunBirim>()
                .HasIndex(hub => new { hub.AktifMi, hub.KayitTarihi })
                .HasDatabaseName("IX_HksUrunBirimleri_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksIsletmeTuru>()
                .HasIndex(hit => hit.HksIsletmeTuruId)
                .IsUnique()
                .HasDatabaseName("IX_HksIsletmeTurleri_HksIsletmeTuruId_Unique");

            modelBuilder.Entity<HksIsletmeTuru>()
                .HasIndex(hit => new { hit.AktifMi, hit.KayitTarihi })
                .HasDatabaseName("IX_HksIsletmeTurleri_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksUretimSekli>()
                .HasIndex(hus => hus.HksUretimSekliId)
                .IsUnique()
                .HasDatabaseName("IX_HksUretimSekilleri_HksUretimSekliId_Unique");

            modelBuilder.Entity<HksUretimSekli>()
                .HasIndex(hus => new { hus.AktifMi, hus.KayitTarihi })
                .HasDatabaseName("IX_HksUretimSekilleri_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksUrunCinsi>()
                .HasIndex(huc => huc.HksUrunCinsiId)
                .IsUnique()
                .HasDatabaseName("IX_HksUrunCinsleri_HksUrunCinsiId_Unique");

            modelBuilder.Entity<HksUrunCinsi>()
                .HasIndex(huc => new { huc.HksUrunId, huc.AktifMi, huc.KayitTarihi })
                .HasDatabaseName("IX_HksUrunCinsleri_HksUrunId_AktifMi_KayitTarihi");

            modelBuilder.Entity<HksReferansKunyeKayit>()
                .Property(hk => hk.BaslangicTarihi)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<HksReferansKunyeKayit>()
                .Property(hk => hk.BitisTarihi)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<HksReferansKunyeKayit>()
                .HasIndex(hk => hk.MusteriId)
                .IsUnique()
                .HasDatabaseName("IX_HksReferansKunyeKayitlari_MusteriId_Unique");

            modelBuilder.Entity<Musteri>().HasData(new Musteri
            {
                Id = SystemMusteriId,
                MusteriKodu = "ATLASWEB",
                Unvan = "AtlasWeb Sistem Yonetimi",
                VergiNo = "00000000000",
                VergiDairesi = "SISTEM",
                KimlikTuru = KimlikTuruEnum.VKN,
                GsmNo = "0000000000",
                EPosta = "admin@atlasweb.local",
                Il = "ANKARA",
                Ilce = "CANKAYA",
                AdresDetay = "SISTEM MERKEZI",
                PaketTipi = PaketTipiEnum.Kurumsal,
                AktifMi = true,
                KayitTarihi = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            modelBuilder.Entity<Stok>()
                .HasIndex(s => new { s.MusteriId, s.StokKodu })
                .IsUnique()
                .HasDatabaseName("IX_Stoklar_MusteriId_StokKodu_Unique");

            modelBuilder.Entity<CariKart>()
                .HasIndex(ck => new { ck.MusteriId, ck.VTCK_No })
                .IsUnique()
                .HasFilter("\"VTCK_No\" IS NOT NULL")
                .HasDatabaseName("IX_CariKartlar_MusteriId_VTCK_No_Unique");

            modelBuilder.Entity<CariKart>()
                .HasOne(ck => ck.CariTip)
                .WithMany(ct => ct.CariKartlar)
                .HasForeignKey(ck => ck.CariTipId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fatura>()
                .HasOne(f => f.CariKart)
                .WithMany()
                .HasForeignKey(f => f.CariKartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FaturaDetay>()
                .HasOne(fd => fd.Fatura)
                .WithMany(f => f.Detaylar)
                .HasForeignKey(fd => fd.FaturaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FaturaDetay>()
                .HasOne(fd => fd.Stok)
                .WithMany()
                .HasForeignKey(fd => fd.StokId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KasaFis>()
                .HasIndex(kf => new { kf.MusteriId, kf.BelgeNo })
                .IsUnique()
                .HasDatabaseName("IX_KasaFisleri_MusteriId_BelgeNo_Unique");

            modelBuilder.Entity<KasaFis>()
                .HasOne(kf => kf.CariKart)
                .WithMany()
                .HasForeignKey(kf => kf.CariKartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Stok>()
                .HasOne(s => s.Birim)
                .WithMany(b => b.Stoklar)
                .HasForeignKey(s => s.BirimId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KullaniciToken>()
                .HasOne(t => t.Kullanici)
                .WithMany(k => k.Tokens)
                .HasForeignKey(t => t.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<KullaniciToken>()
                .HasIndex(t => t.RefreshTokenHash)
                .IsUnique()
                .HasDatabaseName("IX_KullaniciTokenler_RefreshTokenHash_Unique");

            modelBuilder.Entity<KullaniciToken>()
                .HasIndex(t => new { t.KullaniciId, t.ExpiryTime })
                .HasDatabaseName("IX_KullaniciTokenler_KullaniciId_Expiry");

            modelBuilder.Entity<KullaniciSifreSifirlamaToken>()
                .HasOne(t => t.Kullanici)
                .WithMany(k => k.SifreSifirlamaTokenleri)
                .HasForeignKey(t => t.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<KullaniciSifreSifirlamaToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_KullaniciSifreSifirlamaTokenler_TokenHash_Unique");

            modelBuilder.Entity<KullaniciSifreSifirlamaToken>()
                .HasIndex(t => new { t.KullaniciId, t.ExpiryTime })
                .HasDatabaseName("IX_KullaniciSifreSifirlamaTokenler_KullaniciId_Expiry");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var user = _currentUserService.EPosta ?? "System";
            var mid = _currentUserService.MusteriId ?? Guid.Empty;
            var isAdmin = _currentUserService.IsSystemAdmin;

            var auditEntries = new List<AuditLog>();

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.Id == Guid.Empty)
                        {
                            entry.Entity.Id = IdGenerator.CreateV7();
                        }

                        if (entry.Entity.MusteriId == Guid.Empty)
                        {
                            entry.Entity.MusteriId = mid == Guid.Empty ? SystemMusteriId : mid;
                        }

                        entry.Entity.OlusturanKullanici = user;
                        auditEntries.Add(new AuditLog
                        {
                            Id = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId = entry.Entity.Id.ToString(),
                            Action = "Insert",
                            UserId = user,
                            Timestamp = DateTime.UtcNow
                        });
                        break;

                    case EntityState.Modified:
                        if (!isAdmin && entry.Property(x => x.MusteriId).IsModified)
                        {
                            throw new UnauthorizedAccessException();
                        }

                        entry.Entity.GuncellemeTarihi = DateTime.UtcNow;
                        entry.Entity.GuncelleyenKullanici = user;
                        auditEntries.Add(new AuditLog
                        {
                            Id = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId = entry.Entity.Id.ToString(),
                            Action = "Update",
                            UserId = user,
                            Timestamp = DateTime.UtcNow
                        });
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.AktifMi = false;
                        entry.Entity.SilinmeTarihi = DateTime.UtcNow;
                        entry.Entity.SilenKullanici = user;
                        auditEntries.Add(new AuditLog
                        {
                            Id = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId = entry.Entity.Id.ToString(),
                            Action = "Delete",
                            UserId = user,
                            Timestamp = DateTime.UtcNow
                        });
                        break;
                }
            }

            foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
            {
                if (entry.Entity is BaseEntity || entry.State != EntityState.Deleted)
                {
                    continue;
                }

                entry.State = EntityState.Modified;
                entry.Entity.AktifMi = false;

                if (entry.Entity is Birim birim)
                {
                    birim.SilinmeTarihi = DateTime.UtcNow;
                    birim.SilenKullanici = user;
                }

                if (entry.Entity is CariTip cariTip)
                {
                    cariTip.SilinmeTarihi = DateTime.UtcNow;
                    cariTip.SilenKullanici = user;
                }

                var entityId = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Id")?.CurrentValue?.ToString()
                    ?? string.Empty;

                auditEntries.Add(new AuditLog
                {
                    Id = IdGenerator.CreateV7(),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entityId,
                    Action = "Delete",
                    UserId = user,
                    Timestamp = DateTime.UtcNow
                });
            }

            if (auditEntries.Count > 0)
            {
                AuditLogs.AddRange(auditEntries);
            }

            return await base.SaveChangesAsync(ct);
        }
    }
}
