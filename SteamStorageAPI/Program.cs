using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Middlewares;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;
using SteamStorageAPI.Utilities.JWT;
using System.Net.Mime;
using System.Text.Json.Serialization;
using SteamStorageAPI.Services.CryptographyService;
using SteamStorageAPI.Services.JwtProvider;

namespace SteamStorageAPI;

public static class Program
{
    #region Records

    private record HealthResponse(string Status, TimeSpan Duration, IEnumerable<MonitorResponse> Monitors);

    private record MonitorResponse(string Name, string? Status);

    #endregion Records

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
        string connectionString = builder.Configuration.GetConnectionString("SteamStorageDB")
                                  ?? throw new ArgumentNullException("ConnectionString");

        builder.Services.AddDbContext<SteamStorageContext>(opt => opt.UseSqlServer(connectionString));

        //HealthCheck
        builder.Services.AddHealthChecks()
            .AddSqlServer(connectionString)
            .AddCheck<ApiHealthChecker>(nameof(ApiHealthChecker));

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
                    ValidIssuer = JwtOptions.ISSUER,
                    ValidateAudience = true,
                    ValidAudience = JwtOptions.AUDIENCE,
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

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health-details",
            new()
            {
                ResponseWriter = async (context, report) =>
                {
                    string result = JsonConvert.SerializeObject(
                        new HealthResponse(
                            report.Status.ToString(),
                            report.TotalDuration,
                            report.Entries.Select(e =>
                                new MonitorResponse(e.Key, Enum.GetName(typeof(HealthStatus), e.Value.Status)))
                        )
                    );
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(result);
                }
            }
        );

        app.UseIpRateLimiting();

        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseHttpsRedirection();

        app.MapControllers();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Run();
    }

    #endregion Methods
}
