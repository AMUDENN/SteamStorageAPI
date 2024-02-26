using SteamStorageAPI.Services.BackgroundServices.Tools;
using SteamStorageAPI.Services.RefreshSkinDynamicsService;

namespace SteamStorageAPI.Services.BackgroundServices;

public class RefreshSkinDynamicsBackgroundService : BackgroundServiceBase
{
    #region Fields

    private readonly ILogger<RefreshSkinDynamicsBackgroundService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshSkinDynamicsBackgroundService(
        ILogger<RefreshSkinDynamicsBackgroundService> logger,
        IHostApplicationLifetime lifetime,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion Constructor

    #region Methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Начинается обновление стоимости предметов");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshSkinDynamicsService refreshSkinDynamicsService =
                        scope.ServiceProvider.GetRequiredService<IRefreshSkinDynamicsService>();

                    await refreshSkinDynamicsService.RefreshSkinDynamicsAsync(stoppingToken);
                }

                _logger.LogInformation("Обновление стоимости предметов завершено");

                await Task.Delay(60 * 60 * 1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обновлении курса валют: {ex.Message}");

                await Task.Delay(30 * 60 * 1000, stoppingToken);
            }
        }
    }

    #endregion Methods
}
