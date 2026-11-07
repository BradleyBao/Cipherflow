using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace DataLock.Functions
{
    public class Decrypt
    {
        public static Task<bool> AES_GCM_Decrypt(string encryptedFilePath, string outputFilePath, byte[] key, CancellationToken cancellationToken = default)
        {
            return OperationFiles.WriteAsync(encryptedFilePath, outputFilePath,
                (staged, token) => AES_GCM_DecryptCore(encryptedFilePath, staged, key, token), cancellationToken);
        }

        private static async Task<bool> AES_GCM_DecryptCore(string encryptedFilePath, string outputFilePath, byte[] key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Read the encrypted file
            byte[] fileBytes = await File.ReadAllBytesAsync(encryptedFilePath, cancellationToken);

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

            await File.WriteAllBytesAsync(outputFilePath, plaintext, cancellationToken);
            return true;
        }

        public static Task<bool> AES_GCM_Decrypt(string encryptedFilePath, string outputFilePath, string password, CancellationToken cancellationToken = default)
        {
            return OperationFiles.WriteAsync(encryptedFilePath, outputFilePath,
                (staged, token) => AES_GCM_DecryptCore(encryptedFilePath, staged, password, token), cancellationToken);
        }

        private static async Task<bool> AES_GCM_DecryptCore(string encryptedFilePath, string outputFilePath, string password, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] fileBytes = await File.ReadAllBytesAsync(encryptedFilePath, cancellationToken);

            byte[] salt = new byte[16];
            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[fileBytes.Length - 16 - 12 - 16];

            Array.Copy(fileBytes, 0, salt, 0, 16);
            Array.Copy(fileBytes, 16, nonce, 0, 12);
            Array.Copy(fileBytes, 28, tag, 0, 16);
            Array.Copy(fileBytes, 44, ciphertext, 0, ciphertext.Length);

            byte[] key = DeriveKeyFromPassword(password, salt);
            byte[] plaintext = new byte[ciphertext.Length];

            try
            {
                using (var aes = new AesGcm(key))
                {
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);
                }

                await File.WriteAllBytesAsync(outputFilePath, plaintext, cancellationToken);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static Task<bool> AES_GCM_Decrypt_Stream(string inputFilePath, string outputFilePath, string password, CancellationToken cancellationToken = default)
        {
            return OperationFiles.WriteAsync(inputFilePath, outputFilePath,
                (staged, token) => AES_GCM_Decrypt_StreamCore(inputFilePath, staged, password, token), cancellationToken);
        }

        private static async Task<bool> AES_GCM_Decrypt_StreamCore(string inputFilePath, string outputFilePath, string password, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using FileStream inputFs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

                // Salt
                byte[] salt = new byte[16];
                int readSalt = await inputFs.ReadAsync(salt, 0, salt.Length, cancellationToken);
                if (readSalt != 16) return false;

                byte[] key = DeriveKeyFromPassword(password, salt);

                while (inputFs.Position < inputFs.Length)
                {
                    // 读取 4 字节长度（little endian）
                    byte[] lengthBytes = new byte[4];
                    if (await inputFs.ReadAsync(lengthBytes, 0, 4, cancellationToken) != 4)
                        return false;
                    int cipherLength = BitConverter.ToInt32(lengthBytes, 0);

                    // Read nonce
                    byte[] nonce = new byte[12];
                    if (await inputFs.ReadAsync(nonce, 0, nonce.Length, cancellationToken) != nonce.Length)
                        return false;

                    // Read tag
                    byte[] tag = new byte[16];
                    if (await inputFs.ReadAsync(tag, 0, tag.Length, cancellationToken) != tag.Length)
                        return false;

                    // Read ciphertext
                    byte[] ciphertext = new byte[cipherLength];
                    if (await inputFs.ReadAsync(ciphertext, 0, ciphertext.Length, cancellationToken) != ciphertext.Length)
                        return false;

                    // Decrypt
                    byte[] plaintext = new byte[ciphertext.Length];
                    try
                    {
                        using var aes = new AesGcm(key);
                        aes.Decrypt(nonce, ciphertext, tag, plaintext);
                    }
                    catch (CryptographicException)
                    {
                        Console.WriteLine("Decryption failed: tag mismatch or invalid password.");
                        return false;
                    }

                    await outputFs.WriteAsync(plaintext, 0, plaintext.Length, cancellationToken);
                }

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                return false;
            }
        }


        public static Task<bool> ChaCha20_Poly1305_Decrypt(string encryptedFilePath, string outputFilePath, string password, CancellationToken cancellationToken = default)
        {
            return OperationFiles.WriteAsync(encryptedFilePath, outputFilePath,
                (staged, token) => ChaCha20_Poly1305_DecryptCore(encryptedFilePath, staged, password, token), cancellationToken);
        }

        private static async Task<bool> ChaCha20_Poly1305_DecryptCore(string encryptedFilePath, string outputFilePath, string password, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] fileBytes = await File.ReadAllBytesAsync(encryptedFilePath, cancellationToken);
            byte[] salt = new byte[16];
            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            // Create Ciphertext Bits minus metadata length
            byte[] ciphertext = new byte[fileBytes.Length - 16 - 12 - 16];

            // Divide the data and save it to the corresponding array
            Array.Copy(fileBytes, 0, salt, 0, 16);
            Array.Copy(fileBytes, 16, nonce, 0, 12);
            Array.Copy(fileBytes, 28, tag, 0, 16);
            Array.Copy(fileBytes, 44, ciphertext, 0, ciphertext.Length);

            // Get key from password
            byte[] key = DeriveKeyFromPassword(password, salt);
            byte[] plaintext = new byte[ciphertext.Length];

            try
            {
                using (var chacha = new ChaCha20Poly1305(key))
                {
                    chacha.Decrypt(nonce, ciphertext, tag, plaintext);
                }
                await File.WriteAllBytesAsync(outputFilePath, plaintext, cancellationToken);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static Task<bool> ChaCha20_Poly1305_Decrypt_Stream(string inputFilePath, string outputFilePath, string password, CancellationToken cancellationToken = default)
        {
            return OperationFiles.WriteAsync(inputFilePath, outputFilePath,
                (staged, token) => ChaCha20_Poly1305_Decrypt_StreamCore(inputFilePath, staged, password, token), cancellationToken);
        }

        private static async Task<bool> ChaCha20_Poly1305_Decrypt_StreamCore(string inputFilePath, string outputFilePath, string password, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using FileStream inputFs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

                // 读取 Salt
                byte[] salt = new byte[16];
                if (await inputFs.ReadAsync(salt, 0, salt.Length, cancellationToken) != salt.Length)
                    return false;

                byte[] key = DeriveKeyFromPassword(password, salt);

                while (inputFs.Position < inputFs.Length)
                {
                    // 读取密文长度
                    byte[] lengthBytes = new byte[4];
                    if (await inputFs.ReadAsync(lengthBytes, 0, 4, cancellationToken) != 4)
                        return false;

                    int cipherLength = BitConverter.ToInt32(lengthBytes, 0);

                    // 读取 Nonce
                    byte[] nonce = new byte[12];
                    if (await inputFs.ReadAsync(nonce, 0, nonce.Length, cancellationToken) != nonce.Length)
                        return false;

                    // 读取 Tag
                    byte[] tag = new byte[16];
                    if (await inputFs.ReadAsync(tag, 0, tag.Length, cancellationToken) != tag.Length)
                        return false;

                    // 读取 Ciphertext
                    byte[] ciphertext = new byte[cipherLength];
                    if (await inputFs.ReadAsync(ciphertext, 0, ciphertext.Length, cancellationToken) != ciphertext.Length)
                        return false;

                    byte[] plaintext = new byte[ciphertext.Length];
                    try
                    {
                        using var chacha = new ChaCha20Poly1305(key);
                        chacha.Decrypt(nonce, ciphertext, tag, plaintext);
                    }
                    catch (CryptographicException)
                    {
                        Console.WriteLine("Decryption failed: tag mismatch or invalid password.");
                        return false;
                    }

                    await outputFs.WriteAsync(plaintext, 0, plaintext.Length, cancellationToken);
                }

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                return false;
            }
        }


        public static async Task DecryptFilesInParallelAsync(IEnumerable<(string EncryptedFilePath, string OutputFilePath, string Password)> files)
        {
            var tasks = files.Select(file =>
                AES_GCM_Decrypt(file.EncryptedFilePath, file.OutputFilePath, file.Password));

            bool[] results = await Task.WhenAll(tasks);

            // Log results
            for (int i = 0; i < files.Count(); i++)
            {
                var file = files.ElementAt(i);
                Console.WriteLine($"File: {file.EncryptedFilePath} -> {(results[i] ? "Decrypted Successfully" : "Decryption Failed")}");
            }
        }

        private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }
    }
}
