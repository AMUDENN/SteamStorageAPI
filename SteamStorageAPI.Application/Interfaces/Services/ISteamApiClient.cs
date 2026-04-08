using SteamStorageAPI.Application.DTOs.Auth;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.Interfaces.Services;

public interface ISteamApiClient
{
    string GetAuthUrl(string returnTo, string realm);
    
    Task<bool> ValidateAuthCallbackAsync(SteamCallbackDto callback, CancellationToken ct = default);
    
    long ParseSteamId(string claimedId);
    
    Task<SteamUserProfileDto?> GetUserProfileAsync(long steamId, CancellationToken ct = default);
    
    Task<IReadOnlyList<SteamInventoryItemDto>> GetInventoryAsync(
        long steamId, int appId, int count, CancellationToken ct = default);
    
    Task<SteamMarketPageDto?> GetMarketPageAsync(
        int appId, int currencyId, int count, int start, CancellationToken ct = default);
    
    Task<decimal?> GetSkinPriceAsync(
        int appId, string marketHashName, int currencyId, CancellationToken ct = default);
}

#region SteamAPI Dtos

public sealed record SteamUserProfileDto(
    string? Username,
    string? IconUrl,
    string? IconUrlMedium,
    string? IconUrlFull);

public sealed record SteamInventoryItemDto(
    string MarketHashName,
    string Name,
    string IconUrl,
    int Count);

public sealed record SteamMarketPageDto(
    int TotalCount,
    IReadOnlyList<SteamMarketSkinDto> Results);

public sealed record SteamMarketSkinDto(
    string HashName,
    string Name,
    string IconUrl,
    string SellPriceText);

#endregion SteamAPI Dtos