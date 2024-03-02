using Quartz;
using SteamStorageAPI.Services.RefreshActiveDynamicsService;

namespace SteamStorageAPI.Services.QuartzJobs;

public class RefreshActiveGroupDynamicsJob : IJob
{
    #region Fields

    private readonly ILogger<RefreshActiveGroupDynamicsJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion Fields

    #region Constructor

    public RefreshActiveGroupDynamicsJob(
        ILogger<RefreshActiveGroupDynamicsJob> logger,
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

                await Task.Delay(30 * 60 * 1000, context.CancellationToken);
            }
        }
    }

    #endregion Methods
}
