using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace backend.BL.Integrations
{
    public class GoogleSignInManager
    {
        private IConfiguration _config;

        private const string authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string tokenUrl = "https://oauth2.googleapis.com/token";

        public GoogleSignInManager(IConfiguration config) => _config = config;

        public string GetLoginUrl()
        {
            var returnUrl = new Uri(_config["Authentication:Google:RedirectUri"]);
            var defaultScopes = new[] { "https://www.googleapis.com/auth/userinfo.profile", "https://www.googleapis.com/auth/userinfo.email" };
            var uriBuilder = authUrl;
            uriBuilder += "?client_id=" + _config["Authentication:Google:ClientId"];
            uriBuilder += "&redirect_uri=" + returnUrl.GetLeftPart(UriPartial.Path);
            uriBuilder += "&response_type=code";
            uriBuilder += "&access_type=offline";
            uriBuilder += "&include_granted_scopes=true";
            uriBuilder += "&scope=" + string.Join(" ", defaultScopes);
            uriBuilder += "&state=state";

            return uriBuilder;
        }

        public async Task<string?> GetAccessCode(string authCode)
        {
            var payload = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", _config["Authentication:Google:ClientId"] },
                { "client_secret", _config["Authentication:Google:ClientSecret"] },
                { "redirect_uri", _config["Authentication:Google:RedirectUri"] },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(payload));
            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            if (tokenResponse.IsSuccessStatusCode)
            {
                var tokenResult = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenResponseBody);
                return tokenResult?.IdToken;
            }

            return null;
        }
    }

    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }
}