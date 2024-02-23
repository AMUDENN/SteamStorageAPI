using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SteamStorageAPI.Utilities.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    #region Fields

    private readonly ILogger<GlobalExceptionHandler> _logger;

    #endregion Fields

    #region Constructor

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    #endregion Constructor

    #region Methods

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception.Message);

        ProblemDetails problemDetails = new()
        {
            Title = "BadRequest",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message
        };

        if (exception is OperationCanceledException)
        {
            problemDetails.Status = StatusCodes.Status499ClientClosedRequest;
            problemDetails.Title = "Client Closed Request";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails.Detail, cancellationToken);

        return true;
    }

    #endregion Methods
}
