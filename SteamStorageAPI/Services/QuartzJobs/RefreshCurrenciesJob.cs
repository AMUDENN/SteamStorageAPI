using Quartz;
using SteamStorageAPI.Services.RefreshCurrenciesService;

namespace SteamStorageAPI.Services.QuartzJobs;

public class RefreshCurrenciesJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshCurrenciesJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshCurrenciesJob(
        ILogger<RefreshCurrenciesJob> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion Constructor

    #region Methods

    public async Task Execute(
        IJobExecutionContext context)
    {
        bool isSuccessful = false;
        
        while (!isSuccessful && !context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Начинается обновление курса валют");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshCurrenciesService refreshCurrenciesService =
                        scope.ServiceProvider.GetRequiredService<IRefreshCurrenciesService>();

                    await refreshCurrenciesService.RefreshCurrenciesAsync(context.CancellationToken);
                }

                isSuccessful = true;
                
                _logger.LogInformation("Обновление курса валют завершено");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обновлении курса валют: {ex.Message}");

                await Task.Delay(30 * 60 * 1000, context.CancellationToken);
            }
        }
    }

    #endregion Methods
}
