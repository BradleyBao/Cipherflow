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

        public static async Task<bool> AES_GCM_Encrypt_Stream(string inputFilePath, string outputFilePath, string password)
        {
            const int chunkSize = 1024 * 1024; // 1MB per chunk

            byte[] salt = RandomNumberGenerator.GetBytes(16); // for key derivation
            byte[] key = DeriveKeyFromPassword(password, salt);

            try
            {
                using FileStream inputFs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

                // 写入salt
                await outputFs.WriteAsync(salt, 0, salt.Length);

                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = await inputFs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] nonce = RandomNumberGenerator.GetBytes(12);
                    byte[] tag = new byte[16];
                    byte[] plaintext = buffer.Take(bytesRead).ToArray();
                    byte[] ciphertext = new byte[plaintext.Length];

                    using (var aes = new AesGcm(key))
                    {
                        aes.Encrypt(nonce, plaintext, ciphertext, tag);
                    }

                    // 写入每个块的信息：[4字节块长度][nonce][tag][ciphertext]
                    byte[] lengthBytes = BitConverter.GetBytes(ciphertext.Length);
                    await outputFs.WriteAsync(lengthBytes, 0, 4);
                    await outputFs.WriteAsync(nonce, 0, nonce.Length);
                    await outputFs.WriteAsync(tag, 0, tag.Length);
                    await outputFs.WriteAsync(ciphertext, 0, ciphertext.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex.Message}");
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

        public static async Task<bool> ChaCha20_Poly1305_Encrypt_Stream(string inputFilePath, string outputFilePath, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16); // 盐值
            byte[] key = DeriveKeyFromPassword(password, salt);

            const int chunkSize = 1024 * 1024 * 4; // 4MB 每块

            try
            {
                using FileStream inputFs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

                // 写入 Salt
                await outputFs.WriteAsync(salt, 0, salt.Length);

                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = await inputFs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96位
                    byte[] tag = new byte[16]; // 128位认证标签
                    byte[] plaintext = buffer[..bytesRead];
                    byte[] ciphertext = new byte[bytesRead];

                    using var chacha = new ChaCha20Poly1305(key);
                    chacha.Encrypt(nonce, plaintext, ciphertext, tag);

                    // 写入：长度（4字节）+ Nonce + Tag + CipherText
                    await outputFs.WriteAsync(BitConverter.GetBytes(ciphertext.Length));
                    await outputFs.WriteAsync(nonce);
                    await outputFs.WriteAsync(tag);
                    await outputFs.WriteAsync(ciphertext);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                return false;
            }
        }


        private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }
    }
}
