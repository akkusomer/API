using Microsoft.EntityFrameworkCore;
using AtlasWeb.Models;
using AtlasWeb.Services;

namespace AtlasWeb.Data
{
    public class AtlasDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public AtlasDbContext(DbContextOptions<AtlasDbContext> options, ICurrentUserService currentUserService)
            : base(options) => _currentUserService = currentUserService;

        /// <summary>
        /// EF global query filter ifadeleri bu özellikleri her sorguda değerlendirir; OnModelCreating içinde
        /// yerel değişken kapanışı kullanılmadığı için kiracı filtresi istek başına doğrudur.
        /// </summary>
        public bool IsAdminForQueryFilter => _currentUserService.IsAdmin;

        /// <summary>Kullanıcının bağlı olduğu müşteri (kiracı) kimliği.</summary>
        public Guid CurrentTenantId => _currentUserService.MusteriId ?? Guid.Empty;

        // ── DbSets ──────────────────────────────────────────────────────────────
        public DbSet<Musteri>    Musteriler  { get; set; }
        public DbSet<Kullanici>  Kullanicilar { get; set; }
        public DbSet<Fatura>     Faturalar   { get; set; }
        public DbSet<AuditLog>   AuditLogs   { get; set; }
        public DbSet<ErrorLog>   ErrorLogs   { get; set; }
        public DbSet<Birim>      Birimler    { get; set; }
        public DbSet<Stok>       Stoklar     { get; set; }
        public DbSet<CariTip>        CariTipler      { get; set; }
        public DbSet<CariKart>       CariKartlar     { get; set; }
        public DbSet<KullaniciToken> KullaniciTokenler { get; set; } // 🛡️ Hashed refresh tokens

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Global Query Filters (Tenant + Soft Delete) ───────────────────────
            modelBuilder.Entity<Kullanici>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Stok>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Fatura>().HasQueryFilter(e =>
                (IsAdminForQueryFilter || e.MusteriId == CurrentTenantId) && e.AktifMi);

            modelBuilder.Entity<Musteri>().HasQueryFilter(m =>
                (IsAdminForQueryFilter || m.Id == CurrentTenantId) && m.AktifMi);

            modelBuilder.Entity<Birim>().HasQueryFilter(b => b.AktifMi);

            // ✅ CariTip global filtresi (tenant'sız, sadece aktif)
            modelBuilder.Entity<CariTip>().HasQueryFilter(ct => ct.AktifMi);

            // ✅ CariKart tenant + aktif filtresi
            modelBuilder.Entity<CariKart>().HasQueryFilter(ck =>
                (IsAdminForQueryFilter || ck.MusteriId == CurrentTenantId) && ck.AktifMi);

            // ── SaaS Bileşik İndeksler ────────────────────────────────────────────
            modelBuilder.Entity<Fatura>().ConfigureSaaSEntity();
            modelBuilder.Entity<Stok>().ConfigureSaaSEntity();
            modelBuilder.Entity<Kullanici>().ConfigureSaaSEntity();
            modelBuilder.Entity<CariKart>().ConfigureSaaSEntity();   // ✅ YENİ

            // ✅ FaturaNo → kiracı bazında unique (aynı müşteri altında iki kez aynı fatura no olamaz)
            modelBuilder.Entity<Fatura>()
                .HasIndex(f => new { f.MusteriId, f.FaturaNo })
                .IsUnique()
                .HasDatabaseName("IX_Faturalar_MusteriId_FaturaNo_Unique");

