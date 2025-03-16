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

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoDatabase _db;

        private readonly Func<string, OAuthSignInManager> _signInManagerFactory;

        public AuthController(
            IConfiguration configuration,
            IMongoDatabase db,
            Func<string, OAuthSignInManager> signInManagerFactory)
        {
            _configuration = configuration;
            _db = db;
            _signInManagerFactory = signInManagerFactory;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _db.GetCollection<User>("users").CountDocumentsAsync(u => u.Email == request.Email) > 0)
            {
                return BadRequest("User already exists.");
            }

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Created = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            await _db.GetCollection<User>("users").InsertOneAsync(user);

            var token = GenerateJwtToken(user.IdInternal);
            return Ok(new { token });
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
            return Ok(new { token });
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
            var accessToken = await manager.GetAccessCode(body.provider, body.code);
            var userInfo = await manager.GetUserInfo(accessToken);

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

            var token = GenerateJwtToken(user.IdInternal);

            return Ok(new { token });
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

            var tokenResponse =  new JwtSecurityTokenHandler().WriteToken(token);
            return tokenResponse;
        }


    }

    public class ProcessOAuthLoginBody
    {
        public string code { get; set; }

        public string provider { get; set; }
    }

}