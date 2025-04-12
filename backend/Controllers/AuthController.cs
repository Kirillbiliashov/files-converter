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
using backend.Repositories;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
         private readonly IAccessTokenRepository _accessTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly int _tokenExpiryMins;
        private readonly IEncryptionKeyStorage _encryptionKeyStorage;
        private readonly IEncryptor _encryptor;
        private readonly JwtTokenService _jwtTokenService;

        private readonly Func<string, OAuthSignInManager> _signInManagerFactory;

        public DateTime JwtTokenExpirationTime => DateTime.UtcNow.AddMinutes(_tokenExpiryMins);

        public AuthController(
            IUserRepository userRepository,
            IAccessTokenRepository accessTokenRepository,
            IConfiguration configuration,
            Func<string, OAuthSignInManager> signInManagerFactory,
            IEncryptionKeyStorage encryptionKeyStorage,
            JwtTokenService jwtTokenService,
            IEncryptor encryptor)
        {
            _userRepository = userRepository;
            _accessTokenRepository = accessTokenRepository;
            _configuration = configuration;
            _tokenExpiryMins = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]);
            _signInManagerFactory = signInManagerFactory;
            _encryptionKeyStorage = encryptionKeyStorage;
            _encryptor = encryptor;
            _jwtTokenService = jwtTokenService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _userRepository.GetUsersCount(request.Email) > 0)
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

            await _userRepository.AddUser(user);
            user.PasswordHash = null;

            var token = _jwtTokenService.GenerateJwtToken(user.IdInternal);
            return Ok(new { token, user });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
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

            var token = _jwtTokenService.GenerateJwtToken(user.IdInternal);
            user.PasswordHash = null;

            await _userRepository.UpdateLastLoginTime(user.IdInternal);

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

            var user = await _userRepository.GetUserByEmail(userInfo.Email);
            if (user == null)
            {
                user = new User
                {
                    Username = userInfo.Username,
                    Email = userInfo.Email
                };
                await _userRepository.AddUser(user);
            }

            await UpsertAccessToken(accessTokenResponse, user.IdInternal, body.provider);
            await _userRepository.UpdateLastLoginTime(user.IdInternal);

            var token = _jwtTokenService.GenerateJwtToken(user.IdInternal);

            return Ok(new { token, user, tokenExpirationDate = JwtTokenExpirationTime });
        }

        private async Task UpsertAccessToken(AccessTokenResponse response, string userId, string provider)
        {
            var encryptionKey = await _encryptionKeyStorage.GetKey(userId);
            var encryptedAccessToken = _encryptor.EncryptData(Encoding.UTF8.GetBytes(response.AccessToken), encryptionKey);
            var encryptedRefreshToken = _encryptor.EncryptData(Encoding.UTF8.GetBytes(response.AccessToken), encryptionKey);

            var accessToken = new AccessToken 
            {
                Token = Convert.ToBase64String(encryptedAccessToken),
                RefreshToken = Convert.ToBase64String(encryptedRefreshToken),
                ConnectedAt = DateTime.UtcNow,
                Provider = provider
            };

            await _accessTokenRepository.UpsertUserAccessToken(userId, accessToken);
        }


    }

    public class ProcessOAuthLoginBody
    {
        public string code { get; set; }

        public string provider { get; set; }
    }

}