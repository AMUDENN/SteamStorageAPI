﻿using SteamStorageAPI.Services.BackgroundServices.Tools;
using SteamStorageAPI.Services.RefreshCurrenciesService;

namespace SteamStorageAPI.Services.BackgroundServices;

public class RefreshCurrenciesBackgroundService : BackgroundServiceBase
{
    #region Fields

    private readonly ILogger<RefreshCurrenciesBackgroundService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshCurrenciesBackgroundService(
        ILogger<RefreshCurrenciesBackgroundService> logger,
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
                _logger.LogInformation("Начинается обновление курса валют");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshCurrenciesService refreshCurrenciesService =
                        scope.ServiceProvider.GetRequiredService<IRefreshCurrenciesService>();

                    await refreshCurrenciesService.RefreshCurrenciesAsync(stoppingToken);
                }

                _logger.LogInformation("Обновление курса валют завершено");

                await Task.Delay(24 * 60 * 60 * 1000, stoppingToken);
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
