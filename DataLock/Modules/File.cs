using System;

namespace DataLock.Modules
{
    public class File
    {
        // Properties
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public long FileSize { get; private set; } // File size in bytes
        public DateTime CreatedDate { get; private set; }
        public DateTime ModifiedDate { get; private set; }

        // Constructor
        public File(string fileName, string filePath, long fileSize, DateTime createdDate, DateTime modifiedDate)
        {
            FileName = fileName;
            FilePath = filePath;
            FileSize = fileSize;
            CreatedDate = createdDate;
            ModifiedDate = modifiedDate;
        }

        // Methods
        public string GetFullPath()
        {
            return System.IO.Path.Combine(FilePath, FileName);
        }

        public void UpdateFilePath(string newFilePath)
        {
            FilePath = newFilePath;
        }

        public void UpdateModifiedDate()
        {
            ModifiedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"File: {FileName}, Path: {FilePath}, Size: {FileSize} bytes, Created: {CreatedDate}, Modified: {ModifiedDate}";
        }
    }
}