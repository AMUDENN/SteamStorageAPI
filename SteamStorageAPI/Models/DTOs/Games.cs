using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Games;

namespace SteamStorageAPI.Models.DTOs;

public record GameResponse(
    int Id,
    int SteamGameId,
    string Title,
    string GameIconUrl);

public record GamesResponse(
    int Count,
    IEnumerable<GameResponse> Games);

[Validator<PostGameRequestValidator>]
public record PostGameRequest(
    int SteamGameId,
    string IconUrlHash);

[Validator<PutGameRequestValidator>]
public record PutGameRequest(
    int GameId,
    string IconUrlHash,
    string Title);

[Validator<DeleteGameRequestValidator>]
public record DeleteGameRequest(
    int GameId);