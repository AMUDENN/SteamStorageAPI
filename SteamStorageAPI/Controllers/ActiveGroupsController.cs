﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ActiveGroupsController : ControllerBase
    {
        #region Enums
        public enum ActiveGroupOrderName
        {
            Title, Count, BuySum, CurrentSum, Change
        }
        #endregion Enums

        #region Fields
        private readonly ILogger<ActiveGroupsController> _logger;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ActiveGroupOrderName, Func<ActiveGroup, object>> _orderNames;
        #endregion Fields

        #region Constructor
        public ActiveGroupsController(ILogger<ActiveGroupsController> logger, ISkinService skinService, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [ActiveGroupOrderName.Title] = x => x.Title,
                [ActiveGroupOrderName.Count] = x => _context.Entry(x).Collection(x => x.Actives).Query().Count(),
                [ActiveGroupOrderName.BuySum] = x => _context.Entry(x).Collection(x => x.Actives).Query().Sum(x => x.Count * x.BuyPrice),
                [ActiveGroupOrderName.CurrentSum] = x => _context.Entry(x).Collection(x => x.Actives).Query().Include(x => x.Skin).Sum(x => x.Count * _skinService.GetCurrentPrice(x.Skin)),
                [ActiveGroupOrderName.Change] = x =>
                {
                    decimal buySum = _context.Entry(x).Collection(x => x.Actives).Query().Sum(x => x.Count * x.BuyPrice);
                    decimal currentSum = _context.Entry(x).Collection(x => x.Actives).Query().Include(x => x.Skin).Sum(x => x.Count * _skinService.GetCurrentPrice(x.Skin));

                    return buySum == 0 ? 0 : (currentSum - buySum) / buySum;
                }
            };
        }
        #endregion Constructor

        #region Records
        public record ActiveGroupsResponse(int Id, string Title, string Description, string Colour, decimal? GoalSum);
        public record ActiveGroupDynamicsResponse(int Id, DateTime DateUpdate, decimal Sum);
        public record ActiveGroupsCountResponse(int Count);
        public record GetActiveGroupsRequest(ActiveGroupOrderName? OrderName, bool? IsAscending);
        public record GetActiveGroupDynamicRequest(int GroupId, int DaysDynamic);
        public record PostActiveGroupRequest(string Title, string? Description, string? Colour, decimal? GoalSum);
        public record PutActiveGroupRequest(int GroupId, string Title, string? Description, string? Colour, decimal? GoalSum);
        public record DeleteActiveGroupRequest(int GroupId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetActiveGroups")]
        public ActionResult<IEnumerable<ActiveGroupsResponse>> GetActiveGroups([FromQuery] GetActiveGroupsRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<ActiveGroup> groups = _context.Entry(user).Collection(x => x.ActiveGroups).Query();

                if (request.OrderName != null && request.IsAscending != null)
                    groups = (bool)request.IsAscending ? groups.OrderBy(_orderNames[(ActiveGroupOrderName)request.OrderName])
                                                       : groups.OrderByDescending(_orderNames[(ActiveGroupOrderName)request.OrderName]);

                return Ok(groups.Select(x =>
                                    new ActiveGroupsResponse(x.Id,
                                                             x.Title,
                                                             x.Description ?? string.Empty,
                                                             $"#{x.Colour ?? ProgramConstants.BaseActiveGroupColour}",
                                                             x.GoalSum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetActiveGroupDynamics")]
        public ActionResult<IEnumerable<ActiveGroupDynamicsResponse>> GetActiveGroupDynamics([FromQuery] GetActiveGroupDynamicRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user)
                                             .Collection(u => u.ActiveGroups)
                                             .Query()
                                             .FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

                DateTime startDate = DateTime.Now.AddDays(-request.DaysDynamic);

                return Ok(_context.Entry(group)
                                  .Collection(s => s.ActiveGroupsDynamics)
                                  .Query()
                                  .Where(x => x.DateUpdate > startDate)
                                  .Select(x =>
                                        new ActiveGroupDynamicsResponse(x.Id,
                                                                        x.DateUpdate,
                                                                        x.Sum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetActiveGroupsCount")]
        public ActionResult<ActiveGroupsCountResponse> GetActiveGroupsCount()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ActiveGroupsCountResponse(_context.Entry(user).Collection(x => x.ActiveGroups).Query().Count()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET

        #region POST
        [HttpPost(Name = "PostActiveGroup")]
        public async Task<ActionResult> PostActiveGroup(PostActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ActiveGroups.Add(new ActiveGroup()
                {
                    UserId = user.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Colour = request.Colour,
                    GoalSum = request.GoalSum
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
        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroup(PutActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user).Collection(u => u.ActiveGroups).Query().FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                group.Title = request.Title;
                group.Description = request.Description;
                group.Colour = request.Colour;
                group.GoalSum = request.GoalSum;

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
        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(DeleteActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user).Collection(u => u.ActiveGroups).Query().FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                _context.ActiveGroups.Remove(group);

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