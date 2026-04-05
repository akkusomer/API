using AtlasWeb.Data;
using AtlasWeb.DTOs;
using AtlasWeb.Models;
using AtlasWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CariTipController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CariTipController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCariTipler([FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Cari tipleri listelemek icin bir sirket baglantisi gereklidir." });
            }

            var tipler = await _context.CariTipler
                .IgnoreQueryFilters()
                .Where(ct => ct.MusteriId == tenantId.Value && ct.AktifMi)
                .OrderBy(ct => ct.Adi)
                .Select(ct => new
                {
                    ct.Id,
                    ct.Adi,
                    ct.Aciklama,
                    ct.KayitTarihi
                })
                .ToListAsync();

            return Ok(tipler);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Cari tipi gormek icin bir sirket baglantisi gereklidir." });
            }

            var tip = await _context.CariTipler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.MusteriId == tenantId.Value && ct.AktifMi);

            if (tip is null)
            {
                return NotFound(new { hata = "Cari tip bulunamadi." });
            }

            return Ok(tip);
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] CariTipDto dto, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Cari tip tanimlamak icin bir sirket baglantisi gereklidir." });
            }

            var mevcutMu = await _context.CariTipler
                .IgnoreQueryFilters()
                .Where(ct => ct.MusteriId == tenantId.Value && ct.AktifMi)
                .AnyAsync(ct => ct.Adi.ToLower() == dto.Adi.ToLower());

            if (mevcutMu)
            {
                return BadRequest(new { hata = "Bu isimde bir cari tip zaten mevcut." });
            }

            var yeniTip = new CariTip
            {
                MusteriId = tenantId.Value,
                Adi = dto.Adi.Trim(),
                Aciklama = dto.Aciklama?.Trim()
            };

            _context.CariTipler.Add(yeniTip);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari tip basariyla eklendi.", id = yeniTip.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] CariTipDto dto, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Cari tip guncellemek icin bir sirket baglantisi gereklidir." });
            }

            var tip = await _context.CariTipler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.MusteriId == tenantId.Value && ct.AktifMi);

            if (tip is null)
            {
                return NotFound(new { hata = "Cari tip bulunamadi." });
            }

            var cakisma = await _context.CariTipler
                .IgnoreQueryFilters()
                .Where(ct => ct.MusteriId == tenantId.Value && ct.AktifMi)
                .AnyAsync(ct => ct.Adi.ToLower() == dto.Adi.ToLower() && ct.Id != id);

            if (cakisma)
            {
                return BadRequest(new { hata = "Bu isimde baska bir cari tip zaten mevcut." });
            }

            tip.Adi = dto.Adi.Trim();
            tip.Aciklama = dto.Aciklama?.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Cari tip guncellendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Cari tip silmek icin bir sirket baglantisi gereklidir." });
            }

            var tip = await _context.CariTipler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.MusteriId == tenantId.Value && ct.AktifMi);

            if (tip is null)
            {
                return NotFound(new { hata = "Cari tip bulunamadi." });
            }

            var bagliKartVarMi = await _context.CariKartlar
                .IgnoreQueryFilters()
                .AnyAsync(ck => ck.MusteriId == tenantId.Value && ck.CariTipId == id && ck.AktifMi);

            if (bagliKartVarMi)
            {
                return Conflict(new { hata = "Bu cari tipe bagli aktif cari kartlar bulunuyor." });
            }

            _context.CariTipler.Remove(tip);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Cari tip pasife alindi." });
        }

        private async Task<Guid?> ResolveTargetTenantIdAsync(Guid? requestedTenantId)
        {
            if (_currentUserService.MusteriId is null || _currentUserService.MusteriId == Guid.Empty)
            {
                return null;
            }

            var targetTenantId = _currentUserService.MusteriId.Value;
            if (_currentUserService.IsSystemAdmin && requestedTenantId is not null && requestedTenantId != Guid.Empty)
            {
                targetTenantId = requestedTenantId.Value;
            }

            var tenantExists = await _context.Musteriler
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == targetTenantId && m.AktifMi);

            return tenantExists ? targetTenantId : null;
        }
    }
}
