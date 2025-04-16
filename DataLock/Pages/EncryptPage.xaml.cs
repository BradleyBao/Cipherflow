using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DataLock.Modules;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EncryptPage : Page
    {
        public ObservableCollection<DataLock.Modules.File> FileList { get; } = new ObservableCollection<DataLock.Modules.File>();

        public ObservableCollection<DataLock.Modules.Folder> FolderList { get; } = new ObservableCollection<DataLock.Modules.Folder>();

        public ObservableCollection<DataLock.Modules.DataType> DataList { get; set; } = new ObservableCollection<DataLock.Modules.DataType>();

        public EncryptPage()
        {
            this.InitializeComponent();
            //LoadEncryptedFiles();
        }

        private void LoadEncryptedFiles()
        {
            // 模拟：你可以改成从某个加密文件夹扫描
            string mockPath = @"C:\EncryptedFiles";

            FileList.Add(new Modules.File("Secret-Report.enc", DateTime.Now.AddDays(-3), ".enc", 24000, mockPath));
            FileList.Add(new Modules.File("Finance-2023.enc", DateTime.Now.AddMonths(-1), ".enc", 132000, mockPath));
            FileList.Add(new Modules.File("PhotosBackup.enc", DateTime.Now.AddMonths(-2), ".enc", 5242880, mockPath));
        }

        private void UploadBanner_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var storageItems = e.DataView.GetStorageItemsAsync().GetAwaiter().GetResult();
                foreach (var item in storageItems)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        // Fix: Retrieve file size using GetBasicPropertiesAsync
                        var basicProperties = file.GetBasicPropertiesAsync().GetAwaiter().GetResult();
                        long fileSize = (long)basicProperties.Size;

                        string fileName = file.Name;
                        string filePath = file.Path;
                        string fileType = file.FileType;
                        DateTime createdDate = file.DateCreated.DateTime;
                        DateTime modifiedDate = DateTime.Now; // Assume upload time as modified date
                        FileList.Add(new Modules.File(fileName, createdDate, fileType, fileSize, filePath));
                    }
                    else if (item is Windows.Storage.StorageFolder folder)
                    {
                        // 处理文件夹
                        string folderName = folder.Name;
                        string folderPath = folder.Path;
                        DateTime createdDate = folder.DateCreated.DateTime;
                        FolderList.Add(new Modules.Folder(folderName, createdDate, folderPath));
                    }
                }
                UpdateList();
                UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
                UploadBanner.BorderThickness = new Thickness(2);
            }
        }

        internal void UpdateList()
        {
            DataList = new ObservableCollection<DataLock.Modules.DataType>(
                FileList.Cast<DataLock.Modules.DataType>().Concat(FolderList.Cast<DataLock.Modules.DataType>())
            );

            EncryptFileDataGrid.ItemsSource = DataList;
            NumofFilesRecord.Inlines.Clear(); // 清除现有的内容

            // Replace the problematic line with the following:
            NumofFilesRecord.Inlines.Add(new Run { Text = DataList.Count.ToString() }); // 添加新文本
        }

        private void UploadBanner_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
                UploadBanner.BorderThickness = new Thickness(2);
            }
        }

        private void UploadBanner_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void UploadBanner_DragLeave(object sender, DragEventArgs e)
        {
            UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
            UploadBanner.BorderThickness = new Thickness(0);
        }
    }
}
