using DataLock.Functions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLock.Modules
{
    public class DynamicLock
    {
        public string Title { get; set; }
        public int Status { get; set; }
        // 0: not encrypted, 1: encrypted, 2: encrypting, 3: error
        public string Path { get; set; }
        public string status_icon;
        public string FileType;
        public string FileBanner;

        public DynamicLock(string title, int status, string path) {
            {
                this.Title = title;
                this.Status = status;
                this.Path = path;
                this.status_icon = Match_status_icon(status);
                this.FileType = DetermineFileType(path);
                this.FileBanner = "/Assets/" + this.FileType + "_Banner.png";
            } }

        internal static string Match_status_icon(int status)
        {
            if (status == 0) return "\uEB59";
            else if (status == 1) return "\uF809";
            else if (status == 2) return "\uF143";
            else return "\uEA83";
        }

        internal static string DetermineFileType(string path)
        {
            if (File.Exists(path))
            {
                return "File";

            }
            else if (Directory.Exists(path))
            {
                return "Folder";
            }
            else
            {
                return "NotExist";
            }
        }
    }
}
