using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.ArchiveGroupService;

public class ArchiveGroupService : IArchiveGroupService
{
    #region Fields

    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public ArchiveGroupService(SteamStorageContext context)
    {
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public ArchiveGroupResponse GetArchiveGroupResponse(ArchiveGroup group)
    {
        return new ArchiveGroupResponse(group.Id,
            group.Title,
            group.Description,
            $"#{group.Colour ?? ArchiveGroup.BASE_ARCHIVE_GROUP_COLOUR}",
            group.Archives.Sum(y => y.Count),
            group.Archives.Sum(y => y.BuyPrice * y.Count),
            group.Archives.Sum(y => y.SoldPrice * y.Count),
            group.Archives.Sum(y => y.BuyPrice * y.Count) != 0
                ? (group.Archives.Sum(y => y.SoldPrice * y.Count)
                   - group.Archives.Sum(y => y.BuyPrice * y.Count))
                  / group.Archives.Sum(y => y.BuyPrice * y.Count)
                : 0,
            group.DateCreation);
    }

    public async Task<IEnumerable<ArchiveGroupResponse>> GetArchiveGroupsResponseAsync(
        IQueryable<ArchiveGroup> groups,
        CancellationToken cancellationToken = default)
    {
        return await groups.Select(x => new ArchiveGroupResponse(
            x.Id,
            x.Title,
            x.Description,
            $"#{x.Colour ?? ArchiveGroup.BASE_ARCHIVE_GROUP_COLOUR}",
            x.Archives.Sum(y => y.Count),
            x.Archives.Sum(y => y.BuyPrice * y.Count),
            x.Archives.Sum(y => y.SoldPrice * y.Count),
            x.Archives.Sum(y => y.BuyPrice * y.Count) != 0
                ? (x.Archives.Sum(y => y.SoldPrice * y.Count)
                   - x.Archives.Sum(y => y.BuyPrice * y.Count))
                  / x.Archives.Sum(y => y.BuyPrice * y.Count)
                : 0,
            x.DateCreation)).ToListAsync(cancellationToken);
    }

    public IQueryable<ArchiveGroup> GetArchiveGroupsQuery(User user)
    {
        return _context.Entry(user)
            .Collection(x => x.ArchiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Archives);
    }

    public IEnumerable<ArchiveGroupResponse> ApplyOrder(
        IEnumerable<ArchiveGroupResponse> groups,
        ArchiveGroupOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return groups.OrderBy(x => x.Id);

        return orderName switch
        {
            ArchiveGroupOrderName.Title => isAscending.Value
                ? groups.OrderBy(x => x.Title)
                : groups.OrderByDescending(x => x.Title),
            ArchiveGroupOrderName.Count => isAscending.Value
                ? groups.OrderBy(x => x.Count)
                : groups.OrderByDescending(x => x.Count),
            ArchiveGroupOrderName.BuySum => isAscending.Value
                ? groups.OrderBy(x => x.BuySum)
                : groups.OrderByDescending(x => x.BuySum),
            ArchiveGroupOrderName.SoldSum => isAscending.Value
                ? groups.OrderBy(x => x.SoldSum)
                : groups.OrderByDescending(x => x.SoldSum),
            ArchiveGroupOrderName.Change => isAscending.Value
                ? groups.OrderBy(x => x.Change)
                : groups.OrderByDescending(x => x.Change),
            _ => groups.OrderBy(x => x.Id)
        };
    }

    public async Task<ArchiveGroupsStatisticResponse> GetArchiveGroupsStatisticAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Archive> archives = _context.Entry(user)
            .Collection(x => x.ArchiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Archives).ThenInclude(x => x.Skin).ThenInclude(x => x.Game)
            .SelectMany(x => x.Archives);

        List<Game> games = await archives
            .Select(x => x.Skin.Game)
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToListAsync(cancellationToken);

        int archivesCount = await archives.SumAsync(x => x.Count, cancellationToken);
        decimal buySum = await archives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken);
        decimal soldSum = await archives.SumAsync(x => x.SoldPrice * x.Count, cancellationToken);

        List<ArchiveGroupsGameCountResponse> gamesCountResponse = games.Select(item =>
            new ArchiveGroupsGameCountResponse(
                item.Title,
                archivesCount == 0
                    ? 0
                    : archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count) / (decimal)archivesCount,
                archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count))).ToList();

        List<ArchiveGroupsGameBuySumResponse> gamesBuySumResponse = games.Select(item =>
            new ArchiveGroupsGameBuySumResponse(
                item.Title,
                buySum == 0
                    ? 0
                    : archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.BuyPrice * x.Count)
                      / buySum,
                archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.BuyPrice * x.Count))).ToList();

        List<ArchiveGroupsGameSoldSumResponse> gamesSoldSumResponse = games.Select(item =>
            new ArchiveGroupsGameSoldSumResponse(
                item.Title,
                soldSum == 0
                    ? 0
                    : archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.SoldPrice * x.Count)
                      / soldSum,
                archives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.SoldPrice * x.Count))).ToList();

        return new ArchiveGroupsStatisticResponse(archivesCount, buySum, soldSum,
            gamesCountResponse, gamesBuySumResponse, gamesSoldSumResponse);
    }

    public async Task PostArchiveGroupAsync(
        User user,
        PostArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        await _context.ArchiveGroups.AddAsync(new ArchiveGroup
        {
            UserId = user.Id,
            Title = request.Title,
            Description = request.Description,
            Colour = request.Colour,
            DateCreation = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutArchiveGroupAsync(
        User user,
        PutArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArchiveGroup group = await _context.Entry(user)
                                 .Collection(u => u.ArchiveGroups)
                                 .Query()
                                 .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                             ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                 "You do not have access to edit this group or the group with this Id does not exist");

        group.Title = request.Title;
        group.Description = request.Description;
        group.Colour = request.Colour;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteArchiveGroupAsync(
        User user,
        int groupId,
        CancellationToken cancellationToken = default)
    {
        ArchiveGroup group = await _context.Entry(user)
                                 .Collection(u => u.ArchiveGroups)
                                 .Query()
                                 .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken)
                             ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                 "You do not have access to edit this group or the group with this Id does not exist");

        _context.ArchiveGroups.Remove(group);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}