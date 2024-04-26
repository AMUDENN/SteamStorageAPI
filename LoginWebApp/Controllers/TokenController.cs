using System.Diagnostics;
using LoginWebApp.Models;
using LoginWebApp.Utilities;
using LoginWebApp.Utilities.TokenHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LoginWebApp.Controllers;

[Route("[action]")]
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

    public record TokenRequest(bool IsTokenEmpty = true);

    #endregion Records

    #region Methods

    [HttpGet(Name = "SetToken")]
    public async Task<IActionResult> SetToken([FromQuery] SetTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Group) || string.IsNullOrWhiteSpace(request.Token))
            return Redirect($"{ProgramConstants.TOKEN_ADRESS}/Token?IsTokenEmpty={true}");
        await _hubContext.Clients.Group(request.Group).SendAsync("Token", request.Token);
        return Redirect($"{ProgramConstants.TOKEN_ADRESS}/Token?IsTokenEmpty={false}");
    }

    [HttpGet(Name = "Token")]
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
