using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Archives;

namespace SteamStorageAPI.Models.DTOs;

public record ArchiveResponse(
    int Id,
    int GroupId,
    BaseSkinResponse Skin,
    DateTime BuyDate,
    DateTime SoldDate,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    decimal SoldSum,
    double Change,
    string? Description);

public record ArchivesResponse(
    int Count,
    int PagesCount,
    IEnumerable<ArchiveResponse> Archives);

public record ArchivesStatisticResponse(
    int ArchivesCount,
    decimal InvestmentSum,
    decimal SoldSum);

public record ArchivesPagesCountResponse(
    int Count);

public record ArchivesCountResponse(
    int Count);

[Validator<GetArchiveInfoRequestValidator>]
public record GetArchiveInfoRequest(
    int Id);

[Validator<GetArchivesRequestValidator>]
public record GetArchivesRequest(
    int? GroupId,
    int? GameId,
    string? Filter,
    ArchiveOrderName? OrderName,
    bool? IsAscending,
    int PageNumber,
    int PageSize);

[Validator<GetArchivesStatisticRequestValidator>]
public record GetArchivesStatisticRequest(
    int? GroupId,
    int? GameId,
    string? Filter);

[Validator<GetArchivesPagesCountRequestValidator>]
public record GetArchivesPagesCountRequest(
    int? GroupId,
    int? GameId,
    string? Filter,
    int PageSize);

[Validator<GetArchivesCountRequestValidator>]
public record GetArchivesCountRequest(
    int? GroupId,
    int? GameId,
    string? Filter);

[Validator<PostArchiveRequestValidator>]
public record PostArchiveRequest(
    int GroupId,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    int SkinId,
    string? Description,
    DateTime BuyDate,
    DateTime SoldDate);

[Validator<PutArchiveRequestValidator>]
public record PutArchiveRequest(
    int Id,
    int GroupId,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    int SkinId,
    string? Description,
    DateTime BuyDate,
    DateTime SoldDate);

[Validator<DeleteArchiveRequestValidator>]
public record DeleteArchiveRequest(
    int Id);