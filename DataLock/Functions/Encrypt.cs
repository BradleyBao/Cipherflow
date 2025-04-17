using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace DataLock.Functions
{
    public class Encrypt
    {
        public static byte[] AES_GCM_Encrypt(string inputFilePath, string outputFilePath)
        {
            // Create Random Key
            byte[] key = RandomNumberGenerator.GetBytes(32); // 256 bits
            byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96 bits
            byte[] tag = new byte[16]; // 128 bits GCM Tag

            // Read File
            byte[] plaintext = File.ReadAllBytes(inputFilePath); 
            byte[] ciphertext = new byte[plaintext.Length];


            using (var aes = new AesGcm(key))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                // Write Key, Nonce, Tag and Ciphertext to file
                fs.Write(nonce, 0, nonce.Length);
                fs.Write(tag, 0, tag.Length);
                fs.Write(ciphertext, 0, ciphertext.Length);
            }

            return key;
        }
    }
}
