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

namespace SteamStorageAPI.Utilities.Extensions;

public static partial class ServiceCollectionExtensions
{
    #region Methods

    public static IServiceCollection AddDomainServices(
        this IServiceCollection services)
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

    #endregion Methods
}