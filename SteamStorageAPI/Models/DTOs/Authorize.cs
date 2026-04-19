using Microsoft.AspNetCore.Mvc;

namespace SteamStorageAPI.Models.DTOs;

public record GetAuthUrlRequest(
    string? ReturnTo);

public record AuthUrlResponse(
    string Url,
    string Group);

public record CookieAuthResponse(
    string Token);

public record SteamAuthRequest(
    [FromQuery(Name = "requestInfo")] string RequestInfo,
    [FromQuery(Name = "openid.ns")] string Ns,
    [FromQuery(Name = "openid.mode")] string Mode,
    [FromQuery(Name = "openid.op_endpoint")]
    string OpEndpoint,
    [FromQuery(Name = "openid.claimed_id")]
    string ClaimedId,
    [FromQuery(Name = "openid.identity")] string Identity,
    [FromQuery(Name = "openid.return_to")] string ReturnTo,
    [FromQuery(Name = "openid.response_nonce")]
    string ResponseNonce,
    [FromQuery(Name = "openid.assoc_handle")]
    string AssocHandle,
    [FromQuery(Name = "openid.signed")] string Signed,
    [FromQuery(Name = "openid.sig")] string Sig);

public record CheckCookieAuthRequest(
    long SteamId);

public record ExchangeTokenResponse(
    string Token);