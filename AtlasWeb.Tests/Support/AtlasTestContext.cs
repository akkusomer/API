using AtlasWeb.Data;
using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Tests.Support;

internal sealed class AtlasTestContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public AtlasTestContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        CurrentUser = new TestCurrentUserService();

        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new AtlasDbContext(options, CurrentUser);
        DbContext.Database.EnsureCreated();
    }

    public AtlasDbContext DbContext { get; }

    public TestCurrentUserService CurrentUser { get; }

    public void SetUser(Guid? tenantId, bool isAdmin = false, string? email = null)
    {
        CurrentUser.MusteriId = tenantId;
        CurrentUser.IsAdmin = isAdmin;
        CurrentUser.EPosta = email ?? (isAdmin ? "admin@atlasweb.local" : "user@atlasweb.local");
    }

    public async Task<Musteri> CreateTenantAsync(string musteriKodu, string unvan)
    {
        var tenant = new Musteri
        {
            Id = IdGenerator.CreateV7(),
            MusteriKodu = musteriKodu,
            Unvan = unvan,
            VergiNo = $"VKN{musteriKodu}",
            VergiDairesi = "Merkez",
            KimlikTuru = KimlikTuruEnum.VKN,
            GsmNo = "05000000000",
            EPosta = $"{musteriKodu.ToLowerInvariant()}@atlasweb.local",
            Il = "İstanbul",
            Ilce = "Kadıköy",
            AdresDetay = "Test adresi",
            PaketTipi = PaketTipiEnum.Kurumsal,
            AktifMi = true,
            KayitTarihi = DateTime.UtcNow,
        };

        DbContext.Musteriler.Add(tenant);
        await DbContext.SaveChangesAsync();
        return tenant;
    }

    public async Task<Kullanici> CreateUserAsync(
        Guid tenantId,
        string email,
        string role = KullaniciRol.User,
        string password = "Strong!123",
        string ad = "Test",
        string soyad = "User")
    {
        var user = new Kullanici
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            Ad = ad,
            Soyad = soyad,
            EPosta = IdentityNormalizer.NormalizeEmail(email),
            Telefon = "05000000000",
            SifreHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = role,
            AktifMi = true,
        };

        DbContext.Kullanicilar.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    public async Task<Birim> CreateUnitAsync(Guid tenantId, string ad, string sembol)
    {
        var unit = new Birim
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            Ad = ad,
            Sembol = sembol,
            AktifMi = true,
        };

        DbContext.Birimler.Add(unit);
        await DbContext.SaveChangesAsync();
        return unit;
    }

    public async Task<CariTip> CreateCariTypeAsync(Guid tenantId, string ad, string? aciklama = null)
    {
        var cariType = new CariTip
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            Adi = ad,
            Aciklama = aciklama,
            AktifMi = true,
        };

        DbContext.CariTipler.Add(cariType);
        await DbContext.SaveChangesAsync();
        return cariType;
    }

    public async Task<HksIl> CreateHksIlAsync(int hksIlId, string ad)
    {
        var il = new HksIl
        {
            Id = IdGenerator.CreateV7(),
            HksIlId = hksIlId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksIller.Add(il);
        await DbContext.SaveChangesAsync();
        return il;
    }

    public async Task<HksSifat> CreateHksSifatAsync(int hksSifatId, string ad)
    {
        var sifat = new HksSifat
        {
            Id = IdGenerator.CreateV7(),
            HksSifatId = hksSifatId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksSifatlar.Add(sifat);
        await DbContext.SaveChangesAsync();
        return sifat;
    }

    public async Task<HksIsletmeTuru> CreateHksIsletmeTuruAsync(int hksIsletmeTuruId, string ad)
    {
        var isletmeTuru = new HksIsletmeTuru
        {
            Id = IdGenerator.CreateV7(),
            HksIsletmeTuruId = hksIsletmeTuruId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksIsletmeTurleri.Add(isletmeTuru);
        await DbContext.SaveChangesAsync();
        return isletmeTuru;
    }

    public async Task<HksIlce> CreateHksIlceAsync(int hksIlceId, int hksIlId, string ad)
    {
        var ilce = new HksIlce
        {
            Id = IdGenerator.CreateV7(),
            HksIlceId = hksIlceId,
            HksIlId = hksIlId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksIlceler.Add(ilce);
        await DbContext.SaveChangesAsync();
        return ilce;
    }

    public async Task<HksBelde> CreateHksBeldeAsync(int hksBeldeId, int hksIlceId, string ad)
    {
        var belde = new HksBelde
        {
            Id = IdGenerator.CreateV7(),
            HksBeldeId = hksBeldeId,
            HksIlceId = hksIlceId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksBeldeler.Add(belde);
        await DbContext.SaveChangesAsync();
        return belde;
    }

    public async Task<Stok> CreateStockAsync(Guid tenantId, Guid birimId, string stokKodu, string stokAdi)
    {
        var stock = new Stok
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            StokKodu = stokKodu,
            StokAdi = stokAdi,
            BirimId = birimId,
            AktifMi = true,
        };

        DbContext.Stoklar.Add(stock);
        await DbContext.SaveChangesAsync();
        return stock;
    }

    public async Task<HksUrun> CreateHksUrunAsync(int hksUrunId, string ad)
    {
        var urun = new HksUrun
        {
            Id = IdGenerator.CreateV7(),
            HksUrunId = hksUrunId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksUrunler.Add(urun);
        await DbContext.SaveChangesAsync();
        return urun;
    }

    public async Task<HksUretimSekli> CreateHksUretimSekliAsync(int hksUretimSekliId, string ad)
    {
        var uretimSekli = new HksUretimSekli
        {
            Id = IdGenerator.CreateV7(),
            HksUretimSekliId = hksUretimSekliId,
            Ad = ad,
            AktifMi = true
        };

        DbContext.HksUretimSekilleri.Add(uretimSekli);
        await DbContext.SaveChangesAsync();
        return uretimSekli;
    }

    public async Task<HksUrunCinsi> CreateHksUrunCinsiAsync(
        int hksUrunCinsiId,
        int hksUrunId,
        int? hksUretimSekliId,
        string ad,
        bool? ithalMi)
    {
        var urunCinsi = new HksUrunCinsi
        {
            Id = IdGenerator.CreateV7(),
            HksUrunCinsiId = hksUrunCinsiId,
            HksUrunId = hksUrunId,
            HksUretimSekliId = hksUretimSekliId,
            Ad = ad,
            IthalMi = ithalMi,
            AktifMi = true
        };

        DbContext.HksUrunCinsleri.Add(urunCinsi);
        await DbContext.SaveChangesAsync();
        return urunCinsi;
    }

    public async Task<CariKart> CreateCariCardAsync(Guid tenantId, Guid cariTipId, string unvan, string vtckNo)
    {
        var cariCard = new CariKart
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            CariTipId = cariTipId,
            Unvan = unvan,
            FaturaTipi = FaturaTipiEnum.Kurumsal,
            VTCK_No = vtckNo,
            Telefon = "02120000000",
            AktifMi = true,
        };

        DbContext.CariKartlar.Add(cariCard);
        await DbContext.SaveChangesAsync();
        return cariCard;
    }

    public async Task<Fatura> CreateInvoiceAsync(Guid tenantId, Guid cariKartId, Guid stokId, string faturaNo = "FTR000001")
    {
        var invoiceId = IdGenerator.CreateV7();
        var detail = new FaturaDetay
        {
            Id = IdGenerator.CreateV7(),
            MusteriId = tenantId,
            FaturaId = invoiceId,
            StokId = stokId,
            Miktar = 2,
            BirimFiyat = 15,
            SatirToplami = 30,
            AktifMi = true,
        };

        var invoice = new Fatura
        {
            Id = invoiceId,
            MusteriId = tenantId,
            FaturaNo = faturaNo,
            CariKartId = cariKartId,
            FaturaTarihi = DateTime.SpecifyKind(new DateTime(2026, 3, 30), DateTimeKind.Utc),
            Tutar = 30,
            Detaylar = [detail],
            AktifMi = true,
        };

        DbContext.Faturalar.Add(invoice);
        await DbContext.SaveChangesAsync();
        return invoice;
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
