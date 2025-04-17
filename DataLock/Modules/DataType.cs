using System.Collections.ObjectModel;
using System;

namespace DataLock.Modules
{
    public interface DataType
    {
        public string Name { get; }
        public string Path { get; }
        public string dataType { get; }
        public string dataFileIcon { get; }
        public DateTime DataModifiled { get; }
        public bool Encrypted { get; set; }
    }
}
