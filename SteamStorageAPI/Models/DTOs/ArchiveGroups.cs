using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

namespace SteamStorageAPI.Models.DTOs;

public record ArchiveGroupResponse(
    int Id,
    string Title,
    string? Description,
    string Colour,
    int Count,
    decimal BuySum,
    decimal SoldSum,
    double Change,
    DateTime DateCreation);

public record ArchiveGroupsResponse(
    int Count,
    IEnumerable<ArchiveGroupResponse> ArchiveGroups);

public record ArchiveGroupsGameCountResponse(
    string GameTitle,
    double Percentage,
    int Count);

public record ArchiveGroupsGameBuySumResponse(
    string GameTitle,
    double Percentage,
    decimal BuySum);

public record ArchiveGroupsGameSoldSumResponse(
    string GameTitle,
    double Percentage,
    decimal SoldSum);

public record ArchiveGroupsStatisticResponse(
    int ArchivesCount,
    decimal BuySum,
    decimal SoldSum,
    IEnumerable<ArchiveGroupsGameCountResponse> GameCount,
    IEnumerable<ArchiveGroupsGameBuySumResponse> GameBuySum,
    IEnumerable<ArchiveGroupsGameSoldSumResponse> GameSoldSum);

public record ArchiveGroupsCountResponse(
    int Count);

[Validator<GetArchiveGroupInfoRequestValidator>]
public record GetArchiveGroupInfoRequest(
    int GroupId);

[Validator<GetArchiveGroupsRequestValidator>]
public record GetArchiveGroupsRequest(
    ArchiveGroupOrderName? OrderName,
    bool? IsAscending);

[Validator<PostArchiveGroupRequestValidator>]
public record PostArchiveGroupRequest(
    string Title,
    string? Description,
    string? Colour);

[Validator<PutArchiveGroupRequestValidator>]
public record PutArchiveGroupRequest(
    int GroupId,
    string Title,
    string? Description,
    string? Colour);

[Validator<DeleteArchiveGroupRequestValidator>]
public record DeleteArchiveGroupRequest(
    int GroupId);