using System.Net;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.SteamAPIModels.Games;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.GameService;

public class GameService : IGameService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public GameService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<GamesResponse> GetGamesAsync(
        CancellationToken cancellationToken = default)
    {
        List<Game> games = await _context.Games.AsNoTracking().ToListAsync(cancellationToken);

        return new GamesResponse(games.Count, games.Select(x =>
            new GameResponse(x.Id, x.SteamGameId, x.Title,
                _steamApiUrlBuilder.GetGameIconUrl(x.SteamGameId, x.GameIconUrl))));
    }

    public async Task<bool> IsGameIconExistsAsync(
        int steamGameId,
        string iconUrlHash,
        CancellationToken cancellationToken = default)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage response =
            await client.GetAsync(_steamApiUrlBuilder.GetGameIconUrl(steamGameId, iconUrlHash), cancellationToken);
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task PostGameAsync(
        PostGameRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        SteamGameResponse steamResponse =
            await client.GetFromJsonAsync<SteamGameResponse>(_steamApiUrlBuilder.GetGameInfoUrl(request.SteamGameId),
                cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status400BadRequest, "Invalid game id provided");

        if (!await IsGameIconExistsAsync(request.SteamGameId, request.IconUrlHash, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Invalid game icon hash code provided");

        await _context.Games.AddAsync(new Game
        {
            SteamGameId = request.SteamGameId,
            Title = steamResponse.name ?? string.Empty,
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
                        "A game with this Id does not exist");

        if (!await IsGameIconExistsAsync(game.SteamGameId, request.IconUrlHash, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Invalid game icon hash code provided");

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
                        "A game with this Id does not exist");

        _context.Games.Remove(game);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}