using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.Middlewares;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Services.Infrastructure.JwtProvider;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.ExceptionHandlers;
using SteamStorageAPI.Utilities.Extensions;
using SteamStorageAPI.Utilities.HealthCheck.Tools;
using SteamStorageAPI.Utilities.JWT;
using RateLimitRule=AspNetCoreRateLimit.RateLimitRule;

namespace SteamStorageAPI;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        //Controllers
        builder.Services
            .AddControllers(options => { options.AddAutoValidation(); })
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        builder.Services.AddEndpointsApiExplorer();


        // Initialize config
        string? configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
        if (configPath is null) throw new Exception("Path to configuration file not set");

        AppConfig config = ConfigurationReader.Read(configPath);

        builder.Services.AddSingleton(config);


        JwtOptions jwtOptions = new(config);
        builder.Services.AddSingleton(jwtOptions);

        //SteamAPI Service
        builder.Services.AddSingleton<ISteamApiUrlBuilder, SteamApiUrlBuilder>();


        //Services
        builder.Services.AddScoped<IJwtProvider, JwtProvider>();
        builder.Services.AddScoped<IContextUserService, ContextUserService>();

        // Domain services
        builder.Services.AddDomainServices();

        //Quartz
        builder.Services.AddQuartzServices(config);

        //Swagger
        builder.Services.AddSwagger();

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();


        //DataBase
        builder.Services.AddDbContext<SteamStorageContext>(options =>
            options.UseSqlServer(config.Database.SteamStorage));

        //ExceptionHandlers
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        //Telemetry
        builder.Services.AddTelemetry();

        //HealthCheck
        builder.Services.AddHealthChecksServices(config);

        builder.Services.AddMemoryCache();

        //RateLimit
        builder.Services.Configure<IpRateLimitOptions>(options => {
            options.EnableEndpointRateLimiting = config.RateLimit.EnableEndpointRateLimiting;
            options.StackBlockedRequests = config.RateLimit.StackBlockedRequests;
            options.HttpStatusCode = config.RateLimit.HttpStatusCode;
            options.RealIpHeader = config.RateLimit.RealIpHeader;
            options.ClientIdHeader = config.RateLimit.ClientIdHeader;
            options.IpWhitelist = config.RateLimit.IpWhiteList;
            options.GeneralRules = config.RateLimit.Rules
                .Select(r => new RateLimitRule
                {
                    Endpoint = r.Endpoint,
                    Period = r.Period,
                    Limit = r.Limit
                })
                .ToList();
        });
        builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        builder.Services.AddInMemoryRateLimiting();

        //Authorization
        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwtOptions.GetSymmetricSecurityKey()
                };
            });

        //WebRoot
        builder.WebHost.UseWebRoot("wwwroot");

        return builder;
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(WebApplication.CreateBuilder(args));

        WebApplication app = builder.Build();


        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(swaggerOptions => {
                swaggerOptions.RouteTemplate = "api/swagger/{documentname}/swagger.json";
            });

            app.UseSwaggerUI(swaggerUiOptions => {
                swaggerUiOptions.RoutePrefix = "api/swagger";
            });
        }

        //ForwardedHeaders
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/api"
        });

        // Prometheus metrics
        string metricsBearer = $"Bearer {app.Services.GetRequiredService<AppConfig>().App.InternalApiKey}";
        app.MapPrometheusScrapingEndpoint("/api/metrics")
            .AddEndpointFilter(async (ctx, next) => {
                string? authHeader = ctx.HttpContext.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader == metricsBearer)
                    return await next(ctx);
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return null;
            });

        // HealthChecks
        app.MapHealthChecks("/api/health-all", CreateHealthCheckOptions(_ => true))
            .RequireAuthorization();
        app.MapHealthChecks("/api/health-api", CreateHealthCheckOptions(r => r.Tags.Contains("api")))
            .RequireAuthorization();
        app.MapHealthChecks("/api/health-db", CreateHealthCheckOptions(r => r.Tags.Contains("db")))
            .RequireAuthorization();
        app.MapHealthChecks("/api/health-steam", CreateHealthCheckOptions(r => r.Tags.Contains("steam")))
            .RequireAuthorization();

        // RateLimit
        app.UseIpRateLimiting();

        //ExceptionHandler
        app.UseExceptionHandler();

        //Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        //Middlewares
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.MapControllers();

        app.Run();
    }

    private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        };
    }

    #endregion Methods
}