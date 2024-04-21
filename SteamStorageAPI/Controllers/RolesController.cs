using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Roles;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [Route("[controller]/[action]")]
    public class RolesController : ControllerBase
    {
        #region Fields

        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public RolesController(
            SteamStorageContext context)
        {
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record RoleResponse(
            int Id,
            string Title);
        
        public record RolesResponse(
            int Count,
            IEnumerable<RoleResponse> Roles);

        [Validator<SetRoleRequestValidator>]
        public record SetRoleRequest(
            int UserId,
            int RoleId);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение списка ролей
        /// </summary>
        /// <response code="200">Возвращает список ролей</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetRoles")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<RolesResponse>> GetRoles(
            CancellationToken cancellationToken = default)
        {
            List<Role> roles = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);

            return Ok(new RolesResponse(roles.Count, roles.Select(x => new RoleResponse(x.Id, x.Title))));
        }

        #endregion GET

        #region PUT

        /// <summary>
        /// Установка роли пользователю
        /// </summary>
        /// <response code="200">Роль успешно установлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Роли с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "SetRole")]
        public async Task<ActionResult> SetRole(
            SetRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            if (!await _context.Roles.AnyAsync(x => x.Id == request.RoleId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Роли с таким Id не существует");

            user.RoleId = request.RoleId;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT
    }
}
