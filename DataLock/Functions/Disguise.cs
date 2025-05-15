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
        public static async Task<bool> DisguiseToImagePublic(string input_file_path, string image_path, string output_path, string output_folder_path, string data_extension)
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
