using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace backend.BL.Encryption
{
    public class EncryptionKeyAzureStorage : IEncryptionKeyStorage
    {
        private readonly SecretClient _secretClient;

        public EncryptionKeyAzureStorage(IConfiguration config)
        {
            var azureSection = config.GetSection("AzureSettings");
            var credentials = new ClientSecretCredential(azureSection["TenantId"], azureSection["AppId"], azureSection["AppSecret"]);
            _secretClient = new SecretClient(new Uri(azureSection["KeyVaultUrl"]), credentials);
        }

        public async Task<byte[]> GetKey(string keyName)
        {
            try
            {
                var result = await _secretClient.GetSecretAsync(keyName);
                var secret = result.Value.Value;
                return Convert.FromBase64String(secret);
            }
            catch (Azure.RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    var secret = IEncryptionKeyStorage.GetRandomKey(32);
                    await SaveKey(keyName, secret);
                    return secret;
                }
                return null;
            }
        }

        public async Task SaveKey(string keyName, byte[] key)
        {
            await _secretClient.SetSecretAsync(new KeyVaultSecret(keyName, Convert.ToBase64String(key)));
        }

    }
}