using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace backend.BL.Encryption
{
    public interface IEncryptor
    {
        byte[] EncryptData(byte[] dataBytes, byte[] key);

        byte[] DecryptData(byte[] dataBytes, byte[] key);
    }
}