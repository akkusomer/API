using Microsoft.EntityFrameworkCore;
using AtlasWeb.Models;
using AtlasWeb.Services;
using AtlasWeb.Data.Configurations;
using Serilog;

namespace AtlasWeb.Data
{
    public class AtlasDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public AtlasDbContext(DbContextOptions<AtlasDbContext> options, ICurrentUserService currentUserService)
            : base(options) => _currentUserService = currentUserService;

        public DbSet<Musteri> Musteriler { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Fatura> Faturalar { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<Birim> Birimler { get; set; }
        public DbSet<Stok> Stoklar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var mid = _currentUserService.MusteriId ?? Guid.Empty;
            var isAdmin = _currentUserService.IsAdmin;

            modelBuilder.ApplyGlobalFilters<ITenantEntity>(e => isAdmin || e.MusteriId == mid);
            modelBuilder.ApplyGlobalFilters<ISoftDelete>(e => e.AktifMi);

            modelBuilder.Entity<Fatura>().ConfigureSaaSEntity();
            modelBuilder.Entity<Stok>().ConfigureSaaSEntity(); // Stoklar için üçlü SaaS performan indeksi eklendi
            modelBuilder.Entity<Kullanici>().ConfigureSaaSEntity();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var user = _currentUserService.EPosta ?? "System";
            var mid = _currentUserService.MusteriId ?? Guid.Empty; // 🛡️ Fix
            var isAdmin = _currentUserService.IsAdmin; // 🛡️ Fix

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.Id == Guid.Empty) entry.Entity.Id = IdGenerator.CreateV7();
                        entry.Entity.MusteriId = mid;
                        entry.Entity.OlusturanKullanici = user;
                        break;

                    case EntityState.Modified:
                        if (!isAdmin && entry.Property(x => x.MusteriId).IsModified) throw new UnauthorizedAccessException();
                        entry.Entity.GuncellemeTarihi = DateTime.UtcNow;
                        entry.Entity.GuncelleyenKullanici = user;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.AktifMi = false;
                        entry.Entity.SilinmeTarihi = DateTime.UtcNow;
                        entry.Entity.SilenKullanici = user;
                        break;
                }
            }
            return await base.SaveChangesAsync(ct);
        }
    }
}