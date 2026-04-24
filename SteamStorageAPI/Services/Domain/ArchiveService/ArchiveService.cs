using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Extensions;

namespace SteamStorageAPI.Services.Domain.ArchiveService;

public class ArchiveService : IArchiveService
{
    #region Fields

    private readonly ISkinService _skinService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public ArchiveService(
        ISkinService skinService,
        SteamStorageContext context)
    {
        _skinService = skinService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public ArchiveResponse GetArchiveResponse(
        Archive archive)
    {
        return new ArchiveResponse(archive.Id,
            archive.GroupId,
            _skinService.GetBaseSkinResponse(archive.Skin),
            archive.BuyDate,
            archive.SoldDate,
            archive.Count,
            archive.BuyPrice,
            archive.SoldPrice,
            archive.SoldPrice * archive.Count,
            archive.BuyPrice == 0 ? 0 : (archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice,
            archive.Description);
    }

    public async Task<ArchivesResponse> GetArchivesResponseAsync(
        IQueryable<Archive> archives,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int archivesCount = await archives.CountAsync(cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)archivesCount / pageSize);

        List<Archive> archiveList = await archives
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ArchivesResponse(archivesCount,
            pagesCount,
            archiveList
                .Select(x => new ArchiveResponse(
                    x.Id,
                    x.GroupId,
                    _skinService.GetBaseSkinResponse(x.Skin),
                    x.BuyDate,
                    x.SoldDate,
                    x.Count,
                    x.BuyPrice,
                    x.SoldPrice,
                    x.SoldPrice * x.Count,
                    x.BuyPrice == 0 ? 0 : (x.SoldPrice - x.BuyPrice) / x.BuyPrice,
                    x.Description)));
    }

    public IQueryable<Archive> GetArchivesQuery(
        User user,
        int? groupId,
        int? gameId,
        string? filter)
    {
        return _context.Entry(user)
            .Collection(x => x.ArchiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Archives).ThenInclude(x => x.Skin)
            .Include(x => x.Archives).ThenInclude(x => x.Skin.Game)
            .SelectMany(x => x.Archives)
            .Where(x => (gameId == null || x.Skin.GameId == gameId)
                        && (groupId == null || x.GroupId == groupId))
            .WhereMatchFilter(x => x.Skin.Title, filter);
    }

    public IQueryable<Archive> ApplyOrder(
        IQueryable<Archive> archives,
        ArchiveOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return archives.OrderBy(x => x.Id);

        return orderName switch
        {
            ArchiveOrderName.Title => isAscending.Value
                ? archives.OrderBy(x => x.Skin.Title)
                : archives.OrderByDescending(x => x.Skin.Title),
            ArchiveOrderName.Count => isAscending.Value
                ? archives.OrderBy(x => x.Count)
                : archives.OrderByDescending(x => x.Count),
            ArchiveOrderName.BuyPrice => isAscending.Value
                ? archives.OrderBy(x => x.BuyPrice)
                : archives.OrderByDescending(x => x.BuyPrice),
            ArchiveOrderName.SoldPrice => isAscending.Value
                ? archives.OrderBy(x => x.SoldPrice)
                : archives.OrderByDescending(x => x.SoldPrice),
            ArchiveOrderName.SoldSum => isAscending.Value
                ? archives.OrderBy(x => x.SoldPrice * x.Count)
                : archives.OrderByDescending(x => x.SoldPrice * x.Count),
            ArchiveOrderName.Change => isAscending.Value
                ? archives.OrderBy(x => x.BuyPrice == 0 ? 0 : (x.SoldPrice - x.BuyPrice) / x.BuyPrice)
                : archives.OrderByDescending(x => x.BuyPrice == 0 ? 0 : (x.SoldPrice - x.BuyPrice) / x.BuyPrice),
            _ => archives.OrderBy(x => x.Id)
        };
    }

    public async Task PostArchiveAsync(
        User user,
        PostArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "You do not have access to edit this group or the group with this Id does not exist");

        if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A skin with this Id does not exist");

        await _context.Archives.AddAsync(new Archive
        {
            GroupId = request.GroupId,
            Count = request.Count,
            BuyPrice = request.BuyPrice,
            SoldPrice = request.SoldPrice,
            SkinId = request.SkinId,
            Description = request.Description,
            BuyDate = request.BuyDate,
            SoldDate = request.SoldDate
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutArchiveAsync(
        User user,
        PutArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        Archive archive = await _context.Entry(user)
                              .Collection(u => u.ArchiveGroups)
                              .Query()
                              .Include(x => x.Archives)
                              .SelectMany(x => x.Archives)
                              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                          ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                              "You do not have access to edit this archive item or the archive item with this Id does not exist");

        if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "You do not have access to edit this group or the group with this Id does not exist");

        if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A skin with this Id does not exist");

        archive.GroupId = request.GroupId;
        archive.Count = request.Count;
        archive.BuyPrice = request.BuyPrice;
        archive.SoldPrice = request.SoldPrice;
        archive.SkinId = request.SkinId;
        archive.Description = request.Description;
        archive.BuyDate = request.BuyDate;
        archive.SoldDate = request.SoldDate;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteArchiveAsync(
        User user,
        int archiveId,
        CancellationToken cancellationToken = default)
    {
        Archive archive = await _context.Entry(user)
                              .Collection(u => u.ArchiveGroups)
                              .Query()
                              .Include(x => x.Archives)
                              .SelectMany(x => x.Archives)
                              .FirstOrDefaultAsync(x => x.Id == archiveId, cancellationToken)
                          ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                              "You do not have access to edit this archive item or the archive item with this Id does not exist");

        _context.Archives.Remove(archive);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}