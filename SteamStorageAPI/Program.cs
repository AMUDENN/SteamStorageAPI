using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Middlewares;
using SteamStorageAPI.Utilities.JWT;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Services.Infrastructure.JwtProvider;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.ExceptionHandlers;
using SteamStorageAPI.Utilities.Extensions;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

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
        string? configPath = Environment.GetEnvironmentVariable("CONFIG_PATH") ?? ".config.yaml";
        if (configPath is null) throw new Exception("Path to configuration file not set");

        AppConfig config = ConfigurationReader.Read(configPath);

        builder.Services.AddSingleton(config);


        JwtOptions jwtOptions = new(config);
        builder.Services.AddSingleton<JwtOptions>(jwtOptions);

        //SteamAPI Service
        builder.Services.AddSingleton<ISteamApiUrlBuilder, SteamApiUrlBuilder>();


        //Services
        builder.Services.AddScoped<IJwtProvider, JwtProvider>();
        builder.Services.AddTransient<IContextUserService, ContextUserService>();

        // Domain services
        builder.Services.AddDomainServices();

        //Quartz
        builder.Services.AddQuartzServices(config);

        //Background Services
        builder.Services.AddBackgroundServices();

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

        //HealthCheck
        builder.Services.AddHealthChecksServices(config);

        builder.Services
            .AddHealthChecksUI(setup => {
                setup.AddHealthCheckEndpoint("Health details", $"{config.HealthChecks.BaseUrl}/api/health-all");
                setup.AddHealthCheckEndpoint("SteamStorageAPI", $"{config.HealthChecks.BaseUrl}/api/health-api");
                setup.AddHealthCheckEndpoint("DataBase", $"{config.HealthChecks.BaseUrl}/api/health-db");
                setup.AddHealthCheckEndpoint("Steam", $"{config.HealthChecks.BaseUrl}/api/health-steam");
                setup.MaximumHistoryEntriesPerEndpoint(config.HealthChecks.MaximumHistoryEntriesPerEndpoint);
                setup.SetEvaluationTimeInSeconds(config.HealthChecks.EvaluationTimeInSeconds);
                setup.SetMinimumSecondsBetweenFailureNotifications(config.HealthChecks
                    .MinimumSecondsBetweenFailureNotifications);
            })
            .AddSqlServerStorage(config.Database.HealthChecks);

        builder.Services.AddMemoryCache();

        //RateLimit
        builder.Services.Configure<IpRateLimitOptions>(options => {
            options.EnableEndpointRateLimiting = config.RateLimit.EnableEndpointRateLimiting;
            options.StackBlockedRequests = config.RateLimit.StackBlockedRequests;
            options.HttpStatusCode = config.RateLimit.HttpStatusCode;
            options.RealIpHeader = config.RateLimit.RealIpHeader;
            options.ClientIdHeader = config.RateLimit.ClientIdHeader;
            options.GeneralRules = config.RateLimit.Rules
                .Select(r => new AspNetCoreRateLimit.RateLimitRule
                    { Endpoint = r.Endpoint, Period = r.Period, Limit = r.Limit })
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


        app.UseSwagger(swaggerOptions => {
            swaggerOptions.RouteTemplate = "api/swagger/{documentname}/swagger.json";
        });

        app.UseSwaggerUI(swaggerUiOptions => {
            swaggerUiOptions.RoutePrefix = "api/swagger";
        });

        //ForwardedHeaders
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/api"
        });

        // HealthChecks
        app.MapHealthChecks("/api/health", CreateHealthCheckOptions(reg => !reg.Tags.Contains("steam")));

        app.MapHealthChecks("/api/health-api", CreateHealthCheckOptions(reg => reg.Tags.Contains("api")));

        app.MapHealthChecks("/api/health-db", CreateHealthCheckOptions(reg => reg.Tags.Contains("db")));

        app.MapHealthChecks("/api/health-all", CreateHealthCheckOptions(_ => true))
            .RequireAuthorization();

        app.MapHealthChecks("/api/health-steam", CreateHealthCheckOptions(reg => reg.Tags.Contains("steam")))
            .RequireAuthorization();

        app.MapHealthChecksUI(options => {
            options.UIPath = "/api/health-ui";
            options.ApiPath = "/api";
            options.ResourcesPath = "/api";
            options.UseRelativeApiPath = false;
            options.UseRelativeResourcesPath = false;
            options.UseRelativeWebhookPath = false;
            options.AddCustomStylesheet("wwwroot/ui/css/health-ui-style.css");
        });

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