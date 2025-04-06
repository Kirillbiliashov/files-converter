using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Models.DTO;
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

        private readonly IMongoDatabase _db;
        private readonly AzureBlobService _azureBlobService;

        public UserApiController(IMongoDatabase db, AzureBlobService azureBlobService)
        {
            _db = db;
            _azureBlobService = azureBlobService;
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

            var match = new BsonDocument("$match", new BsonDocument("_id", ObjectId.Parse(userId)));
            var lookup = new BsonDocument("$lookup",
                new BsonDocument
                {
                    { "from", "accessTokens" },
                    { "localField", "_id" },
                    { "foreignField", "userId" },
                    { "as", "providers" }
                });
            var project = new BsonDocument("$project",
                new BsonDocument
                {
                    { "providers._id", 0 },
                    { "_id", 0 },
                    { "providers.refreshToken", 0 },
                });

            var pipeline = new[] { match, lookup, project };
            var userInfo = _db.GetCollection<User>("users").Aggregate<UserInfo>(pipeline).FirstOrDefault();

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

            var conversionsColl = _db.GetCollection<Conversion>("conversions");

            var conversionUrls = await conversionsColl
            .Find(c => c.UserId == ObjectId.Parse(userId) && c.OutputUrl != null)
            .Project(c => c.OutputUrl)
            .ToListAsync();

            var deleteUrlTasks = conversionUrls.Select(_azureBlobService.DeleteFileAsync);
            await Task.WhenAll(deleteUrlTasks);

            var deleteFileInteractionsTask = _db.GetCollection<FileInteraction>("fileInteractions")
            .DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));

            var deleteConversionsTask = conversionsColl
            .DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));

            var deleteAccessTokensTask = _db.GetCollection<AccessToken>("accessTokens")
            .DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));

            var deleteUserTask = _db.GetCollection<User>("users")
            .DeleteOneAsync(i => i.Id == ObjectId.Parse(userId));

            await Task.WhenAll(
                deleteFileInteractionsTask,
                deleteConversionsTask,
                deleteAccessTokensTask,
                deleteUserTask);

            return NoContent();
        }
    }
}