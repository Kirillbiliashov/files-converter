using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace backend.BL.Integrations
{
    public class DropboxSignInManager : OAuthSignInManager
    {
        public DropboxSignInManager(IConfiguration config) : base(config)
        {
        }

        public override async Task<OAuthUserInfo?> GetUserInfo(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var apiUrl = "https://api.dropboxapi.com/2/users/get_current_account";

            var response = await httpClient.PostAsync(apiUrl, null);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonSerializer.Deserialize<DropboxAccountResponse>(content);

            return new OAuthUserInfo
            {
                Email = accountResponse.Email,
                Username = accountResponse.Name.DisplayName
            };
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

