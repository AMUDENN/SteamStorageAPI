using SteamStorageAPI.Services.RefreshCurrenciesService;

namespace SteamStorageAPI.Services.BackgroundServices;

public class RefreshCurrenciesBackgroundService : BackgroundService
{
    private readonly ILogger<RefreshCurrenciesBackgroundService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RefreshCurrenciesBackgroundService(
        ILogger<RefreshCurrenciesBackgroundService> logger,
        IHostApplicationLifetime lifetime,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceScopeFactory = serviceScopeFactory;
    }

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

                    await refreshCurrenciesService.RefreshCurrencies(stoppingToken);
                }
                
                _logger.LogInformation("Обновление курса валют завершено");

                await Task.Delay(24 * 60 * 60 * 1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обновлении курса валют: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime,
        CancellationToken stoppingToken)
    {
        TaskCompletionSource startedSource = new();
        await using CancellationTokenRegistration reg1 =
            lifetime.ApplicationStarted.Register(() => startedSource.SetResult());

        TaskCompletionSource cancelledSource = new();
        await using CancellationTokenRegistration reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        Task completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);

        return completedTask == startedSource.Task;
    }
}
