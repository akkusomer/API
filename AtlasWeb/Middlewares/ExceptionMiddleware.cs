using System.Net;
using System.Text.Json;
using AtlasWeb.Data;
using AtlasWeb.Models;
using Serilog;

namespace AtlasWeb.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (UnauthorizedAccessException ex)
            {
                Log.Fatal("🛑 ALERT: Unauthorized Tenant Access Attempt! {Msg}", ex.Message);
                await LogErrorToDbAsync(context, ex, "Güvenlik İhlali Bildirildi.");
                
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { hata = "Sisteme erişim yetkiniz bulunmamaktadır." }));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical Error: {Msg}", ex.Message);
                await LogErrorToDbAsync(context, ex, ex.Message);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { hata = "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz." }));
            }
        }

        private async Task LogErrorToDbAsync(HttpContext context, Exception ex, string message)
        {
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
                
                var errorLog = new ErrorLog
                {
                    HataMesaji = message,
                    HataDetayi = ex.StackTrace,
                    IstekYolu = context.Request.Path,
                    KullaniciId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                };

                dbContext.ErrorLogs.Add(errorLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                Log.Fatal(dbEx, "Veritabanına hata loglanırken başka bir hata oluştu!");
            }
        }
    }
}