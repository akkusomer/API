using System.IO;
using AtlasWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AtlasDbContext _context;

        public AdminController(AtlasDbContext context)
        {
            _context = context;
        }

        [HttpGet("aktiviteler")]
        public async Task<IActionResult> GetUserActivities()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .Take(100)
                .Select(x => new 
                {
                    x.Id,
                    x.Action,
                    x.UserId,
                    x.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("sistem-hatalari")]
        public async Task<IActionResult> GetSystemErrors()
        {
            var hatalar = await _context.ErrorLogs
                .OrderByDescending(x => x.Tarih)
                .Take(100)
                .ToListAsync();

            return Ok(hatalar);
        }

        [HttpGet("dosya-loglari")]
        public async Task<IActionResult> GetFileLogs()
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
                if (!Directory.Exists(logPath)) return NotFound("Log dizini bulunamadı.");

                var file = Directory.GetFiles(logPath)
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (file == null) return NotFound("Log dosyası bulunamadı.");

                var lines = await System.IO.File.ReadAllLinesAsync(file);
                var lastLines = lines.Skip(Math.Max(0, lines.Length - 100)).ToList();

                return Ok(lastLines);
            }
            catch (Exception ex)
            {
                return BadRequest("Loglar okunurken hata: " + ex.Message);
            }
        }
    }
}
