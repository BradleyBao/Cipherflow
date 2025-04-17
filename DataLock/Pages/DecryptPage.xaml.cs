using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DataLock.Modules;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Documents;
using DataLock.Functions;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DecryptPage : Page
    {

        public ObservableCollection<DataLock.Modules.File> FileList { get; } = new ObservableCollection<DataLock.Modules.File>();

        public ObservableCollection<DataLock.Modules.Folder> FolderList { get; } = new ObservableCollection<DataLock.Modules.Folder>();

        public ObservableCollection<DataLock.Modules.DataType> DataList { get; set; } = new ObservableCollection<DataLock.Modules.DataType>();

        public DecryptPage()
        {
            this.InitializeComponent();
        }

        private async System.Threading.Tasks.Task<ContentDialogResult> ShowDialog(string title,
            string btn1,
            string btn2 = "",
            string closebtn = "Cancel",
            ContentDialogButton DefaultButton = ContentDialogButton.Primary,
            string content = "")
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.PrimaryButtonText = btn1;
            if (!btn2.Equals(""))
            {
                dialog.SecondaryButtonText = btn2;
            }
            dialog.SecondaryButtonText = btn2;
            dialog.CloseButtonText = closebtn;
            dialog.DefaultButton = DefaultButton;
            dialog.Content = content;

            var result = await dialog.ShowAsync();
            return result;
        }

        public async void DecryptRun_Click(object sender, RoutedEventArgs e)
        {
            LockDown();
            DecryptProgress.Visibility = Visibility.Visible;

            string psd = FilePsdBox.Password;
            int algorithm_index = SelectDecryptionAlgorithmBox.SelectedIndex;
            int num_of_files = DataList.Count;
            int current_progress = 0;
            int error_time = 0;
            DecryptProgress.ShowPaused = false;
            DecryptProgress.ShowError = false;

            var tasks = new List<Task>();

            foreach (var item in DataList)
            {
                if (item is Modules.File file)
                {
                    string file_path = file.Path;
                    string new_file_path = file_path.Substring(0, file_path.Length - 4);

                    var task = Task.Run(async () =>
                    {
                        bool result = false;
                        switch (algorithm_index)
                        {
                            case 0:
                                if (!string.IsNullOrEmpty(psd))
                                {
                                    result = await Decrypt.AES_GCM_Decrypt(file_path, new_file_path, psd);
                                }
                                break;
                            default:
                                break;
                        }

                        if (!result) Interlocked.Increment(ref error_time);
                        Interlocked.Increment(ref current_progress);

                        // 更新进度条必须在主线程
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            DecryptProgress.Value = (int)((current_progress / (float)num_of_files) * 100);
                        });
                    });

                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            UnlockPage();

            int complete_files = num_of_files - error_time;
            if (complete_files == 0)
            {
                DecryptProgress.ShowError = true;
            }
            else if (complete_files < num_of_files)
            {
                DecryptProgress.ShowPaused = true;
            }
            else
            {
                DecryptProgress.ShowPaused = false;
                DecryptProgress.ShowError = false;
            }

            await ShowDialog("Decryption Complete", "OK", content: $"{complete_files} files decrypted. {error_time} failed.");
            DecryptProgress.Visibility = Visibility.Collapsed;
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
                        if (fileType == ".enc")
                        {
                            // 处理加密文件
                            FileList.Add(new Modules.File(fileName, createdDate, fileType, fileSize, filePath));
                        }
                        else
                        {
                            // 处理其他文件
                            // 这里可以添加其他文件类型的处理逻辑
                            // 例如：FileList.Add(new Modules.File(fileName, createdDate, fileType, fileSize, filePath));
                        }
                        //FileList.Add(new Modules.File(fileName, createdDate, fileType, fileSize, filePath));
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

        private void LockDown()
        {
            DecryptRun.IsEnabled = false;
        }

        private void UnlockPage()
        {
            DecryptRun.IsEnabled = true;
        }

        internal void UpdateList()
        {
            DataList = new ObservableCollection<DataLock.Modules.DataType>(
                FileList.Cast<DataLock.Modules.DataType>().Concat(FolderList.Cast<DataLock.Modules.DataType>())
            );

            DecryptFileDataGrid.ItemsSource = DataList;
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

        private void isSaveInDifferentPath_Toggled(object sender, RoutedEventArgs e)
        {
            // If ison is true, show the path selection dialog
            if (isSaveInDifferentPath.IsOn)
            {
                SavePathSettingCard.IsEnabled = true;
                decryptSavePathSettingsCard.IsExpanded = true;
            }
            else
            {
                // Hide the path selection dialog
                SavePathSettingCard.IsEnabled = false;
                decryptSavePathSettingsCard.IsExpanded = false;
            }
        }
    }
}
