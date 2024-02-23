using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Games;
using SteamStorageAPI.Utilities.Steam;
using System.Net;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]

    public class GamesController : ControllerBase
    {
        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public GamesController(
            IHttpClientFactory httpClientFactory,
            SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record GameResponse(
            int Id,
            int SteamGameId,
            string Title,
            string GameIconUrl);

        public record PostGameRequest(
            int SteamGameId,
            string IconUrlHash);

        public record PutGameRequest(
            int GameId,
            string IconUrlHash,
            string Title);

        public record DeleteGameRequest(
            int GameId);

        #endregion Records

        #region Methods

        private async Task<bool> IsGameIconExistsAsync(
            int steamGameId,
            string iconUrlHash,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            HttpResponseMessage response =
                await client.GetAsync(SteamApi.GetGameIconUrl(steamGameId, iconUrlHash), cancellationToken);
            return response.StatusCode == HttpStatusCode.OK;
        }

        private async Task<SteamGameResponse?> GetGameResponseAsync(
            int steamGameId,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            return await client.GetFromJsonAsync<SteamGameResponse>(SteamApi.GetGameInfoUrl(steamGameId),
                cancellationToken);
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка игр
        /// </summary>
        /// <response code="200">Возвращает список игр</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetGames")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<GameResponse>> GetGames(
            CancellationToken cancellationToken = default)
        {
            return Ok(_context.Games.Select(x =>
                new GameResponse(x.Id,
                    x.SteamGameId,
                    x.Title,
                    SteamApi.GetGameIconUrl(x.SteamGameId, x.GameIconUrl))));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление новой игры
        /// </summary>
        /// <response code="200">Игра успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostGame")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PostGame(
            PostGameRequest request,
            CancellationToken cancellationToken = default)
        {
            SteamGameResponse response = await GetGameResponseAsync(request.SteamGameId, cancellationToken) ??
                                         throw new HttpResponseException(StatusCodes.Status400BadRequest,
                                             "Указан неверный id игры");

            if (!await IsGameIconExistsAsync(request.SteamGameId, request.IconUrlHash, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status400BadRequest, "Указан неверный хэш-код иконки игры");

            await _context.Games.AddAsync(new()
            {
                SteamGameId = request.SteamGameId,
                Title = response.name,
                GameIconUrl = request.IconUrlHash
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение игры
        /// </summary>
        /// <response code="200">Игры успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutGameInfo")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PutGameInfo(
            PutGameRequest request,
            CancellationToken cancellationToken = default)
        {
            Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "Игры с таким Id не существует");

            if (!await IsGameIconExistsAsync(game.SteamGameId, request.IconUrlHash, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status400BadRequest, "Указан неверный хэш-код иконки игры");

            game.GameIconUrl = request.IconUrlHash;
            game.Title = request.Title;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление игры
        /// </summary>
        /// <response code="200">Игры успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует</response>
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteGame")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> DeleteGame(
            DeleteGameRequest request,
            CancellationToken cancellationToken = default)
        {
            Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "Игры с таким Id не существует");

            _context.Games.Remove(game);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
