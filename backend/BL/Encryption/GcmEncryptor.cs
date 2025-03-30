using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace backend.BL.Encryption
{
    public class GcmEncryptor : IEncryptor
    {
        public byte[] DecryptData(byte[] dataBytes, byte[] key)
        {
            int ivLength = AesGcm.NonceByteSizes.MaxSize;  
            int tagLength = AesGcm.TagByteSizes.MaxSize;    

            byte[] iv = new byte[ivLength];
            Buffer.BlockCopy(dataBytes, 0, iv, 0, ivLength);

            byte[] tag = new byte[tagLength];
            Buffer.BlockCopy(dataBytes, ivLength, tag, 0, tagLength);

            int ciphertextLength = dataBytes.Length - ivLength - tagLength;
            byte[] ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(dataBytes, ivLength + tagLength, ciphertext, 0, ciphertextLength);

            byte[] plaintext = new byte[ciphertextLength];

            using (AesGcm aesGcm = new AesGcm(key))
            {
                aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
            }

            return plaintext;
        }

        public byte[] EncryptData(byte[] dataBytes, byte[] key)
        {
            using (AesGcm aesGcm = new AesGcm(key))
            {
                byte[] iv = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(iv);

                byte[] ciphertext = new byte[dataBytes.Length];
                byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

                aesGcm.Encrypt(iv, dataBytes, ciphertext, tag);

                byte[] encryptedData = new byte[iv.Length + tag.Length + ciphertext.Length];
                Buffer.BlockCopy(iv, 0, encryptedData, 0, iv.Length);
                Buffer.BlockCopy(tag, 0, encryptedData, iv.Length, tag.Length);
                Buffer.BlockCopy(ciphertext, 0, encryptedData, iv.Length + tag.Length, ciphertext.Length);

                return encryptedData;
            }
        }
    }
}