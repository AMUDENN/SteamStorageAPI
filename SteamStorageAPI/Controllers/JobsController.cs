using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class JobsController : ControllerBase
{
    #region Fields

    private readonly ISchedulerFactory _schedulerFactory;

    #endregion Fields

    #region Constructor

    public JobsController(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    #endregion Constructor

    #region POST

    /// <summary>
    /// Manually trigger a background job
    /// </summary>
    /// <response code="200">The job was successfully triggered</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No job with the given name exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPost(Name = "TriggerJob")]
    public async Task<ActionResult> TriggerJob(
        TriggerJobRequest request,
        CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        JobKey key = new(request.JobName.ToString());

        if (!await scheduler.CheckExists(key, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                $"No job with the name '{request.JobName}' exists");

        await scheduler.TriggerJob(key, cancellationToken);

        return Ok();
    }

    #endregion POST
}