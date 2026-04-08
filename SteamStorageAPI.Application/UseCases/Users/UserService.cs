using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Users;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Users;

public sealed class UserService
{
    #region Fields

    private readonly IUserRepository _userRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IRoleRepository _roleRepository;

    #endregion

    #region Constructor

    public UserService(
        IUserRepository userRepository,
        ICurrencyRepository currencyRepository,
        IPageRepository pageRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _currencyRepository = currencyRepository;
        _pageRepository = pageRepository;
        _roleRepository = roleRepository;
    }

    #endregion

    #region Methods

    public async Task<UserDto> GetByIdAsync(int userId, CancellationToken ct = default) =>
        await _userRepository.GetDtoByIdAsync(userId, ct)
        ?? throw new NotFoundException("User", userId);

    public async Task<PagedResult<UserDto>> GetPagedAsync(
        PaginationDto pagination, CancellationToken ct = default) =>
        await _userRepository.GetPagedAsync(pagination, ct);

    public async Task<int> GetCountAsync(CancellationToken ct = default) =>
        await _userRepository.GetCountAsync(ct);

    public async Task<bool> HasAdminAccessAsync(int userId, CancellationToken ct = default) =>
        await _userRepository.HasAdminAccessAsync(userId, ct);

    public async Task SetGoalSumAsync(UpdateGoalSumDto dto, CancellationToken ct = default)
    {
        User user = await GetUserOrThrowAsync(dto.UserId, ct);
        user.SetGoalSum(dto.GoalSum);
        await _userRepository.UpdateAsync(user, ct);
    }

    public async Task SetCurrencyAsync(UpdateCurrencyDto dto, CancellationToken ct = default)
    {
        User user = await GetUserOrThrowAsync(dto.UserId, ct);

        if (await _currencyRepository.GetByIdAsync(dto.CurrencyId, ct) is null)
            throw new NotFoundException("Currency", dto.CurrencyId);

        user.SetCurrency(dto.CurrencyId);
        await _userRepository.UpdateAsync(user, ct);
    }

    public async Task SetStartPageAsync(UpdateStartPageDto dto, CancellationToken ct = default)
    {
        User user = await GetUserOrThrowAsync(dto.UserId, ct);

        if (await _pageRepository.GetByIdAsync(dto.PageId, ct) is null)
            throw new NotFoundException("Page", dto.PageId);

        user.SetStartPage(dto.PageId);
        await _userRepository.UpdateAsync(user, ct);
    }

    public async Task SetRoleAsync(UpdateRoleDto dto, CancellationToken ct = default)
    {
        User user = await GetUserOrThrowAsync(dto.TargetUserId, ct);

        if (await _roleRepository.GetByIdAsync(dto.RoleId, ct) is null)
            throw new NotFoundException("Role", dto.RoleId);

        user.SetRole(dto.RoleId);
        await _userRepository.UpdateAsync(user, ct);
    }

    #endregion

    #region Private helpers

    private async Task<User> GetUserOrThrowAsync(int userId, CancellationToken ct) =>
        await _userRepository.GetByIdAsync(userId, ct)
        ?? throw new NotFoundException("User", userId);

    #endregion
}
