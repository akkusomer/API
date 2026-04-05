using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Data
{
    public static class TenantReferenceDataSeeder
    {
        private static readonly (string Ad, string Sembol)[] DefaultUnits =
        {
            ("Adet", "ADET"),
            ("Kilo", "KG")
        };

        private static readonly (string Adi, string? Aciklama)[] DefaultCariTypes =
        {
            ("Musteri", "Varsayilan musteri cari tipi"),
            ("Tedarikci", "Varsayilan tedarikci cari tipi")
        };

        public static async Task EnsureDefaultsForAllCustomersAsync(
            AtlasDbContext db,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var customerIds = await db.Musteriler
                .IgnoreQueryFilters()
                .Where(m => m.AktifMi)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            foreach (var customerId in customerIds)
            {
                await EnsureDefaultsForCustomerAsync(db, customerId, cancellationToken);
            }

            await RemapLegacyStockUnitsAsync(db, cancellationToken);
            await RemapLegacyCariTypesAsync(db, cancellationToken);

            logger?.LogInformation("Tenant reference data verified for {CustomerCount} customer(s).", customerIds.Count);
        }

        public static async Task EnsureDefaultsForCustomerAsync(
            AtlasDbContext db,
            Guid musteriId,
            CancellationToken cancellationToken = default)
        {
            var birimler = await db.Birimler
                .IgnoreQueryFilters()
                .Where(b => b.MusteriId == musteriId)
                .ToListAsync(cancellationToken);

            foreach (var (ad, sembol) in DefaultUnits)
            {
                var mevcutBirim = birimler.FirstOrDefault(b =>
                    string.Equals(b.Ad, ad, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(b.Sembol, sembol, StringComparison.OrdinalIgnoreCase));

                if (mevcutBirim is not null)
                {
                    ReactivateSoftDeletedEntity(mevcutBirim);
                    continue;
                }

                var birim = new Birim
                {
                    Id = IdGenerator.CreateV7(),
                    MusteriId = musteriId,
                    Ad = ad,
                    Sembol = sembol,
                    AktifMi = true
                };

                db.Birimler.Add(birim);
                birimler.Add(birim);
            }

            var cariTipler = await db.CariTipler
                .IgnoreQueryFilters()
                .Where(ct => ct.MusteriId == musteriId)
                .ToListAsync(cancellationToken);

            foreach (var (adi, aciklama) in DefaultCariTypes)
            {
                var mevcutCariTip = cariTipler.FirstOrDefault(ct =>
                    string.Equals(ct.Adi, adi, StringComparison.OrdinalIgnoreCase));

                if (mevcutCariTip is not null)
                {
                    ReactivateSoftDeletedEntity(mevcutCariTip);
                    continue;
                }

                var cariTip = new CariTip
                {
                    Id = IdGenerator.CreateV7(),
                    MusteriId = musteriId,
                    Adi = adi,
                    Aciklama = aciklama,
                    AktifMi = true
                };

                db.CariTipler.Add(cariTip);
                cariTipler.Add(cariTip);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static void ReactivateSoftDeletedEntity(ISoftDelete entity)
        {
            if (entity.AktifMi)
            {
                return;
            }

            entity.AktifMi = true;

            switch (entity)
            {
                case Birim birim:
                    birim.SilinmeTarihi = null;
                    birim.SilenKullanici = null;
                    break;
                case CariTip cariTip:
                    cariTip.SilinmeTarihi = null;
                    cariTip.SilenKullanici = null;
                    break;
            }
        }

        private static async Task RemapLegacyStockUnitsAsync(AtlasDbContext db, CancellationToken cancellationToken)
        {
            var stoklar = await db.Stoklar
                .IgnoreQueryFilters()
                .Include(s => s.Birim)
                .Where(s => s.Birim != null && s.Birim.MusteriId != s.MusteriId)
                .ToListAsync(cancellationToken);

            if (stoklar.Count == 0)
            {
                return;
            }

            var tenantUnits = await db.Birimler
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            foreach (var stok in stoklar)
            {
                if (stok.Birim is null)
                {
                    continue;
                }

                var hedefBirim = tenantUnits.FirstOrDefault(b =>
                    b.MusteriId == stok.MusteriId
                    && string.Equals(b.Ad, stok.Birim.Ad, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(b.Sembol, stok.Birim.Sembol, StringComparison.OrdinalIgnoreCase));

                if (hedefBirim is null)
                {
                    hedefBirim = new Birim
                    {
                        Id = IdGenerator.CreateV7(),
                        MusteriId = stok.MusteriId,
                        Ad = stok.Birim.Ad,
                        Sembol = stok.Birim.Sembol,
                        AktifMi = true
                    };

                    db.Birimler.Add(hedefBirim);
                    tenantUnits.Add(hedefBirim);
                }

                stok.BirimId = hedefBirim.Id;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task RemapLegacyCariTypesAsync(AtlasDbContext db, CancellationToken cancellationToken)
        {
            var cariKartlar = await db.CariKartlar
                .IgnoreQueryFilters()
                .Include(ck => ck.CariTip)
                .Where(ck => ck.CariTip != null && ck.CariTip.MusteriId != ck.MusteriId)
                .ToListAsync(cancellationToken);

            if (cariKartlar.Count == 0)
            {
                return;
            }

            var tenantCariTypes = await db.CariTipler
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            foreach (var cariKart in cariKartlar)
            {
                if (cariKart.CariTip is null)
                {
                    continue;
                }

                var hedefCariTip = tenantCariTypes.FirstOrDefault(ct =>
                    ct.MusteriId == cariKart.MusteriId
                    && string.Equals(ct.Adi, cariKart.CariTip.Adi, StringComparison.OrdinalIgnoreCase));

                if (hedefCariTip is null)
                {
                    hedefCariTip = new CariTip
                    {
                        Id = IdGenerator.CreateV7(),
                        MusteriId = cariKart.MusteriId,
                        Adi = cariKart.CariTip.Adi,
                        Aciklama = cariKart.CariTip.Aciklama,
                        AktifMi = true
                    };

                    db.CariTipler.Add(hedefCariTip);
                    tenantCariTypes.Add(hedefCariTip);
                }

                cariKart.CariTipId = hedefCariTip.Id;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
