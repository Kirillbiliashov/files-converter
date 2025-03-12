using System;
using System.Collections.Generic;
using System.Linq;
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

        protected override string? GetAccessTokenResult(string responseBody)
        {
            var tokenResult = JsonSerializer.Deserialize<GoogleTokenResponse>(responseBody);
            return tokenResult?.IdToken;
        }

        public override async Task<OAuthUserInfo?> GetUserInfo(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(accessToken);
            if (payload == null)
            {
                return null;
            }

            return new OAuthUserInfo
            {
                Email = payload.Email,
                Username = $"{payload.GivenName} {payload.FamilyName}"
            };
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
