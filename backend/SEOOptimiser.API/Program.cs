using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SEOOptimiser.API.Authentication;
using SEOOptimiser.API.Middleware;
using SEOOptimiser.API.Pipeline;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Core.UseCases.Sessions.Commands;
using SEOOptimiser.Infrastructure.Persistence;
using SEOOptimiser.Infrastructure.Services;
using SEOOptimiser.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Configuration validation ───────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic:ApiKey not configured.");
var authBypass = builder.Configuration.GetValue<bool>("Auth:Bypass");
var useFakePage = builder.Configuration.GetValue<bool>("PageFetcher:UseFake");
var clerkAuthority = authBypass
    ? null
    : builder.Configuration["Clerk:Authority"]
        ?? throw new InvalidOperationException("Clerk:Authority not configured.");
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

// ── 2. Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connectionString));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ── 3. Named HTTP client for page fetching ────────────────────────────────────
builder.Services.AddHttpClient("PageFetcher", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (compatible; SEOOptimiserBot/1.0)");
});

// ── 4. AI Agent service ───────────────────────────────────────────────────────
builder.Services.AddSingleton<IAgentService>(sp =>
    new SeoAgentService(
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<ILogger<SeoAgentService>>(),
        anthropicApiKey,
        fakePageHtml: useFakePage ? SeoAgentService.FakePageHtml : null));

// ── 5. MediatR ────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateSessionCommand).Assembly);
    cfg.AddOpenBehavior(typeof(TelemetryBehavior<,>));
});

// ── 6. Authentication ─────────────────────────────────────────────────────────
if (authBypass)
{
    builder.Services
        .AddAuthentication(DevBypassAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>(
            DevBypassAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.Authority = clerkAuthority;
            opts.TokenValidationParameters = new()
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });
}
builder.Services.AddAuthorization();

// ── 7. Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SEO Optimiser API",
        Version = "v1",
        Description = """
            AI-powered SEO tag analysis and optimisation API.

            The assistant fetches a given URL, audits its HTML tags (title, meta description, H1),
            and returns structured optimisation suggestions. Conversations are persisted per session
            so context is preserved across requests.

            **Auth**: All endpoints require a Clerk-issued JWT Bearer token unless
            `Auth:Bypass` is enabled (development only).
            """
    });

    // Pull XML doc comments from both API and Core assemblies
    foreach (var xmlFile in new[] { "SEOOptimiser.API.xml", "SEOOptimiser.Core.xml" })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            opts.IncludeXmlComments(xmlPath);
    }

    if (!authBypass)
    {
        opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste your Clerk JWT token (without the 'Bearer ' prefix)."
        });
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                []
            }
        });
    }
});

// ── 8. Controllers + CORS ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── 9. Rate limiting ──────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global: 100 requests/min per IP across all endpoints
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window      = TimeSpan.FromMinutes(1),
            QueueLimit  = 0,
        });
    });

    // Named: 30 messages/min per authenticated user (applied via [EnableRateLimiting] on the action)
    opts.AddPolicy("SendMessage", ctx =>
    {
        var userId = ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? ctx.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window      = TimeSpan.FromMinutes(1),
            QueueLimit  = 0,
        });
    });

    opts.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            title  = "Too Many Requests",
            detail = "Rate limit exceeded. Please wait before sending another request."
        }, ct);
    };
});

// ── 10. OpenTelemetry ─────────────────────────────────────────────────────────
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SEOOptimiser.API"))
    .WithTracing(tracing => tracing
        .AddSource(SeoTelemetry.SourceName)
        .AddAspNetCoreInstrumentation(opts =>
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/swagger"))
        .AddHttpClientInstrumentation(opts =>
            opts.FilterHttpRequestMessage = req =>
                req.RequestUri?.Host.Contains("api.anthropic.com") != true)
        .AddEntityFrameworkCoreInstrumentation(opts =>
            opts.SetDbStatementForText = true)
        .AddOtlpExporter(opts =>
        {
            if (!string.IsNullOrEmpty(otlpEndpoint))
                opts.Endpoint = new Uri(otlpEndpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts =>
        {
            if (!string.IsNullOrEmpty(otlpEndpoint))
                opts.Endpoint = new Uri(otlpEndpoint);
        }));

if (!string.IsNullOrEmpty(otlpEndpoint))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeScopes = true;
        logging.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
    });
}

// ── 11. Request body size limit ───────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(opts => opts.Limits.MaxRequestBodySize = 32 * 1024); // 32 KB

var app = builder.Build();

// ── 9. Auto-migrate database on startup ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── 11. Middleware pipeline ───────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "SEO Optimiser API v1");
    opts.RoutePrefix = "swagger";
    opts.DisplayRequestDuration();
    opts.DefaultModelsExpandDepth(-1);
});

app.UseSecurityHeaders();
if (app.Environment.IsProduction())
    app.UseHsts();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
