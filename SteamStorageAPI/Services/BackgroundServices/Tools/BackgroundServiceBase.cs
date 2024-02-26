namespace SteamStorageAPI.Services.BackgroundServices.Tools;

public abstract class BackgroundServiceBase : BackgroundService
{
    protected static async Task<bool> WaitForAppStartup(
        IHostApplicationLifetime lifetime,
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