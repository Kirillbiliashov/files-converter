using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using backend.BL.Encryption;
using backend.BL.Integrations;
using backend.Models.Db;
using backend.Repositories;
using Google.Apis.Util;
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
        private readonly IAccessTokenRepository _accessTokenRepository;
        private readonly Func<string, OAuthSignInManager> _signInManagerFactory;
        private readonly IEncryptionKeyStorage _encryptionKeyStorage;
        private readonly IEncryptor _encryptor;

        public AccessTokenApiController(
            IAccessTokenRepository accessTokenRepository,
            Func<string, OAuthSignInManager> signInManagerFactory,
            IEncryptionKeyStorage encryptionKeyStorage,
            IEncryptor encryptor)
        {
            _accessTokenRepository = accessTokenRepository;
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

            var accessToken = await _accessTokenRepository.GetAccessToken(userId, provider);

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
                    var encryptedToken = Convert.ToBase64String(encryptedBytes);
                    var tokenExpiration =  DateTime.UtcNow.AddSeconds(validAccessToken.ExpiresIn);
                    
                    await _accessTokenRepository.UpdateAccessToken(accessToken.Id, encryptedToken, tokenExpiration);
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