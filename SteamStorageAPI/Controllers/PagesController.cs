using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PagesController : ControllerBase
    {
        #region Fields

        private readonly ILogger<PagesController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public PagesController(ILogger<PagesController> logger, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record PageResponse(
            int Id,
            string Title);

        public record SetPageRequest(
            int PageId);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение списка страниц
        /// </summary>
        /// <response code="200">Возвращает список страниц</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        [HttpGet(Name = "GetPages")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<PageResponse>> GetPages()
        {
            try
            {
                return Ok(_context.Pages.ToList().Select(x =>
                    new PageResponse(x.Id, x.Title)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region PUT
        
        /// <summary>
        /// Установка стартовой страницы
        /// </summary>
        /// <response code="200">Стартовая страница успешно установлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Страницы с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "SetStartPage")]
        [Authorize]
        public async Task<ActionResult> SetStartPage(SetPageRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Pages.Any(x => x.Id == request.PageId))
                    return NotFound("Страницы с таким Id не существует");

                user.StartPageId = request.PageId;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion PUT
    }
}
