using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

namespace SteamStorageAPI.Models.DTOs;

public record ActiveGroupResponse(
    int Id,
    string Title,
    string? Description,
    string Colour,
    decimal? GoalSum,
    double? GoalSumCompletion,
    int Count,
    decimal BuySum,
    decimal CurrentSum,
    double Change,
    DateTime DateCreation);

public record ActiveGroupsResponse(
    int Count,
    IEnumerable<ActiveGroupResponse> ActiveGroups);

public record ActiveGroupsGameCountResponse(
    string GameTitle,
    double Percentage,
    int Count);

public record ActiveGroupsGameInvestmentSumResponse(
    string GameTitle,
    double Percentage,
    decimal InvestmentSum);

public record ActiveGroupsGameCurrentSumResponse(
    string GameTitle,
    double Percentage,
    decimal CurrentSum);

public record ActiveGroupsStatisticResponse(
    int ActivesCount,
    decimal InvestmentSum,
    decimal CurrentSum,
    IEnumerable<ActiveGroupsGameCountResponse> GameCount,
    IEnumerable<ActiveGroupsGameInvestmentSumResponse> GameInvestmentSum,
    IEnumerable<ActiveGroupsGameCurrentSumResponse> GameCurrentSum);

public record ActiveGroupDynamicResponse(
    int Id,
    DateTime DateUpdate,
    decimal Sum);

public record ActiveGroupDynamicStatsResponse(
    double ChangePeriod,
    IEnumerable<ActiveGroupDynamicResponse> Dynamic);

public record ActiveGroupsCountResponse(
    int Count);

[Validator<GetActiveGroupInfoRequestValidator>]
public record GetActiveGroupInfoRequest(
    int GroupId);

[Validator<GetActiveGroupsRequestValidator>]
public record GetActiveGroupsRequest(
    ActiveGroupOrderName? OrderName,
    bool? IsAscending);

[Validator<GetActiveGroupDynamicRequestValidator>]
public record GetActiveGroupDynamicRequest(
    int GroupId,
    DateTime StartDate,
    DateTime EndDate);

[Validator<PostActiveGroupRequestValidator>]
public record PostActiveGroupRequest(
    string Title,
    string? Description,
    string? Colour,
    decimal? GoalSum);

[Validator<PutActiveGroupRequestValidator>]
public record PutActiveGroupRequest(
    int GroupId,
    string Title,
    string? Description,
    string? Colour,
    decimal? GoalSum);

[Validator<DeleteActiveGroupRequestValidator>]
public record DeleteActiveGroupRequest(
    int GroupId);