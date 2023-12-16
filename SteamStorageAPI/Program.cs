using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Middlewares;
using SteamStorageAPI.Utilities;
using SteamStorageAPI.Utilities.JWT;
using System.Net.Mime;

namespace SteamStorageAPI;

public class Program
{
    #region Records
    public record HealthResponse(string Status, TimeSpan Duration, IEnumerable<MonitorResponse> Monitors);
    public record MonitorResponse(string Name, string? Status);
    #endregion Records

    #region Methods
    private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        //Services
        builder.Services.AddScoped<IJwtProvider, JwtProvider>();

        //Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SteamStorage API",
                Version = "v1",
                Description = "API для SteamStorage"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Авторизация происходит в формате: Bearer {token}",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
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
                }});
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
                .AddCheck<APIHealthChecker>(nameof(APIHealthChecker));

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
                        Limit = 5,
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
                options.TokenValidationParameters = new TokenValidationParameters
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
        var builder = ConfigureServices(WebApplication.CreateBuilder(args));

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health-details",
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonConvert.SerializeObject(
                        new HealthResponse(
                            report.Status.ToString(),
                            report.TotalDuration,
                            report.Entries.Select(e => new MonitorResponse(e.Key, Enum.GetName(typeof(HealthStatus), e.Value.Status)))
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
