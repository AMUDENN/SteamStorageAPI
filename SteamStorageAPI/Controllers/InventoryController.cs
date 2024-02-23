﻿using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class InventoryController : ControllerBase
    {
        #region Enums

        public enum InventoryOrderName
        {
            Title,
            Count,
            Price,
            Sum
        }

        #endregion Enums

        #region Fields

        private readonly ILogger<InventoryController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<InventoryOrderName, Func<Inventory, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public InventoryController(
            ILogger<InventoryController> logger, 
            IHttpClientFactory httpClientFactory,
            ISkinService skinService, 
            IUserService userService, 
            SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                // TODO: Сортировка по параметрам!
            };
        }

        #endregion Constructor

        #region Records

        public record InventoryResponse(
            int Id,
            BaseSkinResponse Skin,
            int Count);

        public record InventoryPagesCountResponse(
            int Count);

        public record SavedInventoriesCountResponse(
            int Count);

        public record GetInventoryRequest(
            int? GameId,
            string? Filter,
            InventoryOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        public record GetInventoryPagesCountRequest(
            int? GameId,
            string? Filter,
            int PageSize);

        public record GetSavedInventoriesCountRequest(
            int? GameId,
            string? Filter);

        public record RefreshInventoryRequest(
            int GameId);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение списка инвенторя
        /// </summary>
        /// <response code="200">Возвращает список предметов в инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetInventory")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<InventoryResponse>>> GetInventory(
            [FromQuery] GetInventoryRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                    throw new("Размер и номер страницы не могут быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<Inventory> inventories = _context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!)));

                if (request is { OrderName: not null, IsAscending: not null })
                    inventories = (bool)request.IsAscending
                        ? inventories.OrderBy(_orderNames[(InventoryOrderName)request.OrderName])
                        : inventories.OrderByDescending(_orderNames[(InventoryOrderName)request.OrderName]);

                inventories = inventories.Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize);

                return Ok(inventories.Select(async x =>
                    new InventoryResponse(x.Id, await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                        x.Count)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества страниц инвенторя
        /// </summary>
        /// <response code="200">Возвращает количество страниц инвенторя</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetInventoryPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InventoryPagesCountResponse>> GetInventoryPagesCount(
            [FromQuery] GetInventoryPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.PageSize <= 0)
                    throw new("Размер страницы не может быть меньше или равен нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<Inventory> inventories = _context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!)));

                return Ok(new InventoryPagesCountResponse(
                    (int)Math.Ceiling((double)inventories.Count() / request.PageSize)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества элементов в инвентаре
        /// </summary>
        /// <response code="200">Возвращает количество элементов в инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetSavedInventoriesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SavedInventoriesCountResponse>> GetSavedInventoriesCount(
            [FromQuery] GetSavedInventoriesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new SavedInventoriesCountResponse(await _context
                    .Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                     && (string.IsNullOrEmpty(request.Filter) ||
                                         x.Skin.Title.Contains(request.Filter!)), cancellationToken)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Обновление инвенторя
        /// </summary>
        /// <response code="200">Инвентарь успешно обновлён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует или пользователь не найден</response>
        [HttpPost(Name = "RefreshInventory")]
        public async Task<ActionResult> RefreshInventory(
            RefreshInventoryRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Game? game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                _context.Inventories.RemoveRange(_context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => x.Skin.GameId == game.Id));


                HttpClient client = _httpClientFactory.CreateClient();
                SteamInventoryResponse? response =
                    await client.GetFromJsonAsync<SteamInventoryResponse>(
                        SteamApi.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000), cancellationToken);

                if (response is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                foreach (InventoryDescription item in response.descriptions)
                {
                    if (item is { marketable: 0, tradable: 0 })
                        continue;

                    Skin? skin =
                        await _context.Skins.FirstOrDefaultAsync(x => x.MarketHashName == item.market_hash_name,
                            cancellationToken);
                    if (skin is null)
                    {
                        skin = new()
                        {
                            GameId = game.Id,
                            MarketHashName = item.market_hash_name,
                            Title = item.name,
                            SkinIconUrl = item.icon_url
                        };
                        await _context.Skins.AddAsync(skin, cancellationToken);
                    }


                    Inventory? inventory =
                        await _context.Inventories.FirstOrDefaultAsync(x => x.SkinId == skin.Id, cancellationToken);

                    if (inventory is null)
                        await _context.Inventories.AddAsync(new()
                        {
                            User = user,
                            Skin = skin,
                            Count = 1
                        }, cancellationToken);
                    else
                        inventory.Count++;
                }

                await _context.SaveChangesAsync(cancellationToken);

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
    }
}
