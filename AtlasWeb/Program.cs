using AtlasWeb.Data;
using AtlasWeb.Services;
using AtlasWeb.Middlewares;
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

// 🛡️ STRUCTURED LOGGING
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/security-audit-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<AtlasDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();

// 🛡️ JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT Key yapılandırılmamış. 'Jwt:Key' appsettings veya JWT_SECRET_KEY ortam değişkeni ile tanımlanmalıdır.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// 🛡️ FluentValidation
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 🛡️ Rate Limiting - LoginPolicy tanımı
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.PermitLimit = 10; // 5 dakikada maksimum 10 giriş denemesi
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// 🌐 CORS - Frontend entegrasyonu için
builder.Services.AddCors(options =>
{
    options.AddPolicy("AtlasWebCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",  // React / Next.js geliştirme ortamı
                "http://localhost:5173",  // Vite geliştirme ortamı
                "http://localhost:4200",  // Angular geliştirme ortamı
                "http://127.0.0.1:5500"  // VS Code Live Server
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 🔒 Dağıtık Önbellek (Redis için yorum satırı açılabilir)
// builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = "localhost:6379"; });
builder.Services.AddDistributedMemoryCache();

// 📄 Swagger - JWT Bearer token desteği ile
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AtlasWeb API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer", BearerFormat = "JWT",
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

// 🛡️ SAVUNMA KATMANLARI (Sıralama Kritik!)
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityCircuitBreaker>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AtlasWeb API V1");
});

app.UseCors("AtlasWebCors"); // 🌐 CORS aktif

app.UseRateLimiter(); // 🛡️ Rate Limiter aktif

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();