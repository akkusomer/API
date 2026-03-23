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
                .Where(x => x.Action == "Login" || x.Action == "Logout")
                .OrderByDescending(x => x.Timestamp)
                .Take(50)
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
    }
}
