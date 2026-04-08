using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Skins;

namespace SteamStorageAPI.Models.DTOs;

public record BaseSkinResponse(
    int Id,
    string SkinIconUrl,
    string Title,
    string MarketHashName,
    string MarketUrl);

public record BaseSkinsResponse(
    int Count,
    IEnumerable<BaseSkinResponse> Skins);

public record SkinResponse(
    BaseSkinResponse Skin,
    decimal CurrentPrice,
    double Change7D,
    double Change30D,
    bool IsMarked);

public record SkinsResponse(
    int Count,
    int PagesCount,
    IEnumerable<SkinResponse> Skins);

public record SkinDynamicResponse(
    int Id,
    DateTime DateUpdate,
    decimal Price);

public record SkinDynamicStatsResponse(
    double ChangePeriod,
    IEnumerable<SkinDynamicResponse> Dynamic);

public record SkinPagesCountResponse(
    int Count);

public record SteamSkinsCountResponse(
    int Count);

public record SavedSkinsCountResponse(
    int Count);

[Validator<GetSkinInfoRequestValidator>]
public record GetSkinInfoRequest(
    int SkinId);

public record GetBaseSkinsRequest(
    string? Filter);

[Validator<GetSkinsRequestValidator>]
public record GetSkinsRequest(
    int? GameId,
    string? Filter,
    SkinOrderName? OrderName,
    bool? IsAscending,
    bool? IsMarked,
    int PageNumber,
    int PageSize);

[Validator<GetSkinDynamicsRequestValidator>]
public record GetSkinDynamicsRequest(
    int SkinId,
    DateTime StartDate,
    DateTime EndDate);

[Validator<GetSkinPagesCountRequestValidator>]
public record GetSkinPagesCountRequest(
    int? GameId,
    string? Filter,
    bool? IsMarked,
    int PageSize);

[Validator<GetSteamSkinsCountRequestValidator>]
public record GetSteamSkinsCountRequest(
    int GameId);

[Validator<GetSavedSkinsCountRequestValidator>]
public record GetSavedSkinsCountRequest(
    int? GameId,
    string? Filter,
    bool? IsMarked);

[Validator<PostSkinRequestValidator>]
public record PostSkinRequest(
    int GameId,
    string MarketHashName);

[Validator<SetMarkedSkinRequestValidator>]
public record SetMarkedSkinRequest(
    int SkinId);

[Validator<DeleteMarkedSkinRequestValidator>]
public record DeleteMarkedSkinRequest(
    int SkinId);