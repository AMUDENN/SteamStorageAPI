using System.Diagnostics;
using LoginWebApp.Models;
using LoginWebApp.Utilities.Config;
using LoginWebApp.Utilities.TokenHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace LoginWebApp.Controllers;

[Route("token/[action]")]
public class TokenController : Controller
{
    #region Constants

    private const string InternalApiKeyHeader = "X-Internal-Api-Key";

    #endregion Constants

    #region Fields

    private readonly IHubContext<TokenHub> _hubContext;
    private readonly string _internalApiKey;

    #endregion Fields

    #region Constructor

    public TokenController(
        IHubContext<TokenHub> hubContext,
        AppConfig config)
    {
        _hubContext = hubContext;
        _internalApiKey = config.App.InternalApiKey;
    }

    #endregion Constructor

    #region Records

    public record SetTokenRequest(string Group, string Token);

    public record TokenRequest(bool IsTokenEmpty = true);

    #endregion Records

    #region Methods

    [HttpPost]
    public async Task<IActionResult> SetToken([FromBody] SetTokenRequest request)
    {
        if (string.IsNullOrEmpty(_internalApiKey)
            || !Request.Headers.TryGetValue(InternalApiKeyHeader, out StringValues providedKey)
            || providedKey != _internalApiKey)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Group) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest();

        await _hubContext.Clients.Group(request.Group).SendAsync("Token", request.Token);

        return Ok();
    }

    public IActionResult Token([FromQuery] TokenRequest request)
    {
        return View(new TokenViewModel
        {
            IsTokenEmpty = request.IsTokenEmpty
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    #endregion Methods
}