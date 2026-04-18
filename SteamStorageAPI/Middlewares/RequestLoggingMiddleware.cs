using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;

namespace SteamStorageAPI.Middlewares;

public class RequestLoggingMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    #endregion Fields

    #region Constructor

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    #endregion Constructor

    #region Methods

    public async Task Invoke(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        ControllerActionDescriptor? actionDescriptor =
            context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (actionDescriptor is null)
            return;

        _logger.LogInformation(
            "\n\tMethod: {ActionName};\n\tServer time: {ServerTime};\n\tExecution time: {ElapsedMs} ms;\n",
            actionDescriptor.ActionName, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
    }

    #endregion Methods
}