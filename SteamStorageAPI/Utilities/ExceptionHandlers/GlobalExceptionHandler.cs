using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Utilities.Exceptions;

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

    #region Records

    public record ErrorResponse(string Message);

    #endregion Records

    #region Methods

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception.Message);

        ProblemDetails problemDetails = new()
        {
            Detail = exception.Message
        };

        switch (exception)
        {
            case OperationCanceledException:
                problemDetails.Status = StatusCodes.Status499ClientClosedRequest;
                problemDetails.Title = "Client Closed Request";
                break;
            case HttpResponseException ex:
                problemDetails.Status = ex.StatusCode;
                problemDetails.Title = ex.StatusCode.ToString();
                break;
            default:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "BadRequest";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(problemDetails.Detail),
            new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            },
            cancellationToken);

        return true;
    }

    #endregion Methods
}
