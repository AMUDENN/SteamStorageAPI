﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = nameof(Roles.Admin))]
    [Route("api/[controller]/[action]")]
    public class AdminController : ControllerBase
    {
        #region Fields

        private readonly ILogger<AdminController> _logger;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public AdminController(ILogger<AdminController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }

        #endregion Constructor
    }
}
