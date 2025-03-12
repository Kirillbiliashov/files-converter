using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace backend.BL.Integrations
{
    public class DropboxSignInManager
    {
        private readonly IConfiguration _config;
        private const string authUrl = "https://www.dropbox.com/oauth2/authorize";
        private const string tokenUrl = "https://api.dropboxapi.com/oauth2/token";

        public DropboxSignInManager(IConfiguration config) => _config = config;

        public string GetLoginUrl()
        {
            var returnUrl = new Uri(_config["Authentication:Dropbox:RedirectUri"]);
            // Build the Dropbox login URL.
            var uriBuilder = authUrl;
            uriBuilder += "?client_id=" + _config["Authentication:Dropbox:ClientId"];
            uriBuilder += "&redirect_uri=" + returnUrl.GetLeftPart(UriPartial.Path);
            uriBuilder += "&response_type=code";
            uriBuilder += "&state=state"; // In production, generate and validate a unique state value.
            // Optionally, add token_access_type=offline to get a refresh token.
            // uriBuilder += "&token_access_type=offline";
            return uriBuilder;
        }

        public async Task<string?> GetAccessToken(string authCode)
        {
            var payload = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", _config["Authentication:Dropbox:ClientId"] },
                { "client_secret", _config["Authentication:Dropbox:ClientSecret"] },
                { "redirect_uri", _config["Authentication:Dropbox:RedirectUri"] },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(payload));
            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenResult = JsonSerializer.Deserialize<DropboxTokenResponse>(tokenResponseBody);
                return tokenResult?.AccessToken;
            }

            return null;
        }
    }

    public class DropboxTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }
    }
}