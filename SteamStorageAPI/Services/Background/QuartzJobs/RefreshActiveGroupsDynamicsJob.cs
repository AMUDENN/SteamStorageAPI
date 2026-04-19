using Quartz;
using SteamStorageAPI.Services.Background.RefreshActiveDynamicsService;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Services.Background.QuartzJobs;

public class RefreshActiveGroupsDynamicsJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshActiveGroupsDynamicsJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AppConfig _config;

    #endregion Fields

    #region Constructor

    public RefreshActiveGroupsDynamicsJob(
        ILogger<RefreshActiveGroupsDynamicsJob> logger,
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
                _logger.LogInformation("ActiveGroupsDynamic records update is starting");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshActiveGroupDynamicsService refreshActiveGroupDynamicsService =
                        scope.ServiceProvider.GetRequiredService<IRefreshActiveGroupDynamicsService>();

                    await refreshActiveGroupDynamicsService.RefreshActiveDynamicsAsync(context.CancellationToken);
                }

                isSuccessful = true;

                _logger.LogInformation("ActiveGroupsDynamic update completed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ActiveGroupsDynamic: {ExMessage}", ex.Message);

                await Task.Delay(_config.BackgroundServices.RefreshActiveGroupsDynamicsJob.ErrorDelayMs,
                    context.CancellationToken);
            }
    }

    #endregion Methods
}