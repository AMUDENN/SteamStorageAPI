using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Middlewares;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.JWT;
using System.Text.Json.Serialization;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SteamStorageAPI.Services.CryptographyService;
using SteamStorageAPI.Services.JwtProvider;
using SteamStorageAPI.Utilities.HealthCheckers;

namespace SteamStorageAPI;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
        builder.Services.AddEndpointsApiExplorer();

        //JwtOptions Initialize
        JwtOptions.Initialize(builder.Configuration);

        //Services
        builder.Services.AddScoped<IJwtProvider, JwtProvider>();
        builder.Services.AddScoped<ICryptographyService, CryptographyService>();
        builder.Services.AddTransient<ISkinService, SkinService>();
        builder.Services.AddTransient<IUserService, UserService>();

        //Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "SteamStorage API",
                Version = "v1",
                Description = "API для SteamStorage"
            });
            c.AddSecurityDefinition("Bearer", new()
            {
                In = ParameterLocation.Header,
                Description = "Авторизация происходит в формате: Bearer {token}",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();


        //DataBase
        string connectionStringSteamStorage =
            builder.Configuration.GetSection("DataBase").GetValue<string>("SteamStorage")
            ?? throw new ArgumentNullException("MainConnectionString");

        string connectionStringHealthChecks =
            builder.Configuration.GetSection("DataBase").GetValue<string>("SteamStorageHealthChecks")
            ?? throw new ArgumentNullException("HealthChecksConnectionString");

        builder.Services.AddDbContext<SteamStorageContext>(opt => opt.UseSqlServer(connectionStringSteamStorage));

        //HealthCheck
        builder.Services.AddHealthChecks()
            .AddSqlServer(name: "SteamStorageDB", connectionString: connectionStringSteamStorage,
                tags: new[] { "db", "database" })
            .AddSqlServer(name: "SteamStorageHealthChecksDB", connectionString: connectionStringHealthChecks,
                tags: new[] { "db", "database" })
            .AddDbContextCheck<SteamStorageContext>(name: nameof(SteamStorageContext), tags: new[] {"db", "db-context"})
            .AddCheck<ApiHealthChecker>(name: nameof(ApiHealthChecker), tags: new[] { "api" })
            .AddCheck<SteamMarketHealthChecker>(name: nameof(SteamMarketHealthChecker), tags: new[] { "steam" })
            .AddCheck<SteamProfileHealthChecker>(name: nameof(SteamProfileHealthChecker), tags: new[] { "steam" });

        builder.Services.AddHealthChecksUI(setup =>
            {
                setup.MaximumHistoryEntriesPerEndpoint(50);
                setup.SetEvaluationTimeInSeconds(600);
                setup.SetMinimumSecondsBetweenFailureNotifications(300);
            })
            .AddSqlServerStorage(connectionStringHealthChecks);

        builder.Services.AddMemoryCache();

        //RateLimit
        builder.Services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.RealIpHeader = "X-Real-IP";
            options.ClientIdHeader = "X-ClientId";
            options.GeneralRules =
            [
                new()
                {
                    Endpoint = "*",
                    Period = "1s",
                    Limit = 20,
                }
            ];
        });
        builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        builder.Services.AddInMemoryRateLimiting();

        //Authorization
        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = JwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtOptions.GetSymmetricSecurityKey()
                };
            });

        return builder;
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(WebApplication.CreateBuilder(args));

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // HealthChecks
        app.MapHealthChecks("/health", CreateHealthCheckOptions(_ => true));
        app.MapHealthChecks("/health-db", CreateHealthCheckOptions(reg => reg.Tags.Contains("db")));
        app.MapHealthChecks("/health-api", CreateHealthCheckOptions(reg => reg.Tags.Contains("api")));
        app.MapHealthChecks("/health-steam", CreateHealthCheckOptions(reg => reg.Tags.Contains("steam")));

        app.MapHealthChecksUI(u => u.UIPath = "/health-ui");

        // RateLimit
        app.UseIpRateLimiting();

        //Middlewares
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseHttpsRedirection();

        app.MapControllers();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Run();
    }

    private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new()
        {
            Predicate = predicate,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        };
    }

    #endregion Methods
}
