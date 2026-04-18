using System.Reflection;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Quartz;
using SteamStorageAPI.Services.Background.QuartzJobs;
using SteamStorageAPI.Services.Background.RefreshActiveDynamicsService;
using SteamStorageAPI.Services.Background.RefreshCurrenciesService;
using SteamStorageAPI.Services.Background.RefreshSkinDynamicsService;
using SteamStorageAPI.Services.Domain.ActiveGroupService;
using SteamStorageAPI.Services.Domain.ActiveService;
using SteamStorageAPI.Services.Domain.ArchiveGroupService;
using SteamStorageAPI.Services.Domain.ArchiveService;
using SteamStorageAPI.Services.Domain.AuthorizeService;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Domain.FileService;
using SteamStorageAPI.Services.Domain.GameService;
using SteamStorageAPI.Services.Domain.InventoryService;
using SteamStorageAPI.Services.Domain.PageService;
using SteamStorageAPI.Services.Domain.RoleService;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Services.Domain.StatisticsService;
using SteamStorageAPI.Services.Domain.UserService;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.HealthCheck;
using SteamStorageAPI.Utilities.HealthCheck.Steam;

namespace SteamStorageAPI.Utilities.Extensions;

public static partial class ServiceCollectionExtensions
{
    #region Methods

    extension(IServiceCollection services)
    {
        public IServiceCollection AddDomainServices()
        {
            services.AddScoped<IArchiveService, ArchiveService>();
            services.AddScoped<IActiveService, ActiveService>();
            services.AddScoped<IActiveGroupService, ActiveGroupService>();
            services.AddScoped<IArchiveGroupService, ArchiveGroupService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPageService, PagesService>();
            services.AddScoped<IFileService, FileService>();


            services.AddScoped<ISkinService, SkinService>();
            services.AddScoped<ICurrencyService, CurrencyService>();
            services.AddScoped<IAuthorizeService, AuthorizeService>();

            return services;
        }

        public IServiceCollection AddQuartzServices(AppConfig config)
        {
            services.AddScoped<IRefreshSkinDynamicsService, RefreshSkinDynamicsService>();
            services.AddScoped<IRefreshActiveGroupDynamicsService, RefreshActiveGroupDynamicsService>();
            services.AddScoped<IRefreshCurrenciesService, RefreshCurrenciesService>();

            services.AddQuartz(q => {
                q.AddJob<RefreshSkinDynamicsJob>(j => j.WithIdentity(nameof(RefreshSkinDynamicsJob)));
                q.AddJob<RefreshCurrenciesJob>(j => j.WithIdentity(nameof(RefreshCurrenciesJob)));
                q.AddJob<RefreshActiveGroupsDynamicsJob>(j => j.WithIdentity(nameof(RefreshActiveGroupsDynamicsJob)));

                q.AddTrigger(t => t
                    .ForJob(nameof(RefreshSkinDynamicsJob))
                    .WithIdentity(nameof(RefreshSkinDynamicsJob) + "Trigger")
                    .WithCronSchedule(config.BackgroundServices.RefreshSkinDynamicsJob.CronSchedule));

                q.AddTrigger(t => t
                    .ForJob(nameof(RefreshCurrenciesJob))
                    .WithIdentity(nameof(RefreshCurrenciesJob) + "Trigger")
                    .WithCronSchedule(config.BackgroundServices.RefreshCurrencies.CronSchedule));

                q.AddTrigger(t => t
                    .ForJob(nameof(RefreshActiveGroupsDynamicsJob))
                    .WithIdentity(nameof(RefreshActiveGroupsDynamicsJob) + "Trigger")
                    .WithCronSchedule(config.BackgroundServices.RefreshActiveGroupsDynamicsJob.CronSchedule));
            });

            services.AddHostedService<QuartzHostedService>();

            return services;
        }

        public IServiceCollection AddSwagger()
        {
            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SteamStorage API",
                    Version = "v1",
                    Description = "API for SteamStorage"
                });

                options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Authorization: Bearer {token}"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("bearer", document)] = []
                });

                string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            return services;
        }

        public IServiceCollection AddTelemetry()
        {
            services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter());

            return services;
        }

        public IServiceCollection AddHealthChecksServices(AppConfig config)
        {
            services
                .AddHealthChecks()
                .AddSqlServer(
                    config.Database.SteamStorage,
                    name: "database",
                    tags: ["db"])
                .AddCheck<ApiHealthChecker>(
                    "api",
                    tags: ["api"])
                .AddCheck<AdminPanelHealthChecker>(
                    "adminpanel",
                    tags: ["adminpanel"])
                .AddCheck<LoginWebAppHealthChecker>(
                    "login",
                    tags: ["login"])
                .AddCheck<SteamMarketHealthChecker>(
                    "steam-market",
                    tags: ["steam"])
                .AddCheck<SteamProfileHealthChecker>(
                    "steam-profile",
                    tags: ["steam"]);

            return services;
        }
    }

    #endregion Methods
}