using Quartz;
using SteamStorageAPI.Services.RefreshActiveDynamicsService;
using SteamStorageAPI.Services.Tools;

namespace SteamStorageAPI.Services.QuartzJobs;

public class RefreshActiveGroupsDynamicsJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshActiveGroupsDynamicsJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshActiveGroupsDynamicsJob(
        ILogger<RefreshActiveGroupsDynamicsJob> logger,
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
                _logger.LogInformation("Начинается обновление записей ActiveGroupsDynamic");

                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshActiveGroupDynamicsService refreshActiveGroupDynamicsService =
                        scope.ServiceProvider.GetRequiredService<IRefreshActiveGroupDynamicsService>();

                    await refreshActiveGroupDynamicsService.RefreshActiveDynamicsAsync(context.CancellationToken);
                }

                isSuccessful = true;

                _logger.LogInformation("Обновление ActiveGroupsDynamic завершено");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обновлении ActiveGroupsDynamic: {ex.Message}");

                await Task.Delay(ServicesConstants.REFRESH_ACTIVE_GROUPS_DYNAMICS_JOB_ERROR_DELAY,
                    context.CancellationToken);
            }
        }
    }

    #endregion Methods
}
