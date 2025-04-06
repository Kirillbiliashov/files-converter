using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using backend.BL.Integrations;
using backend.Models.Db;
using backend.Models.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Google.Apis.Auth;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using backend.BL.Encryption;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly int _tokenExpiryMins;
        private readonly IMongoDatabase _db;
        private readonly IEncryptionKeyStorage _encryptionKeyStorage;
        private readonly IEncryptor _encryptor;

        private readonly Func<string, OAuthSignInManager> _signInManagerFactory;

        public DateTime JwtTokenExpirationTime => DateTime.UtcNow.AddMinutes(_tokenExpiryMins);

        public AuthController(
            IConfiguration configuration,
            IMongoDatabase db,
            Func<string, OAuthSignInManager> signInManagerFactory,
            IEncryptionKeyStorage encryptionKeyStorage,
            IEncryptor encryptor)
        {
            _configuration = configuration;
            _tokenExpiryMins = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]);
            _db = db;
            _signInManagerFactory = signInManagerFactory;
            _encryptionKeyStorage = encryptionKeyStorage;
            _encryptor = encryptor;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _db.GetCollection<User>("users").CountDocumentsAsync(u => u.Email == request.Email) > 0)
            {
                return BadRequest("User already exists.");
            }

            var hasher = new PasswordHasher<User>();
            var currentDate = DateTime.UtcNow;
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Created = currentDate,
                LastLogin = currentDate
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            await _db.GetCollection<User>("users").InsertOneAsync(user);
            user.PasswordHash = null;

            var token = GenerateJwtToken(user.IdInternal);
            return Ok(new { token, user });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.GetCollection<User>("users")
            .Find(u => u.Email == request.Email)
            .SingleOrDefaultAsync();

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            var token = GenerateJwtToken(user.IdInternal);
            user.PasswordHash = null;

            await UpdateUserLastLogin(user.Id);

            return Ok(new { token, user, tokenExpirationDate = JwtTokenExpirationTime });
        }

        [HttpGet("login/oauth")]
        public async Task<IActionResult> InitiateOAuthFlow(string provider)
        {
            var manager = _signInManagerFactory(provider);
            var redirectUrl = manager.GetLoginUrl(provider);
            return Redirect(redirectUrl);
        }

        [HttpPost("login/oauth/process")]
        public async Task<IActionResult> ProcessGoogleLogin([FromBody] ProcessOAuthLoginBody body)
        {
            var manager = _signInManagerFactory(body.provider);
            var accessTokenResponse = await manager.GetAccessTokenResponse(body.provider, body.code);
            var userInfo = await manager.GetUserInfo(accessTokenResponse?.AccessToken);

            if (userInfo == null)
            {
                return Unauthorized();
            }

            var user = await _db.GetCollection<User>("users")
            .Find(u => u.Email == userInfo.Email)
            .SingleOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    Username = userInfo.Username,
                    Email = userInfo.Email
                };
                await _db.GetCollection<User>("users").InsertOneAsync(user);
            }

            await UpsertAccessToken(accessTokenResponse, user.IdInternal, body.provider);
            await UpdateUserLastLogin(user.Id);

            var token = GenerateJwtToken(user.IdInternal);

            return Ok(new { token, user, tokenExpirationDate = JwtTokenExpirationTime });
        }

        private async Task UpdateUserLastLogin(ObjectId userId)
        {
            var filter = Builders<User>.Filter.Eq(doc => doc.Id, userId);
            var update = Builders<User>.Update.Set(doc => doc.LastLogin, DateTime.UtcNow);
            await _db.GetCollection<User>("users").UpdateOneAsync(filter, update);
        }

        private async Task UpsertAccessToken(AccessTokenResponse response, string userId, string provider)
        {
            var parsedUserId = ObjectId.Parse(userId);
            var filter = Builders<AccessToken>.Filter.Eq(t => t.UserId, parsedUserId);

            var encryptionKey = await _encryptionKeyStorage.GetKey(userId);
            var accessTokenBytes = Encoding.UTF8.GetBytes(response.AccessToken);
            var encryptedAccessToken = _encryptor.EncryptData(Encoding.UTF8.GetBytes(response.AccessToken), encryptionKey);
            var refreshTokenBytes = Encoding.UTF8.GetBytes(response.RefreshToken);
            var encryptedRefreshToken = _encryptor.EncryptData(Encoding.UTF8.GetBytes(response.AccessToken), encryptionKey);

            var update = Builders<AccessToken>.Update
                .Set(t => t.Token, Convert.ToBase64String(encryptedAccessToken))
                .Set(t => t.RefreshToken, Convert.ToBase64String(encryptedRefreshToken))
                .Set(t => t.TokenExpiration, DateTime.UtcNow.AddSeconds(response.ExpiresIn))
                .Set(t => t.ConnectedAt, DateTime.UtcNow)
                .Set(t => t.Provider, provider)
                .SetOnInsert(t => t.UserId, parsedUserId);

            var options = new UpdateOptions { IsUpsert = true };

            await _db.GetCollection<AccessToken>("accessTokens").UpdateOneAsync(filter, update, options);
        }

        private string GenerateJwtToken(string userId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"]);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            var tokenResponse = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenResponse;
        }

    }

    public class ProcessOAuthLoginBody
    {
        public string code { get; set; }

        public string provider { get; set; }
    }

}