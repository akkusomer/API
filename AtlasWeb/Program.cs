using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AtlasWeb.Data;
using AtlasWeb.Middlewares;
using AtlasWeb.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/security-audit-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = BuildConnectionString(builder.Configuration);
var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddDbContext<AtlasDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection().SetApplicationName("AtlasWeb");
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IHksAyarService, HksAyarService>();
builder.Services.AddScoped<IHksSifatService, HksSifatService>();
builder.Services.AddScoped<IHksIlService, HksIlService>();
builder.Services.AddScoped<IHksIlceService, HksIlceService>();
builder.Services.AddScoped<IHksBeldeService, HksBeldeService>();
builder.Services.AddScoped<IHksUrunService, HksUrunService>();
builder.Services.AddScoped<IHksUrunBirimService, HksUrunBirimService>();
builder.Services.AddScoped<IHksIsletmeTuruService, HksIsletmeTuruService>();
builder.Services.AddScoped<IHksUretimSekliService, HksUretimSekliService>();
builder.Services.AddScoped<IHksUrunCinsiService, HksUrunCinsiService>();
builder.Services.AddScoped<IHksReferansKunyeKayitService, HksReferansKunyeKayitService>();
builder.Services.Configure<HksOptions>(builder.Configuration.GetSection(HksOptions.SectionName));
builder.Services.AddHttpClient<IHksService, HksService>();
builder.Services.AddHostedService<HksReferansKunyeQueueWorker>();
builder.Services.AddControllers();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT key yapilandirilmamis.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("JWT key en az 32 byte olmalidir.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string>("LoginPolicy", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: BuildRateLimitPartitionKey(context, "login"),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                PermitLimit = 5,
                QueueLimit = 0
            }));

    options.AddPolicy<string>("RefreshPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildRateLimitPartitionKey(context, "refresh"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 30,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy<string>("ForgotPasswordPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildRateLimitPartitionKey(context, "forgot-password"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 3,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy<string>("PasswordResetPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: BuildRateLimitPartitionKey(context, "password-reset"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 10,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:GlobalPermitLimit", 200),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { hata = "Cok fazla istek gonderildi." },
            token);
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AtlasWebCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 1;
    options.RequireHeaderSymmetry = false;
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AtlasWeb API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await AtlasDbInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Features:EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AtlasWeb API V1");
    });
}

if (allowedOrigins.Length > 0)
{
    app.UseCors("AtlasWebCors");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SecurityCircuitBreaker>();

app.MapControllers();

app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var configured = configuration.GetConnectionString("DefaultConnection");
    var password = configuration["Database:Password"]
        ?? Environment.GetEnvironmentVariable("ATLASWEB_DB_PASSWORD")
        ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

    if (!string.IsNullOrWhiteSpace(configured))
    {
        var builder = new NpgsqlConnectionStringBuilder(configured);

        if (string.IsNullOrWhiteSpace(builder.Password) && !string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
        }

        if (string.IsNullOrWhiteSpace(builder.Host)
            || string.IsNullOrWhiteSpace(builder.Database)
            || string.IsNullOrWhiteSpace(builder.Username)
            || string.IsNullOrWhiteSpace(builder.Password))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection eksik veya guvensiz.");
        }

        return builder.ConnectionString;
    }

    var host = configuration["Database:Host"];
    var database = configuration["Database:Name"];
    var username = configuration["Database:User"];

    if (string.IsNullOrWhiteSpace(host)
        || string.IsNullOrWhiteSpace(database)
        || string.IsNullOrWhiteSpace(username)
        || string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException("Veritabani baglantisi tanimli degil.");
    }

    return new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = configuration.GetValue("Database:Port", 5432),
        Database = database,
        Username = username,
        Password = password
    }.ConnectionString;
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    return configuration.GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()
        ?? Array.Empty<string>();
}

static string BuildRateLimitPartitionKey(HttpContext context, string policyName)
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"{policyName}:{ip}";
}
