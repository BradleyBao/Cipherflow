using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;

namespace DataLock.Functions
{
    public class FileIconHelper
    {
        // 定义 SHFILEINFO 结构体，用于接收 SHGetFileInfo 的信息
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;              // 文件图标句柄
            public int iIcon;                 // 图标索引
            public uint dwAttributes;         // 文件属性
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;      // 显示名称
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;         // 类型名称
        };

        // 调用 Shell32.dll 中的 SHGetFileInfo 函数
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        // 常量定义
        public const uint SHGFI_ICON = 0x000000100;              // 获取图标
        public const uint SHGFI_LARGEICON = 0x000000000;           // 获取大图标
        public const uint SHGFI_SMALLICON = 0x000000001;           // 获取小图标
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;   // 使用文件属性而非实际文件
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;      // 文件的正常属性

        // 用于释放图标资源
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// 根据文件路径获取图标。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="smallIcon">是否返回小图标，false 为大图标</param>
        /// <returns>返回 Icon 对象，如果获取失败则返回 null</returns>
        public static Icon GetFileIcon(string filePath, bool smallIcon)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            // 如果文件存在，则直接获取，不使用 SHGFI_USEFILEATTRIBUTES
            // 如果文件不存在，则使用 SHGFI_USEFILEATTRIBUTES 获取通用图标
            uint flags = SHGFI_ICON;
            if (!File.Exists(filePath))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
            }
            flags |= smallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON;

            // 当文件存在时，dwFileAttributes 传 0；否则传入 FILE_ATTRIBUTE_NORMAL
            uint fileAttributes = File.Exists(filePath) ? 0 : FILE_ATTRIBUTE_NORMAL;

            IntPtr result = SHGetFileInfo(filePath, fileAttributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (result == IntPtr.Zero)
            {
                return null;
            }

            // 根据获取到的句柄创建 Icon 对象
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            // 销毁原始图标句柄以避免资源泄漏
            DestroyIcon(shinfo.hIcon);
            return icon;
        }
    }
}
