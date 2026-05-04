using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SEOOptimiser.API.Authentication;
using SEOOptimiser.Core.Interfaces;
using SEOOptimiser.Core.UseCases.Sessions.Commands;
using SEOOptimiser.Infrastructure.Persistence;
using SEOOptimiser.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Configuration validation ───────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic:ApiKey not configured.");
var authBypass = builder.Configuration.GetValue<bool>("Auth:Bypass");
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
        anthropicApiKey));

// ── 5. MediatR ────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateSessionCommand).Assembly));

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
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── 9. Auto-migrate database on startup ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── 10. Middleware pipeline ───────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "SEO Optimiser API v1");
    opts.RoutePrefix = "swagger";
    opts.DisplayRequestDuration();
    opts.DefaultModelsExpandDepth(-1); // collapse schema section by default
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
