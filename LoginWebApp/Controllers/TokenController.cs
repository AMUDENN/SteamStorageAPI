﻿using System.Diagnostics;
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
    public async Task<IActionResult> SetToken([FromQuery] SetTokenRequest request)
    {
        TempData["Group"] = request.Group;
        TempData["Token"] = request.Token;
        await _hubContext.Clients.Group(request.Group).SendAsync("Token", request.Token);
        return RedirectToAction(nameof(Token));
    }

    public IActionResult Token()
    {
        return View(new TokenViewModel
        {
            IsTokenEmpty = string.IsNullOrEmpty(TempData["Group"] as string)
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #endregion Methods
}
