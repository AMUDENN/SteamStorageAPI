using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.PageService;

public class PagesService : IPageService
{
    #region Fields

    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public PagesService(
        SteamStorageContext context)
    {
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<PagesResponse> GetPagesAsync(
        CancellationToken cancellationToken = default)
    {
        List<Page> pages = await _context.Pages.AsNoTracking().ToListAsync(cancellationToken);

        return new PagesResponse(pages.Count, pages.Select(x => new PageResponse(x.Id, x.Title)));
    }

    public async Task<PageResponse> GetCurrentStartPageAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        Page page = await _context.Pages.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == user.StartPageId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "A page with this Id does not exist");

        return new PageResponse(page.Id, page.Title);
    }

    public async Task SetStartPageAsync(
        User user,
        SetPageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Pages.AnyAsync(x => x.Id == request.PageId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A page with this Id does not exist");

        user.StartPageId = request.PageId;
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}