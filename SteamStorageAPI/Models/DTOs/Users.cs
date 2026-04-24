using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Users;

namespace SteamStorageAPI.Models.DTOs;

#region Records

public record UserResponse(
    int UserId,
    string SteamId,
    string ProfileUrl,
    string? ImageUrl,
    string? ImageUrlMedium,
    string? ImageUrlFull,
    string? Nickname,
    int RoleId,
    string Role,
    int StartPageId,
    string StartPage,
    int CurrencyId,
    DateTime DateRegistration,
    decimal? GoalSum);

public record UsersResponse(
    int Count,
    int PagesCount,
    IEnumerable<UserResponse> Users);

public record UsersCountResponse(
    int Count);

public record GoalSumResponse(
    decimal? GoalSum);

public record HasAccessToAdminPanelResponse(
    bool HasAccess);

[Validator<GetUsersRequestValidator>]
public record GetUsersRequest(
    int PageNumber,
    int PageSize,
    int? UserId,
    string? Nickname,
    int? SteamId);

[Validator<GetUserRequestValidator>]
public record GetUserRequest(
    int UserId);

[Validator<PutGoalSumRequestValidator>]
public record PutGoalSumRequest(
    decimal? GoalSum);

#endregion Records