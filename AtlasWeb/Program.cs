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

// 🏗️ 2. VERİTABANI BAĞLANTISI (PostgreSQL)
builder.Services.AddDbContext<AtlasDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();

// 🛡️ 3. JWT AUTHENTICATION
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? "SeninCokGizliVeUzunJwtAnahtarin123!"; // Güvenlik için appsettings'e alınmalı

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
    options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
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
        Log.Information("--> [DATABASE] Tablolar kontrol ediliyor...");

        // ÖNEMLİ: Tablolar yoksa direkt modellerden oluşturur (Migration hatasını çözer)
        await context.Database.EnsureCreatedAsync();

        var adminVarMi = await context.Kullanicilar.AnyAsync(u => u.Rol == KullaniciRol.Admin);

        if (!adminVarMi)
        {
            var ilkAdmin = new Kullanici
            {
                Id = AtlasWeb.Services.IdGenerator.CreateV7(),
                Ad = "Muhammed Ömer",
                Soyad = "Akkuş",
                EPosta = "akkusomer0742@plostre.com",
                SifreHash = BCrypt.Net.BCrypt.HashPassword("Omer.0742_"),
                Rol = KullaniciRol.Admin,
                MusteriId = Guid.Empty,
                AktifMi = true,
                KayitTarihi = DateTime.UtcNow
            };

            context.Kullanicilar.Add(ilkAdmin);
            await context.SaveChangesAsync();
            Log.Information("--> [SEED] İlk Admin ({Email}) başarıyla oluşturuldu.", ilkAdmin.EPosta);
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