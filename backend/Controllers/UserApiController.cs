using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserApiController : ControllerBase
    {

        private readonly IMongoDatabase _db;

        public UserApiController(IMongoDatabase db)
        {
            _db = db;
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
    }
}