using Quartz;
using SteamStorageAPI.Services.Background.RefreshSkinDynamicsService;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Services.Background.QuartzJobs;

public class RefreshSkinDynamicsJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshSkinDynamicsJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AppConfig _config;

    #endregion Fields

    #region Constructor

    public RefreshSkinDynamicsJob(
        ILogger<RefreshSkinDynamicsJob> logger,
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
                _logger.LogInformation("Item price update is starting");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshSkinDynamicsService refreshSkinDynamicsService =
                        scope.ServiceProvider.GetRequiredService<IRefreshSkinDynamicsService>();

                    await refreshSkinDynamicsService.RefreshSkinDynamicsAsync(context.CancellationToken);
                }

                isSuccessful = true;

                _logger.LogInformation("Item price update completed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item prices: {ExMessage}", ex.Message);

                await Task.Delay(_config.BackgroundServices.RefreshSkinDynamicsJob.ErrorDelayMs,
                    context.CancellationToken);
            }
    }

    #endregion Methods
}