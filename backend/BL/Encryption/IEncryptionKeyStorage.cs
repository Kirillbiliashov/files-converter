using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace backend.BL.Encryption
{
    public interface IEncryptionKeyStorage
    {
        Task<byte[]> GetKey(string keyName);

        Task SaveKey(string keyName, byte[] key);

        static byte[] GetRandomKey(int size)
        {
            byte[] newKey = new byte[size];
            RandomNumberGenerator.Fill(newKey);
            return newKey;
        }

    }
}