            // ── CariKart → CariTip ilişkisi ───────────────────────────────────────
            modelBuilder.Entity<CariKart>()
                .HasOne(ck => ck.CariTip)
                .WithMany(ct => ct.CariKartlar)
                .HasForeignKey(ck => ck.CariTipId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── KullaniciToken → Kullanici ilişkisi ──────────────────────────────
            modelBuilder.Entity<KullaniciToken>()
                .HasOne(t => t.Kullanici)
                .WithMany(k => k.Tokens)
                .HasForeignKey(t => t.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // Refresh token hash unique (tek token = tek satır), hızlı lookup için index
            modelBuilder.Entity<KullaniciToken>()
                .HasIndex(t => t.RefreshTokenHash)
                .IsUnique()
                .HasDatabaseName("IX_KullaniciTokenler_RefreshTokenHash_Unique");

            // Aktif-token sorgularını hızlandırmak için bileşik index
            modelBuilder.Entity<KullaniciToken>()
                .HasIndex(t => new { t.KullaniciId, t.ExpiryTime })
                .HasDatabaseName("IX_KullaniciTokenler_KullaniciId_Expiry");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var user  = _currentUserService.EPosta ?? "System";
            var mid   = _currentUserService.MusteriId ?? Guid.Empty;
            var isAdmin = _currentUserService.IsAdmin;

            var auditEntries = new List<AuditLog>();

            // ── BaseEntity türevleri (Kullanici, Stok, Fatura, CariKart) ──────────
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.Id == Guid.Empty)
                            entry.Entity.Id = IdGenerator.CreateV7();
                        if (entry.Entity.MusteriId == Guid.Empty)
                            entry.Entity.MusteriId = mid;
                        entry.Entity.OlusturanKullanici = user;
                        // ✅ AuditLog
                        auditEntries.Add(new AuditLog
                        {
                            Id         = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId   = entry.Entity.Id.ToString(),
                            Action     = "Insert",
                            UserId     = user,
                            Timestamp  = DateTime.UtcNow
                        });
                        break;

                    case EntityState.Modified:
                        if (!isAdmin && entry.Property(x => x.MusteriId).IsModified)
                            throw new UnauthorizedAccessException();
                        entry.Entity.GuncellemeTarihi    = DateTime.UtcNow;
                        entry.Entity.GuncelleyenKullanici = user;
                        // ✅ AuditLog
                        auditEntries.Add(new AuditLog
                        {
                            Id         = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId   = entry.Entity.Id.ToString(),
                            Action     = "Update",
                            UserId     = user,
                            Timestamp  = DateTime.UtcNow
                        });
                        break;

                    case EntityState.Deleted:
                        entry.State                  = EntityState.Modified;
                        entry.Entity.AktifMi         = false;
                        entry.Entity.SilinmeTarihi   = DateTime.UtcNow;
                        entry.Entity.SilenKullanici  = user;
                        // ✅ AuditLog
                        auditEntries.Add(new AuditLog
                        {
                            Id         = IdGenerator.CreateV7(),
                            EntityName = entry.Entity.GetType().Name,
                            EntityId   = entry.Entity.Id.ToString(),
                            Action     = "Delete",
                            UserId     = user,
                            Timestamp  = DateTime.UtcNow
                        });
                        break;
                }
            }

            // ── ISoftDelete (BaseEntity olmayan: Birim, CariTip) ─────────────────
            foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
            {
                if (entry.Entity is BaseEntity) continue;
                if (entry.State != EntityState.Deleted) continue;

                entry.State          = EntityState.Modified;
                entry.Entity.AktifMi = false;

                // Birim
                if (entry.Entity is Birim birim)
                {
                    birim.SilinmeTarihi  = DateTime.UtcNow;
                    birim.SilenKullanici = user;
                }

                // ✅ CariTip soft delete
                if (entry.Entity is CariTip cariTip)
                {
                    cariTip.SilinmeTarihi  = DateTime.UtcNow;
                    cariTip.SilenKullanici = user;
                }
            }

            // ✅ AuditLog kayıtlarını batch olarak ekle (SaveChanges'i özyinelemeli çağırmamak için AddRange)
            if (auditEntries.Count > 0)
                AuditLogs.AddRange(auditEntries);

            return await base.SaveChangesAsync(ct);
        }
    }
}
