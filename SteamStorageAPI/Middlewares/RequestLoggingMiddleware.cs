using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;

namespace SteamStorageAPI.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            ControllerActionDescriptor? actionDescriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            string? actionName = actionDescriptor?.ActionName ?? "Unknown Action";

            _logger.LogInformation($"Метод: {actionName}, " +
                                   $"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
        }
    }
}
