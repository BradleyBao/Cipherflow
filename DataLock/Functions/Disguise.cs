using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLock.Functions
{
    public class Disguise
    {
        public static async Task<bool> DisguiseToImageAny(string input_file_path, string image_path, string output_path, string output_folder_path, string data_extension)
        {
            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(image_path);     // 加载封面图像
                byte[] fileBytes = await File.ReadAllBytesAsync(input_file_path);  // 加载原始文件

                int imageEndIndex = GetImageEndMarkerIndex(imageBytes);
                if (imageEndIndex < 0)
                {
                    Console.WriteLine("[Error] Unsupported or invalid image format.");
                    return false;
                }

                using (var fs = new FileStream(output_path, FileMode.Create, FileAccess.Write))
                {
                    // 写入图片本体（保留到图像结束标志）
                    await fs.WriteAsync(imageBytes, 0, imageEndIndex + 1);
                    // 写入附加数据
                    await fs.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to disguise: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ExtractFromDisguisedImageAny(string disguised_path, string output_path, string image_path)
        {
            try
            {
                byte[] fullBytes = await File.ReadAllBytesAsync(disguised_path);
                int imageEndIndex = GetImageEndMarkerIndex(fullBytes);
                if (imageEndIndex < 0 || imageEndIndex >= fullBytes.Length - 1)
                {
                    Console.WriteLine("[Error] Not a valid disguised file.");
                    return false;
                }

                byte[] imageBytes = fullBytes.Take(imageEndIndex + 1).ToArray();
                byte[] dataBytes = fullBytes.Skip(imageEndIndex + 1).ToArray();

                await File.WriteAllBytesAsync(output_path, dataBytes);
                await File.WriteAllBytesAsync(image_path, imageBytes);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to extract: {ex.Message}");
                return false;
            }
        }

        public static bool IsSupportedCompressedFile(string filePath)
        {
            string[] supportedExtensions = { ".zip", ".rar", ".7z", ".tar", ".gz" };
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            return supportedExtensions.Contains(fileExtension);
        }

        public static async Task<bool> DisguiseToImageCompress(string input_compressed_path, string image_path, string output_path)
        {
            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(image_path);
                byte[] compressedBytes = await File.ReadAllBytesAsync(input_compressed_path);

                string ext = Path.GetExtension(input_compressed_path).ToLower();
                bool isValid = IsValidCompressFile(compressedBytes);

                string format = "";
                if (ext == ".zip") format = "zip";
                else if (ext == ".rar") format = "rar";
                else
                {
                    Console.WriteLine("[Error] Unsupported archive type. Only .zip and .rar are supported.");
                    return false;
                }

                if (!isValid)
                {
                    Console.WriteLine("[Info] Archive is not in compatible format. Repacking...");

                    // Repack as valid ZIP file (RAR repacking is not supported in .NET, third-party lib required)
                    if (format == "zip")
                    {
                        string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempFolder);

                        // Extract contents
                        System.IO.Compression.ZipFile.ExtractToDirectory(input_compressed_path, tempFolder);

                        // Repack
                        string repackedZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                        System.IO.Compression.ZipFile.CreateFromDirectory(tempFolder, repackedZipPath);

                        compressedBytes = await File.ReadAllBytesAsync(repackedZipPath);

                        Directory.Delete(tempFolder, true);
                        File.Delete(repackedZipPath);
                    }
                    else
                    {
                        Console.WriteLine("[Error] Cannot repackage RAR archives. Please provide a valid RAR file.");
                        return false;
                    }
                }

                int imageEndIndex = GetImageEndMarkerIndex(imageBytes);
                if (imageEndIndex < 0)
                {
                    Console.WriteLine("[Error] Invalid image format.");
                    return false;
                }

                using (var fs = new FileStream(output_path, FileMode.Create, FileAccess.Write))
                {
                    await fs.WriteAsync(imageBytes, 0, imageEndIndex + 1);
                    await fs.WriteAsync(compressedBytes, 0, compressedBytes.Length);
                }

                Console.WriteLine("[Success] Disguised file created. Rename to .zip or .rar to open.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to disguise compress file: {ex.Message}");
                return false;
            }
        }


        // Check if it is a standard compressed file
        private static bool IsValidCompressFile(byte[] bytes)
        {
            if (bytes.Length < 8) return false;

            // ZIP 文件头：PK\x03\x04
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
            {
                // 检查是否包含中央目录结束标记 PK\x05\x06
                for (int i = bytes.Length - 22; i >= 0; i--)
                {
                    if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B &&
                        bytes[i + 2] == 0x05 && bytes[i + 3] == 0x06)
                    {
                        return true;
                    }
                }
                return false; // 无中央目录，可能损坏或不兼容
            }

            // RAR4: 52 61 72 21 1A 07 00
            if (bytes[0] == 0x52 && bytes[1] == 0x61 && bytes[2] == 0x72 && bytes[3] == 0x21 &&
                bytes[4] == 0x1A && bytes[5] == 0x07 && bytes[6] == 0x00)
                return true;

            // RAR5: 52 61 72 21 1A 07 01 00
            if (bytes.Length >= 8 && bytes[0] == 0x52 && bytes[1] == 0x61 && bytes[2] == 0x72 && bytes[3] == 0x21 &&
                bytes[4] == 0x1A && bytes[5] == 0x07 && bytes[6] == 0x01 && bytes[7] == 0x00)
                return true;

            return false;
        }



        private static int GetImageEndMarkerIndex(byte[] imageBytes)
        {
            // JPEG: 0xFFD9
            for (int i = 0; i < imageBytes.Length - 1; i++)
            {
                if (imageBytes[i] == 0xFF && imageBytes[i + 1] == 0xD9)
                {
                    return i + 1;
                }
            }

            // PNG: Ends with 49 45 4E 44 AE 42 60 82
            byte[] pngEnd = new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
            for (int i = 0; i < imageBytes.Length - pngEnd.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pngEnd.Length; j++)
                {
                    if (imageBytes[i + j] != pngEnd[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i + pngEnd.Length - 1;
            }

            // GIF: Ends with 0x3B
            for (int i = imageBytes.Length - 1; i >= 0; i--)
            {
                if (imageBytes[i] == 0x3B)
                {
                    return i;
                }
            }

            return -1; // Unsupported format
        }


    }
}
