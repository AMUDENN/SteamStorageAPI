using System.Reflection;
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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Quartz;
using SteamStorageAPI.Services.BackgroundServices;
using SteamStorageAPI.Services.CryptographyService;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.JwtProvider;
using SteamStorageAPI.Services.QuartzJobs;
using SteamStorageAPI.Services.RefreshActiveDynamicsService;
using SteamStorageAPI.Services.RefreshCurrenciesService;
using SteamStorageAPI.Services.RefreshSkinDynamicsService;
using SteamStorageAPI.Utilities.ExceptionHandlers;
using SteamStorageAPI.Utilities.Extensions;
using SteamStorageAPI.Utilities.HealthCheck;
using SteamStorageAPI.Utilities.HealthCheck.Tools;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI;

public static class Program
{
    #region Methods

    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        //Controllers
        builder.Services
            .AddControllers(options => { options.AddAutoValidation(); })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        builder.Services.AddEndpointsApiExplorer();

        if (builder.Environment.IsDevelopment())
        {
            //JwtOptions Initialize
            JwtOptions.InitializeConfig(builder.Configuration);
            //SteamApi Initialize
            SteamApi.InitializeConfig(builder.Configuration);
        }
        else
        {
            //JwtOptions Initialize
            JwtOptions.InitializeEnvironmentVariables();
            //SteamApi Initialize
            SteamApi.InitializeEnvironmentVariables();
        }

        //Services
        builder.Services.AddScoped<IRefreshActiveGroupDynamicsService, RefreshActiveGroupDynamicsService>();
        builder.Services.AddScoped<IRefreshCurrenciesService, RefreshCurrenciesService>();
        builder.Services.AddScoped<IRefreshSkinDynamicsService, RefreshSkinDynamicsService>();

        builder.Services.AddScoped<IJwtProvider, JwtProvider>();
        builder.Services.AddScoped<ICryptographyService, CryptographyService>();
        builder.Services.AddTransient<ISkinService, SkinService>();
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<ICurrencyService, CurrencyService>();

        //Quartz
        builder.Services.AddQuartz(q =>
        {
            q.AddJob<RefreshCurrenciesJob>(j => j.WithIdentity(nameof(RefreshCurrenciesJob)));

            q.AddJob<RefreshActiveGroupsDynamicsJob>(j => j.WithIdentity(nameof(RefreshActiveGroupsDynamicsJob)));

            q.AddTrigger(t => t
                .ForJob(nameof(RefreshCurrenciesJob))
                .WithIdentity(nameof(RefreshCurrenciesJob) + "Trigger")
                .WithCronSchedule("0 0 1 * * ?")); // 01:00 Every Day

            q.AddTrigger(t => t
                .ForJob(nameof(RefreshActiveGroupsDynamicsJob))
                .WithIdentity(nameof(RefreshActiveGroupsDynamicsJob) + "Trigger")
                .WithCronSchedule("0 0 3 * * ?")); // 01:00 Every Day
        });

        //Background Services
        builder.Services.AddHostedService<RefreshSkinDynamicsBackgroundService>();
        builder.Services.AddHostedService<QuartzHostedService>();

