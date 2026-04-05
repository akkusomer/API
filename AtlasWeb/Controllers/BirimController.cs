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
    public class BirimController : ControllerBase
    {
        private readonly AtlasDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public BirimController(AtlasDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBirimler([FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Birimleri listelemek icin bir sirket baglantisi gereklidir." });
            }

            var birimler = await _context.Birimler
                .IgnoreQueryFilters()
                .Where(b => b.MusteriId == tenantId.Value && b.AktifMi)
                .OrderBy(b => b.Ad)
                .ToListAsync();

            return Ok(birimler);
        }

        [HttpPost]
        public async Task<IActionResult> Ekle([FromBody] BirimDto dto, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Birim tanimlamak icin bir sirket baglantisi gereklidir." });
            }

            var exists = await _context.Birimler
                .IgnoreQueryFilters()
                .Where(b => b.MusteriId == tenantId.Value && b.AktifMi)
                .AnyAsync(b => b.Ad.ToLower() == dto.Ad.ToLower() || b.Sembol.ToLower() == dto.Sembol.ToLower());

            if (exists)
            {
                return BadRequest(new { hata = "Bu olcu birimi veya sembol zaten mevcut." });
            }

            var yeniBirim = new Birim
            {
                MusteriId = tenantId.Value,
                Ad = dto.Ad.Trim(),
                Sembol = dto.Sembol.Trim()
            };

            _context.Birimler.Add(yeniBirim);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Olcu birimi basariyla eklendi." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Sil(Guid id, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Birim silmek icin bir sirket baglantisi gereklidir." });
            }

            var birim = await _context.Birimler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id && b.MusteriId == tenantId.Value && b.AktifMi);

            if (birim is null)
            {
                return NotFound(new { hata = "Olcu birimi bulunamadi." });
            }

            _context.Birimler.Remove(birim);
            await _context.SaveChangesAsync();

            return Ok(new { mesaj = "Olcu birimi pasife alindi." });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Guncelle(Guid id, [FromBody] BirimDto dto, [FromQuery] Guid? musteriId = null)
        {
            var tenantId = await ResolveTargetTenantIdAsync(musteriId);
            if (tenantId is null)
            {
                return Unauthorized(new { hata = "Birim guncellemek icin bir sirket baglantisi gereklidir." });
            }

            var birim = await _context.Birimler
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id && b.MusteriId == tenantId.Value && b.AktifMi);

            if (birim is null)
            {
                return NotFound(new { hata = "Olcu birimi bulunamadi." });
            }

            var exists = await _context.Birimler
                .IgnoreQueryFilters()
                .Where(b => b.MusteriId == tenantId.Value && b.AktifMi && b.Id != id)
                .AnyAsync(b => b.Ad.ToLower() == dto.Ad.ToLower() || b.Sembol.ToLower() == dto.Sembol.ToLower());

            if (exists)
            {
                return BadRequest(new { hata = "Bu olcu birimi veya sembol zaten mevcut." });
            }

            birim.Ad = dto.Ad.Trim();
            birim.Sembol = dto.Sembol.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Olcu birimi guncellendi." });
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
