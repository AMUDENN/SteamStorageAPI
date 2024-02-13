using Microsoft.AspNetCore.Mvc;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CheckController : ControllerBase
    {
        #region GET

        [HttpGet(Name = "GetApiStatus")]
        public ActionResult GetApiStatus()
        {
            return Ok();
        }

        #endregion GET
    }
}
