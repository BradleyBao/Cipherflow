﻿using System;
using System.Collections.ObjectModel;

namespace DataLock.Modules
{
    public class Folder : DataType
    {
        public string folder_name { get; set; }
        private DateTime date_modified; 
        private string folder_path;
        //private List<LocalSync.Modules.File> File_List; 
        Folder father_directory;
        public ObservableCollection<Folder> Children { get; set; } = new ObservableCollection<Folder>();
        public string Name => folder_name;
        public string dataType => "folder";
        public string dataFileIcon => "\uE8B7";
        public string Path => folder_path;
        public DateTime DataModifiled => date_modified;
        public bool Encrypted { get; set; } = false; // Default value is false
        public Folder(string folder_name, DateTime date_modified, string folder_path, Folder father_directory = null) {
            this.folder_name = folder_name; 
            this.date_modified = date_modified;
            this.folder_path = folder_path;
            this.father_directory = father_directory; 
        }

        public string GetFolderPath()
        {
            return folder_path;
        }
    }
}
