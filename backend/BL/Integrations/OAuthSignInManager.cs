using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using backend.Models.DTO;

namespace backend.BL.Integrations
{
    public abstract class OAuthSignInManager
    {

        private IConfiguration _config;

        public OAuthSignInManager(IConfiguration config) => _config = config;

        public string GetLoginUrl(string provider)
        {
            var configPath = $"Authentication:{provider}";
            var configSection = _config.GetSection(configPath);
            if (configSection == null)
            {
                return null;
            }

            var returnUrl = new Uri(configSection["RedirectUri"]);
            var uriBuilder = configSection["AuthUrl"];
            uriBuilder += "?client_id=" + configSection["ClientId"];
            uriBuilder += "&redirect_uri=" + returnUrl.GetLeftPart(UriPartial.Path);
            uriBuilder += "&response_type=code";
            uriBuilder += $"&scope={configSection["Scope"]}";
            uriBuilder += $"&state={provider}";
            uriBuilder += "&access_type=offline";
            uriBuilder += "&prompt=consent";

            return uriBuilder;
        }


        public async Task<AccessTokenResponse?> GetAccessTokenResponse(string provider, string authCode)
        {
            var configPath = $"Authentication:{provider}";
            var configSection = _config.GetSection(configPath);
            if (configSection == null)
            {
                return null;
            }

            var tokenUrl = configSection["TokenUrl"];
            var payload = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", configSection["ClientId"] },
                { "client_secret", configSection["ClientSecret"] },
                { "redirect_uri", configSection["RedirectUri"] },
                { "grant_type", "authorization_code" }
            };

            return await CallAccessTokenAPI(payload, tokenUrl);
        }

        public async Task<AccessTokenResponse?> GetRefreshTokenResponse(string provider, string refreshToken)
        {
            var configPath = $"Authentication:{provider}";
            var configSection = _config.GetSection(configPath);
            if (configSection == null)
            {
                return null;
            }

            var tokenUrl = configSection["TokenUrl"];
            var payload = new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "client_id", configSection["ClientId"] },
                { "client_secret", configSection["ClientSecret"] },
                { "redirect_uri", configSection["RedirectUri"] },
                { "grant_type", "refresh_token" }
            };

            return await CallAccessTokenAPI(payload, tokenUrl);
        }


        private async Task<AccessTokenResponse?> CallAccessTokenAPI(Dictionary<string, string> payload, string url)
        {
            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync(url, new FormUrlEncodedContent(payload));
            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            if (tokenResponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<AccessTokenResponse>(tokenResponseBody);
            }

            return null;
        }

        public abstract Task<OAuthUserInfo?> GetUserInfo(string? accessToken);


    }

    public class OAuthUserInfo
    {
        public string Email { get; set; }

        public string Username { get; set; }
    }

}