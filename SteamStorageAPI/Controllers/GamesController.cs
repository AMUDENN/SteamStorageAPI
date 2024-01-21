using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Games;
using SteamStorageAPI.Utilities.Steam;
using System.Net;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]

    public class GamesController : ControllerBase
    {
        #region Fields

        private readonly ILogger<GamesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public GamesController(ILogger<GamesController> logger, IHttpClientFactory httpClientFactory,
            SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record GameResponse(int Id, int SteamGameId, string Title, string GameIconUrl);

        public record PostGameRequest(int SteamGameId, string IconUrlHash);

        public record PutGameRequest(int GameId, string IconUrlHash, string Title);

        public record DeleteGameRequest(int GameId);

        #endregion Records

        #region Methods

        private async Task<bool> IsGameIconExists(int steamGameId, string iconUrlHash)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            HttpResponseMessage response = await client.GetAsync(SteamApi.GetGameIconUrl(steamGameId, iconUrlHash));
            return response.StatusCode == HttpStatusCode.OK;
        }

        private async Task<SteamGameResponse?> GetGameResponse(int steamGameId)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            return await client.GetFromJsonAsync<SteamGameResponse>(SteamApi.GetGameInfoUrl(steamGameId));
        }

        #endregion Methods

        #region GET

        [HttpGet(Name = "GetGames")]
        public ActionResult<IEnumerable<GameResponse>> GetGames()
        {
            try
            {
                return Ok(_context.Games.Select(x =>
                    new GameResponse(x.Id,
                        x.SteamGameId,
                        x.Title,
                        SteamApi.GetGameIconUrl(x.SteamGameId, x.GameIconUrl))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region POST

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost(Name = "PostGame")]
        public async Task<ActionResult> PostGame(PostGameRequest request)
        {
            try
            {
                SteamGameResponse? response = await GetGameResponse(request.SteamGameId);

                if (response is null)
                    return BadRequest("Указан неверный id игры");

                if (!await IsGameIconExists(request.SteamGameId, request.IconUrlHash))
                    return BadRequest("Указан неверный хэш-код иконки игры");

                _context.Games.Add(new()
                {
                    SteamGameId = request.SteamGameId,
                    Title = response.name,
                    GameIconUrl = request.IconUrlHash
                });

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

        #endregion POST

        #region PUT

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPut(Name = "PutGameInfo")]
        public async Task<ActionResult> PutGameInfo(PutGameRequest request)
        {
            try
            {
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                if (!await IsGameIconExists(game.SteamGameId, request.IconUrlHash))
                    return NotFound("Указан неверный хэш-код иконки игры");
                game.GameIconUrl = request.IconUrlHash;

                game.Title = request.Title;

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

        #region DELETE

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpDelete(Name = "DeleteGame")]
        public async Task<ActionResult> DeleteGame(DeleteGameRequest request)
        {
            try
            {
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                _context.Games.Remove(game);

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

        #endregion DELETE
    }
}
