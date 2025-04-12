using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Models.DTO;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserApiController : ControllerBase
    {
        private readonly AzureBlobService _azureBlobService;

        private readonly IUserRepository _userRepository;
        private readonly IConversionRepository _conversionRepository;
        private readonly IFileInteractionsRepository _fileInteractionsRepository;
        private readonly IAccessTokenRepository _accessTokenRepository;

        public UserApiController(
            IUserRepository userRepository,
            IConversionRepository conversionRepository,
            IFileInteractionsRepository fileInteractionsRepository,
            IAccessTokenRepository accessTokenRepository,
              AzureBlobService azureBlobService)
        {
            _azureBlobService = azureBlobService;
            _userRepository = userRepository;
            _conversionRepository = conversionRepository;
            _fileInteractionsRepository = fileInteractionsRepository;
            _accessTokenRepository = accessTokenRepository;
        }

        [Authorize]
        [HttpGet("")]
        public async Task<IActionResult> GetUserInfo()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var userInfo = await _userRepository.GetUserInfo(userId);
            return Ok(userInfo);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var conversionUrls = await _conversionRepository.GetConversionUrls(userId);
            var deleteUrlTasks = conversionUrls.Select(_azureBlobService.DeleteFileAsync);
            await Task.WhenAll(deleteUrlTasks);

            var deleteFileInteractionsTask = _fileInteractionsRepository.DeleteUserInteractions(userId);
            var deleteConversionsTask = _conversionRepository.DeleteConversions(userId);
            var deleteAccessTokensTask = _accessTokenRepository.DeleteAccessTokens(userId);
            var deleteUserTask = _userRepository.DeleteUser(userId);

            await Task.WhenAll(
                deleteFileInteractionsTask,
                deleteConversionsTask,
                deleteAccessTokensTask,
                deleteUserTask);

            return NoContent();
        }

        [Authorize]
        [HttpPost("update-preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesBody body)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await _userRepository.UpdateUserPreferences(userId, body.DeleteFilesAutomatically);
            return Ok();
        }
    }

    public class UpdatePreferencesBody
    {
        public bool DeleteFilesAutomatically { get; set; }
    }

}