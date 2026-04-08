// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Users;

public sealed record UserDto(
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

public sealed record UsersFilterDto(
    int PageNumber,
    int PageSize);

public sealed record UpdateGoalSumDto(
    int UserId,
    decimal? GoalSum);

public sealed record UpdateCurrencyDto(
    int UserId,
    int CurrencyId);

public sealed record UpdateStartPageDto(
    int UserId,
    int PageId);

public sealed record UpdateRoleDto(
    int TargetUserId,
    int RoleId);
