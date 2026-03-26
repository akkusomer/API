using AtlasWeb.Data;
using AtlasWeb.Services;
using AtlasWeb.Middlewares;
using AtlasWeb.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 🛡️ 1. LOGLAMA YAPISI (Serilog)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/security-audit-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 🏗️ 2. VERİTABANI BAĞLANTISI (PostgreSQL) — parola: ConnectionStrings__DefaultConnection veya User Secrets
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil.");

builder.Services.AddDbContext<AtlasDbContext>(opt =>
    opt.UseNpgsql(connectionString, n => n.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddControllers();

// 🛡️ 3. JWT AUTHENTICATION
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT key yapılandırılmamış. Jwt:Key veya JWT_SECRET_KEY ortam değişkenini tanımlayın.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// 🛡️ 4. VALIDATION & RATE LIMITING
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddRateLimiter(options =>
{
    // 🛡️ Sliding window per IP — her IP kendi bağımsız baskını alır (shared bucket sorunu çözüldü)
    options.AddSlidingWindowLimiter("LoginPolicy", opt =>
    {
        opt.Window               = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow    = 4;          // 15 saniyede bir segment
        opt.PermitLimit          = 5;          // dakikada maks 5 deneme / IP
        opt.QueueLimit           = 0;          // kuyruğa alma, direk reddet
    });

    // Her IP kendi penceresi içinde sayılır
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
        context =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter(ip);
            // Not: "LoginPolicy" named limiter zaten endpoint seviyesinde partition yapıyor,
            // GlobalLimiter buraya ileride API-wide bir limit eklemek için hazır.
        });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/json";
        var ip = ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        Serilog.Log.Warning("🚫 [RATE LIMIT] IP: {IP} → 429 Too Many Requests", ip);
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            hata = "Çok fazla istek gönderildi. Lütfen bir dakika bekleyip tekrar deneyin."
        }, token);
    };
});

// 🌐 5. CORS - TÜM ERİŞİMLERE İZİN VERİLDİ
builder.Services.AddCors(options =>
{
    options.AddPolicy("AtlasWebCors", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();

// 📄 6. SWAGGER (JWT BEARER DESTEKLİ)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AtlasWeb API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token'ınızı 'Bearer <token>' formatında girin."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// --- 🚀 7. [DATABASE INITIALIZATION & SEED DATA] ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AtlasDbContext>();
        Log.Information("--> [DATABASE] Migration uygulanıyor...");

        await context.Database.MigrateAsync();

        var adminEmail = "akkusomer0742@gmail.com";
        var requestedPass = "Omer.01742_";
        var admin = await context.Kullanicilar.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.EPosta == adminEmail);

        if (admin == null)
        {
            admin = new Kullanici
            {
                Id = AtlasWeb.Services.IdGenerator.CreateV7(),
                Ad = "Muhammed Ömer",
                Soyad = "Akkuş",
                EPosta = adminEmail,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(requestedPass),
                Rol = KullaniciRol.Admin,
                MusteriId = AtlasDbContext.SystemMusteriId,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            };

            context.Kullanicilar.Add(admin);
            await context.SaveChangesAsync();
            Log.Information("--> [SEED] İlk Admin ({Email}) başarıyla oluşturuldu.", admin.EPosta);
        }
        else 
        {
            // Eğer varsa şifreyi ve rolü güncelleyelim (Kullanıcı talebi doğrultusunda)
            admin.Rol = KullaniciRol.Admin;
            admin.SifreHash = BCrypt.Net.BCrypt.HashPassword(requestedPass);
            await context.SaveChangesAsync();
            Log.Information("--> [SEED] Mevcut Admin ({Email}) bilgileri başarıyla güncellendi.", admin.EPosta);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "--> [ERROR] Veritabanı başlatılırken hata oluştu!");
    }
}

// 🛡️ 8. MIDDLEWARE PIPELINE (Sıralama Kritik!)
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityCircuitBreaker>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AtlasWeb API V1");
});

app.UseCors("AtlasWebCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();