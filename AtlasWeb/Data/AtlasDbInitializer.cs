using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Data
{
    public static class AtlasDbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();

            if (configuration.GetValue("Database:AutoMigrate", true))
            {
                await db.Database.MigrateAsync();
            }
            await EnsureSystemTenantAsync(db);
            await SeedEmptyDatabaseAsync(db, configuration, logger);
            await TenantReferenceDataSeeder.EnsureDefaultsForAllCustomersAsync(db, logger);
        }

        private static async Task EnsureSystemTenantAsync(AtlasDbContext db)
        {
            var exists = await db.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == AtlasDbContext.SystemMusteriId);

            if (exists)
            {
                return;
            }

            db.Musteriler.Add(new Musteri
            {
                Id = AtlasDbContext.SystemMusteriId,
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
                KayitTarihi = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        private static async Task SeedEmptyDatabaseAsync(AtlasDbContext db, IConfiguration configuration, ILogger logger)
        {
            var hasAnyBusinessCustomer = await db.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id != AtlasDbContext.SystemMusteriId);

            var hasAnyUser = await db.Kullanicilar
                .IgnoreQueryFilters()
                .AnyAsync();

            if (hasAnyBusinessCustomer || hasAnyUser)
            {
                return;
            }

            var customer = new Musteri
            {
                Id = IdGenerator.CreateV7(),
                MusteriKodu = (configuration["SeedData:CustomerCode"] ?? "DEMO-001").Trim(),
                Unvan = (configuration["SeedData:CustomerName"] ?? "AtlasWeb Demo Musteri").Trim(),
                VergiNo = (configuration["SeedData:CustomerTaxNumber"] ?? "11111111111").Trim(),
                VergiDairesi = (configuration["SeedData:CustomerTaxOffice"] ?? "MERKEZ").Trim(),
                KimlikTuru = KimlikTuruEnum.VKN,
                GsmNo = (configuration["SeedData:CustomerPhone"] ?? "05000000000").Trim(),
                EPosta = IdentityNormalizer.NormalizeEmail(configuration["SeedData:CustomerEmail"] ?? "demo@atlasweb.local"),
                Il = (configuration["SeedData:CustomerCity"] ?? "ISTANBUL").Trim(),
                Ilce = (configuration["SeedData:CustomerDistrict"] ?? "KADIKOY").Trim(),
                AdresDetay = (configuration["SeedData:CustomerAddress"] ?? "Demo Kurulum Adresi").Trim(),
                PaketTipi = PaketTipiEnum.Premium,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            };

            var adminEmail = IdentityNormalizer.NormalizeEmail(configuration["SeedData:AdminEmail"] ?? "admin@atlasweb.local");
            var adminPassword = configuration["SeedData:AdminPassword"]
                ?? Environment.GetEnvironmentVariable("ATLASWEB_SEED_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Bos veritabani ilk kurulumu icin SeedData:AdminPassword veya ATLASWEB_SEED_ADMIN_PASSWORD tanimlanmalidir.");
            }

            db.Musteriler.Add(customer);
            db.Kullanicilar.Add(new Kullanici
            {
                Id = IdGenerator.CreateV7(),
                Ad = (configuration["SeedData:AdminFirstName"] ?? "Sistem").Trim(),
                Soyad = (configuration["SeedData:AdminLastName"] ?? "Yoneticisi").Trim(),
                EPosta = adminEmail,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Rol = KullaniciRol.Admin,
                MusteriId = AtlasDbContext.SystemMusteriId,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            logger.LogInformation("Initial seed created. Admin email: {Email}", adminEmail);
        }
    }
}
