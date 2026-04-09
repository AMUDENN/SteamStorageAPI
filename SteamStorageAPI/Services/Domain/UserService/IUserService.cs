using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.UserService;

public interface IUserService
{
    Task<UsersResponse> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUsersCountAsync(
        CancellationToken cancellationToken = default);

    Task<UserResponse> GetUserInfoAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserResponse> GetCurrentUserInfoAsync(
        User user,
        CancellationToken cancellationToken = default);

    GoalSumResponse GetCurrentUserGoalSum(
        User user);

    Task<HasAccessToAdminPanelResponse> GetHasAccessToAdminPanelAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task PutGoalSumAsync(
        User user,
        PutGoalSumRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
        User user,
        CancellationToken cancellationToken = default);
}