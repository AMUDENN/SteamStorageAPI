using SteamStorageAPI.Services.BackgroundServices.Tools;
using SteamStorageAPI.Services.RefreshActiveDynamicsService;

namespace SteamStorageAPI.Services.BackgroundServices;

public class RefreshActiveDynamicsBackgroundService : BackgroundServiceBase
{
    #region Fields

    private readonly ILogger<RefreshActiveDynamicsBackgroundService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshActiveDynamicsBackgroundService(
        ILogger<RefreshActiveDynamicsBackgroundService> logger,
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
                _logger.LogInformation("Начинается обновление записей ActiveDynamics");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshActiveDynamicsService refreshActiveDynamicsService =
                        scope.ServiceProvider.GetRequiredService<IRefreshActiveDynamicsService>();

                    await refreshActiveDynamicsService.RefreshActiveDynamicsAsync(stoppingToken);
                }

                _logger.LogInformation("Обновление ActiveDynamics завершено");

                await Task.Delay(24 * 60 * 60 * 1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обновлении ActiveDynamics: {ex.Message}");

                await Task.Delay(30 * 60 * 1000, stoppingToken);
            }
        }
    }

    #endregion Methods
}
