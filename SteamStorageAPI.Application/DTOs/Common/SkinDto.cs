// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Common;

public sealed record SkinDto(
    int Id,
    string SkinIconUrl,
    string Title,
    string MarketHashName,
    string MarketUrl);
