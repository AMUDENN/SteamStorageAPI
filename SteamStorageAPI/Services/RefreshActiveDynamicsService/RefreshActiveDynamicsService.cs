using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.RefreshActiveDynamicsService;

public class RefreshActiveDynamicsService : IRefreshActiveDynamicsService
{
    #region Fields
    
    private readonly ISkinService _skinService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshActiveDynamicsService(
        ISkinService skinService,
        SteamStorageContext context)
    {
        _skinService = skinService;
        _context = context;
    }

    #endregion Constructor
    
    #region Methods

    public async Task RefreshActiveDynamicsAsync(
        CancellationToken cancellationToken = default)
    {
        if (await _context.ActiveGroupsDynamics.CountAsync(x => x.DateUpdate.Date == DateTime.Today, cancellationToken) ==
            await _context.ActiveGroups.CountAsync(cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "Сегодня уже было выполнено обновление ActiveDynamics!");
        
        //TODO:
        
    }
    
    #endregion Methods
}