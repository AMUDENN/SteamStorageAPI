using Microsoft.AspNetCore.Mvc;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CheckController : ControllerBase
    {
        #region GET

        /// <summary>
        /// Проверка работоспособности API (только для отладки!)
        /// </summary>
        /// <response code="200">API работает</response>
        [HttpGet(Name = "GetApiStatus")]
        public ActionResult GetApiStatus()
        {
            return Ok();
        }

        #endregion GET
    }
}
