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
        public static async Task<bool> AES_GCM_Encrypt(string inputFilePath, string outputFilePath, string password)
        {
            // Create Random Key
            byte[] salt = RandomNumberGenerator.GetBytes(16); // 256 bits
            byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96 bits
            byte[] tag = new byte[16]; // 128 bits GCM Tag

            // Read File
            byte[] key = DeriveKeyFromPassword(password, salt);
            byte[] plaintext = await File.ReadAllBytesAsync(inputFilePath); 
            byte[] ciphertext = new byte[plaintext.Length];

            try
            {
                using (var aes = new AesGcm(key))
                {
                    aes.Encrypt(nonce, plaintext, ciphertext, tag);
                }

                using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    // Write Key, Nonce, Tag and Ciphertext to file
                    fs.Write(salt, 0, salt.Length);
                    fs.Write(nonce, 0, nonce.Length);
                    fs.Write(tag, 0, tag.Length);
                    fs.Write(ciphertext, 0, ciphertext.Length);
                }
            }
            catch (CryptographicException)
            {
                // Handle encryption failure (e.g., invalid key or corrupted data)
                return false;
            }
            catch (IOException ex)
            {
                // Handle file I/O errors
                Console.WriteLine($"File I/O error: {ex.Message}");
                return false;
            }

            return true;
        }

        public static byte[] AES_GCM_Encrypt(string inputFilePath, string outputFilePath)
        {
            // Create Random Key
            byte[] key = RandomNumberGenerator.GetBytes(32);
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

        public static async Task<bool> ChaCha20_Poly1305_Encrypt(string inputFilePath, string outputFilePath, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16); // Create 16 bits Salt
            byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96 bits
            byte[] tag = new byte[16]; // 128 bits Tag
            byte[] plaintext = await File.ReadAllBytesAsync(inputFilePath);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] key = DeriveKeyFromPassword(password, salt);

            try
            {
                using (var chacha = new ChaCha20Poly1305(key))
                {
                    chacha.Encrypt(nonce, plaintext, ciphertext, tag);
                }

                using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    // Write Key, Nonce, Tag and Ciphertext to file
                    fs.Write(salt, 0, salt.Length);
                    fs.Write(nonce, 0, nonce.Length);
                    fs.Write(tag, 0, tag.Length);
                    fs.Write(ciphertext, 0, ciphertext.Length);
                }
            }
            catch (CryptographicException)
            {
                // Handle encryption failure (e.g., invalid key or corrupted data)
                return false;
            }
            catch (IOException ex)
            {
                // Handle file I/O errors
                Console.WriteLine($"File I/O error: {ex.Message}");
                return false;
            }
            return true;
        }

        private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }
    }
}
