using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Google.Apis.Auth;

namespace backend.BL.Integrations
{
    public class GoogleSignInManager : OAuthSignInManager
    {
        public GoogleSignInManager(IConfiguration config) : base(config)
        {
        }

        public override async Task<OAuthUserInfo?> GetUserInfo(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var payload = await GetUserInfoAsync(accessToken);
            if (payload == null)
            {
                return null;
            }

            return new OAuthUserInfo
            {
                Email = payload.Email,
                Username = payload.Name
            };
        }

        private async Task<GoogleUserInfo?> GetUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<GoogleUserInfo>(jsonResponse);

            }
            return null;
        }

    }

    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
    }
}
