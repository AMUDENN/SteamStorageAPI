using System.Net;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.SteamAPIModels.Games;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.Domain.GameService;

public class GameService : IGameService
{
    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public GameService(
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<bool> IsGameIconExistsAsync(
        int steamGameId,
        string iconUrlHash,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage response =
            await client.GetAsync(SteamApi.GetGameIconUrl(steamGameId, iconUrlHash), cancellationToken);
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task PostGameAsync(
        PostGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        SteamGameResponse? steamResponse =
            await client.GetFromJsonAsync<SteamGameResponse>(SteamApi.GetGameInfoUrl(request.SteamGameId),
                cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status400BadRequest, "Указан неверный id игры");

        if (!await IsGameIconExistsAsync(request.SteamGameId, request.IconUrlHash, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Указан неверный хэш-код иконки игры");

        await _context.Games.AddAsync(new()
        {
            SteamGameId = request.SteamGameId,
            Title = steamResponse.name,
            GameIconUrl = request.IconUrlHash
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutGameAsync(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        if (!await IsGameIconExistsAsync(game.SteamGameId, request.IconUrlHash, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Указан неверный хэш-код иконки игры");

        game.GameIconUrl = request.IconUrlHash;
        game.Title = request.Title;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteGameAsync(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        _context.Games.Remove(game);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}