using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DataLock.Functions
{
    public class Decrypt
    {
        public static bool AES_GCM_Decrypt(string encryptedFilePath, string outputFilePath, byte[] key)
        {
            // Read the encrypted file
            byte[] fileBytes = File.ReadAllBytes(encryptedFilePath);

            // Number use once 
            byte[] nonce = new byte[12];

            // GCM Tag: Authentication Tag
            byte[] tag = new byte[16];

            // Ciphertext
            byte[] ciphertext = new byte[fileBytes.Length - nonce.Length - tag.Length];

            Array.Copy(fileBytes, 0, nonce, 0, nonce.Length);
            Array.Copy(fileBytes, nonce.Length, tag, 0, tag.Length);
            Array.Copy(fileBytes, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

            byte[] plaintext = new byte[ciphertext.Length];

            try
            {
                using (var aes = new AesGcm(key))
                {
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);
                }
            }
            catch (CryptographicException)
            {
                // Handle decryption failure (e.g., invalid key or corrupted data)
                return false;
            }

            File.WriteAllBytes(outputFilePath, plaintext);
            return true;
        }
    }
}
