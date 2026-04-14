using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.RoleService;

public class RoleService : IRoleService
{
    #region Fields

    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RoleService(
        SteamStorageContext context)
    {
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<RolesResponse> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        List<Role> roles = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);

        return new RolesResponse(roles.Count, roles.Select(x => new RoleResponse(x.Id, x.Title)));
    }

    public async Task SetRoleAsync(
        User user,
        SetRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == user.Id)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Нельзя изменить свою роль");

        User requestedUser = await _context.Users
                                 .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
                             ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                 "Пользователя с таким Id не существует");

        if (!await _context.Roles.AnyAsync(x => x.Id == request.RoleId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "Роли с таким Id не существует");

        requestedUser.RoleId = request.RoleId;
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}