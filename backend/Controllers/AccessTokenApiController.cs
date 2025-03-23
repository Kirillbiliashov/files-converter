using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using backend.BL.Integrations;
using backend.Models.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/access-token")]
    public class AccessTokenApiController : ControllerBase
    {
        private readonly IMongoDatabase _db;
        private readonly Func<string, OAuthSignInManager> _signInManagerFactory;

        public AccessTokenApiController(
            IMongoDatabase db,
            Func<string, OAuthSignInManager> signInManagerFactory)
        {
            _db = db;
            _signInManagerFactory = signInManagerFactory;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAccessToken(string provider)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var manager = _signInManagerFactory(provider);

            var accessToken = await _db.GetCollection<AccessToken>("accessTokens")
            .Find(t => t.UserId == ObjectId.Parse(userId) && t.Provider == provider)
            .SingleOrDefaultAsync();

            if (accessToken?.TokenExpiration >= DateTime.UtcNow)
            {
                var validAccessToken = await manager.GetRefreshTokenResponse(provider, accessToken.RefreshToken);

                if (validAccessToken != null)
                {
                    accessToken.Token = validAccessToken.AccessToken;
                    var filter = Builders<AccessToken>.Filter.Eq(t => t.Id, accessToken.Id);
                    var update = Builders<AccessToken>.Update
                        .Set(t => t.Token, validAccessToken.AccessToken)
                        .Set(t => t.TokenExpiration, DateTime.UtcNow.AddSeconds(validAccessToken.ExpiresIn))
                        .Set(t => t.ConnectedAt, DateTime.UtcNow);

                    await _db.GetCollection<AccessToken>("accessTokens").UpdateOneAsync(filter, update);
                }
            }

            return Ok(new
            {
                AccessToken = accessToken?.Token
            });
        }
    }
}