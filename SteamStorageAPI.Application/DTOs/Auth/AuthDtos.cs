// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Auth;

public sealed record GetAuthUrlDto(string? ReturnTo);

public sealed record AuthUrlDto(string Url, string Group);

public sealed record SteamCallbackDto(
    string RequestInfo,
    string Ns,
    string Mode,
    string OpEndpoint,
    string ClaimedId,
    string Identity,
    string ReturnTo,
    string ResponseNonce,
    string AssocHandle,
    string Signed,
    string Sig);

public sealed record AuthTokenDto(string Token);