        //Swagger
        builder.Services
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "SteamStorage API",
                    Version = "v1",
                    Description = "API для SteamStorage"
                });
                options.AddSecurityDefinition("Bearer", new()
                {
                    In = ParameterLocation.Header,
                    Description = "Авторизация происходит в формате: Bearer {token}",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });
                options.AddSecurityRequirement(new()
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

                string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                
                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();


        //DataBase
        string connectionStringSteamStorage;
        string connectionStringHealthChecks;
        if (builder.Environment.IsDevelopment())
        {
            //UserSecrets
            connectionStringSteamStorage = builder.Configuration.GetConnectionString("SteamStorage")
                                           ?? throw new ArgumentNullException(nameof(connectionStringSteamStorage));

            connectionStringHealthChecks = builder.Configuration.GetConnectionString("SteamStorageHealthChecks")
                                           ?? throw new ArgumentNullException(nameof(connectionStringHealthChecks));
        }
        else
        {
            //Environment
            connectionStringSteamStorage = Environment.GetEnvironmentVariable("SteamStorageDB")
                                           ?? throw new ArgumentNullException(nameof(connectionStringSteamStorage));

            connectionStringHealthChecks = Environment.GetEnvironmentVariable("SteamStorageHealthChecksDB")
                                           ?? throw new ArgumentNullException(nameof(connectionStringHealthChecks));
        }

        builder.Services.AddDbContext<SteamStorageContext>(
            options => options.UseSqlServer(connectionStringSteamStorage));

        //ExceptionHandlers
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        //HealthCheck
        builder.Services.AddHealthChecks()
            .AddSqlServer(name: "SteamStorageDB",
                connectionString: connectionStringSteamStorage,
                tags: new[] { "db", "database" })
            .AddSqlServer(name: "SteamStorageHealthChecksDB",
                connectionString: connectionStringHealthChecks,
                tags: new[] { "db", "database" })
            .AddDbContextCheck<SteamStorageContext>(name: nameof(SteamStorageContext),
                tags: new[] { "db", "db-context" })
            .AddCheck<ApiHealthChecker>(name: nameof(ApiHealthChecker),
                tags: new[] { "api" })
            .AddCheck<SteamMarketHealthChecker>(name: nameof(SteamMarketHealthChecker),
                tags: new[] { "steam" })
            .AddCheck<SteamProfileHealthChecker>(name: nameof(SteamProfileHealthChecker),
                tags: new[] { "steam" });

        builder.Services
            .AddHealthChecksUI(setup =>
            {
                setup.AddHealthCheckEndpoint("Health details", "https://steamstorage.ru/api/health-all");
                setup.AddHealthCheckEndpoint("SteamStorageAPI", "https://steamstorage.ru/api/health-api");
                setup.AddHealthCheckEndpoint("DataBase", "https://steamstorage.ru/api/health-db");
                setup.AddHealthCheckEndpoint("Steam", "https://steamstorage.ru/api/health-steam");
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
            options.RealIpHeader = "X-Forwarded-For";
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
        
        //WebRoot
        builder.WebHost.UseWebRoot("wwwroot");
        
        return builder;
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(WebApplication.CreateBuilder(args));

        WebApplication app = builder.Build();
        
        app.UseStaticFiles(new StaticFileOptions  
        {
            RequestPath = "/api"  
        });
        
        app.UseSwagger(swaggerOptions =>
        {
            swaggerOptions.RouteTemplate = "api/swagger/{documentname}/swagger.json";
        });
        app.UseSwaggerUI(swaggerUiOptions =>
        {
            swaggerUiOptions.RoutePrefix = "api/swagger";
        });

        //ForwardedHeaders
        app.UseForwardedHeaders(new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // HealthChecks
        app.MapHealthChecks("/api/health", CreateHealthCheckOptions(reg => !reg.Tags.Contains("steam")));

        app.MapHealthChecks("/api/health-api", CreateHealthCheckOptions(reg => reg.Tags.Contains("api")));

        app.MapHealthChecks("/api/health-db", CreateHealthCheckOptions(reg => reg.Tags.Contains("db")));

        app.MapHealthChecks("/api/health-all", CreateHealthCheckOptions(_ => true))
            .RequireAuthorization();

        app.MapHealthChecks("/api/health-steam", CreateHealthCheckOptions(reg => reg.Tags.Contains("steam")))
            .RequireAuthorization();

        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/api/health-ui";
            options.UseRelativeApiPath = false;
            options.UseRelativeResourcesPath = false;
            options.UseRelativeWebhookPath = false;
            options.AddCustomStylesheet("wwwroot/ui/css/health-ui-style.css");
        });

        // RateLimit
        app.UseIpRateLimiting();

        //Middlewares
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.MapControllers();

        //Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        //ExceptionHandler
        app.UseExceptionHandler();

        app.Run();
    }

    private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new()
        {
            Predicate = predicate,
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        };
    }

    #endregion Methods
}
