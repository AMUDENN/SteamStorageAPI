using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
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
        public record PageResponse(int Id, string Title);
        public record SetPageRequest(int PageId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetPages")]
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
        [HttpPut(Name = "SetStartPage")]
        public async Task<ActionResult> SetStartPage(SetPageRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Pages.Any(x => x.Id == request.PageId))
                    return NotFound("Страницы с таким Id не существует");

                user.RoleId = request.PageId;

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
