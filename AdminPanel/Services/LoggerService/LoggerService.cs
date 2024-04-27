using System.Reflection;
using System.Text;
using SteamStorageAPI.SDK.Services.Logger.LoggerService;

namespace AdminPanel.Services.LoggerService;

public class LoggerService : ILoggerService
{
    #region Methods

    public Task LogAsync(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task LogAsync(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"\n{CreateErrorMessage(exception)}");
        return Task.CompletedTask;
    }

    public Task LogAsync(string message, Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"{message}\n{CreateErrorMessage(exception)}");
        return Task.CompletedTask;
    }

    private static string CreateErrorMessage(Exception exception)
    {
        StringBuilder errorInfo = new();
        foreach (PropertyInfo property in exception.GetType().GetProperties())
            errorInfo.AppendLine($"\t{property.Name}: {property.GetValue(exception)}");
        errorInfo.AppendLine();
        return errorInfo.ToString();
    }

    #endregion Methods
}
