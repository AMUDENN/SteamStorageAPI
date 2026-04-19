using Quartz;
using SteamStorageAPI.Services.Background.RefreshCurrenciesService;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Services.Background.QuartzJobs;

public class RefreshCurrenciesJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshCurrenciesJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AppConfig _config;

    #endregion Fields

    #region Constructor

    public RefreshCurrenciesJob(
        ILogger<RefreshCurrenciesJob> logger,
        IServiceScopeFactory serviceScopeFactory,
        AppConfig config)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _config = config;
    }

    #endregion Constructor

    #region Methods

    public async Task Execute(
        IJobExecutionContext context)
    {
        bool isSuccessful = false;

        while (!isSuccessful && !context.CancellationToken.IsCancellationRequested)
            try
            {
                _logger.LogInformation("Currency exchange rate update is starting");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshCurrenciesService refreshCurrenciesService =
                        scope.ServiceProvider.GetRequiredService<IRefreshCurrenciesService>();

                    await refreshCurrenciesService.RefreshCurrenciesAsync(context.CancellationToken);
                }

                isSuccessful = true;

                _logger.LogInformation("Currency exchange rate update completed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating currency exchange rate: {ExMessage}", ex.Message);

                await Task.Delay(_config.BackgroundServices.RefreshCurrencies.ErrorDelayMs,
                    context.CancellationToken);
            }
    }

    #endregion Methods
}