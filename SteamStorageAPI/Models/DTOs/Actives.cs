using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Actives;

namespace SteamStorageAPI.Models.DTOs;

public record ActiveResponse(
    int Id,
    int GroupId,
    BaseSkinResponse Skin,
    DateTime BuyDate,
    int Count,
    decimal BuyPrice,
    decimal CurrentPrice,
    decimal CurrentSum,
    decimal? GoalPrice,
    double? GoalPriceCompletion,
    double Change,
    string? Description);

public record ActivesResponse(
    int Count,
    int PagesCount,
    IEnumerable<ActiveResponse> Actives);

public record ActivesStatisticResponse(
    int ActivesCount,
    decimal InvestmentSum,
    decimal CurrentSum);

public record ActivesPagesCountResponse(
    int Count);

public record ActivesCountResponse(
    int Count);

[Validator<GetActiveInfoRequestValidator>]
public record GetActiveInfoRequest(
    int Id);

[Validator<GetActivesRequestValidator>]
public record GetActivesRequest(
    int? GroupId,
    int? GameId,
    string? Filter,
    ActiveOrderName? OrderName,
    bool? IsAscending,
    int PageNumber,
    int PageSize);

[Validator<GetActivesStatisticRequestValidator>]
public record GetActivesStatisticRequest(
    int? GroupId,
    int? GameId,
    string? Filter);

[Validator<GetActivesPagesCountRequestValidator>]
public record GetActivesPagesCountRequest(
    int? GroupId,
    int? GameId,
    string? Filter,
    int PageSize);

[Validator<GetActivesCountRequestValidator>]
public record GetActivesCountRequest(
    int? GroupId,
    int? GameId,
    string? Filter);

[Validator<PostActiveRequestValidator>]
public record PostActiveRequest(
    int GroupId,
    int Count,
    decimal BuyPrice,
    decimal? GoalPrice,
    int SkinId,
    string? Description,
    DateTime BuyDate);

[Validator<PutActiveRequestValidator>]
public record PutActiveRequest(
    int Id,
    int GroupId,
    int Count,
    decimal BuyPrice,
    decimal? GoalPrice,
    int SkinId,
    string? Description,
    DateTime BuyDate);

[Validator<SoldActiveRequestValidator>]
public record SoldActiveRequest(
    int Id,
    int GroupId,
    int Count,
    decimal SoldPrice,
    DateTime SoldDate,
    string? Description);

[Validator<DeleteActiveRequestValidator>]
public record DeleteActiveRequest(
    int Id);