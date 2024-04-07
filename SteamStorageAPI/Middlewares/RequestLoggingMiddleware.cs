using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;

namespace SteamStorageAPI.Middlewares
{
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
            string actionName = actionDescriptor?.ActionName ?? "Unknown Action";

            _logger.LogInformation($"\n\tМетод: {actionName};\n\tВремя сервера: {DateTime.Now};\n\tВремя выполнения: {stopwatch.ElapsedMilliseconds} мс;");
        }

        #endregion Methods
    }
}
