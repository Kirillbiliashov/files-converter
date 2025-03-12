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

        private readonly GoogleSignInManager _googleSignInManager;

        private readonly DropboxSignInManager _dropboxSignInManager;

        public AuthController(
            IConfiguration configuration,
            IMongoDatabase db,
            GoogleSignInManager googleSignInManager,
            DropboxSignInManager dropboxSignInManager)
        {
            _configuration = configuration;
            _db = db;
            _googleSignInManager = googleSignInManager;
            _dropboxSignInManager = dropboxSignInManager;
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

            var token = GenerateJwtToken(user.Username);
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

            var token = GenerateJwtToken(user.Username);
            return Ok(new { token });
        }

        [HttpGet("login/google")]
        public async Task<IActionResult> LoginWithGoogle()
        {
            var redirectUrl = _googleSignInManager.GetLoginUrl();
            return Redirect(redirectUrl);
        }

        [HttpPost("login/google/process")]
        public async Task<IActionResult> ProcessGoogleLogin([FromBody] ProcessOAuthLoginBody body)
        {
            var accessCode = await _googleSignInManager.GetAccessCode(body.code);
            var payload = await GoogleJsonWebSignature.ValidateAsync(accessCode);

            if (payload == null)
            {
                return Unauthorized();
            }

            var user = await _db.GetCollection<User>("users")
            .Find(u => u.Email == payload.Email)
            .SingleOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    Username = $"{payload.GivenName} {payload.FamilyName}",
                    Email = payload.Email
                };
                await _db.GetCollection<User>("users").InsertOneAsync(user);
            }

            var token = GenerateJwtToken(user.Username);

            return Ok(new { token });
        }


        [HttpGet("login/dropbox")]
        public async Task<IActionResult> LoginWithDropbox()
        {
            var redirectUrl = _dropboxSignInManager.GetLoginUrl();

            return Redirect(redirectUrl);
        }

        [HttpPost("login/dropbox/process")]
        public async Task<IActionResult> ProcessDropboxLogin([FromBody] ProcessOAuthLoginBody body)
        {
            var accessToken = await _dropboxSignInManager.GetAccessToken(body.code);

            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }

            var dropboxUser = await GetDropboxUserInfo(accessToken);

            if (dropboxUser == null)
            {
                return Unauthorized();
            }

            var user = await _db.GetCollection<User>("users")
                .Find(u => u.Email == dropboxUser.Email)
                .SingleOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    Username = dropboxUser.Name, 
                    Email = dropboxUser.Email
                };
                await _db.GetCollection<User>("users").InsertOneAsync(user);
            }

            var token = GenerateJwtToken(user.Username);
            return Ok(new { token });
        }


        private async Task<DropboxUser> GetDropboxUserInfo(string accessToken)
        {
            using var httpClient = new HttpClient();
            // Set the Authorization header with the access token.
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Dropbox API endpoint to get the current account's details.
            var apiUrl = "https://api.dropboxapi.com/2/users/get_current_account";

            // This endpoint expects a POST request with no content.
            var response = await httpClient.PostAsync(apiUrl, null);
            if (!response.IsSuccessStatusCode)
            {
                // You might want to log the error or throw an exception here.
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonSerializer.Deserialize<DropboxAccountResponse>(content);

            // Map the response to our simplified DropboxUser model.
            return new DropboxUser
            {
                Email = accountResponse.Email,
                Name = accountResponse.Name.DisplayName
            };
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"]);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }

    public class ProcessOAuthLoginBody
    {
        public string code { get; set; }
    }


    public class DropboxUser
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }


    public class DropboxAccountResponse
    {
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("name")]
        public DropboxAccountName Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class DropboxAccountName
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }
    }
}