using System.Diagnostics;
using LoginWebApp.Models;
using LoginWebApp.Utilities.TokenHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LoginWebApp.Controllers;

public class TokenController : Controller
{
    #region Fields

    private readonly IHubContext<TokenHub> _hubContext;

    #endregion Fields

    #region Constructor

    public TokenController(IHubContext<TokenHub> hubContext)
    {
        _hubContext = hubContext;
    }

    #endregion Constructor

    #region Records

    public record SetTokenRequest(string Group, string Token);

    #endregion Records

    #region Methods

    [HttpGet(Name = "SetToken")]
    public IActionResult SetToken([FromQuery] SetTokenRequest request)
    {
        TempData["Group"] = request.Group;
        TempData["Token"] = request.Token;
        return RedirectToAction(nameof(TokenView));
    }

    [HttpGet(Name = "TokenView")]
    public async Task<IActionResult> TokenView()
    {
        string group = TempData["Group"] as string ?? string.Empty;
        string token = TempData["Token"] as string ?? string.Empty;
        await _hubContext.Clients.Group(group).SendAsync("Token", token);
        return View(nameof(Token), new TokenViewModel { IsTokenEmpty = string.IsNullOrEmpty(token) });
    }

    public IActionResult Token()
    {
        return View(new TokenViewModel());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #endregion Methods
}
