using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using backend.BL.Encryption;
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
        private readonly IEncryptionKeyStorage _encryptionKeyStorage;
        private readonly IEncryptor _encryptor;

        public AccessTokenApiController(
            IMongoDatabase db,
            Func<string, OAuthSignInManager> signInManagerFactory,
            IEncryptionKeyStorage encryptionKeyStorage,
            IEncryptor encryptor)
        {
            _db = db;
            _signInManagerFactory = signInManagerFactory;
            _encryptionKeyStorage = encryptionKeyStorage;
            _encryptor = encryptor;
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

            var encryptionKey = await _encryptionKeyStorage.GetKey(userId);
            if (accessToken?.TokenExpiration <= DateTime.UtcNow)
            {
                var refreshTokenBytes = Convert.FromBase64String(accessToken.RefreshToken);
                var decryptedRefreshToken = _encryptor.DecryptData(refreshTokenBytes, encryptionKey);
                var refreshToken = Encoding.UTF8.GetString(decryptedRefreshToken);

                var validAccessToken = await manager.GetRefreshTokenResponse(provider, refreshToken);
                if (validAccessToken != null)
                {
                    accessToken.Token = validAccessToken.AccessToken;
                    var accessTokenBytes = Encoding.UTF8.GetBytes(accessToken.Token);
                    var encryptedBytes = _encryptor.EncryptData(accessTokenBytes, encryptionKey);

                    var filter = Builders<AccessToken>.Filter.Eq(t => t.Id, accessToken.Id);
                    var update = Builders<AccessToken>.Update
                        .Set(t => t.Token, Convert.ToBase64String(encryptedBytes))
                        .Set(t => t.TokenExpiration, DateTime.UtcNow.AddSeconds(validAccessToken.ExpiresIn))
                        .Set(t => t.ConnectedAt, DateTime.UtcNow);

                    await _db.GetCollection<AccessToken>("accessTokens").UpdateOneAsync(filter, update);
                }
            }
            else
            {
                var accessTokenBytes = Convert.FromBase64String(accessToken.Token);
                var decryptedAccessToken = _encryptor.DecryptData(accessTokenBytes, encryptionKey);
                accessToken.Token = Encoding.UTF8.GetString(decryptedAccessToken);
            }

            return Ok(new
            {
                AccessToken = accessToken?.Token
            });
        }
    }
}