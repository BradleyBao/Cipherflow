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
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;

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
        public string targetPath = string.Empty;
        private bool isSaveInDifferentPathVar = false;
        private bool keepCurrentFile = false;
        public DecryptPage()
        {
            this.InitializeComponent();
        }

        // Display Information
        private bool isDialogOpen = false;

        private async Task<ContentDialogResult> ShowDialog(string title, string btn1, string btn2 = "", string closebtn = "Cancel", ContentDialogButton DefaultButton = ContentDialogButton.Primary, string content = "")
        {
            if (isDialogOpen) return ContentDialogResult.None; // 防止重复显示
            isDialogOpen = true;

            try
            {
                ContentDialog dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = title,
                    PrimaryButtonText = btn1,
                    SecondaryButtonText = string.IsNullOrEmpty(btn2) ? null : btn2,
                    CloseButtonText = closebtn,
                    DefaultButton = DefaultButton,
                    Content = content
                };

                return await dialog.ShowAsync();
            }
            finally
            {
                isDialogOpen = false;
            }
        }

        private async Task<bool> CheckAllCondition()
        {
            // Check if password is empty or not
            if (string.IsNullOrEmpty(FilePsdBox.Password))
            {
                await ShowDialog("Error", "OK", content: "Password cannot be empty.");
                return false;
            }

            // Check if if save in different path is selected 
            if (isSaveInDifferentPath.IsOn)
            {
                // Check if the path is empty
                if (string.IsNullOrEmpty(targetPath))
                {
                    await ShowDialog("Error", "OK", content: "Please select a save path.");
                    return false;
                }
            }

            // Check if the selected files are empty
            if (DataList.Count == 0)
            {
                await ShowDialog("Error", "OK", content: "Please select files to encrypt.");
                return false;
            }

            return true;
        }

        public async void DecryptRun_Click(object sender, RoutedEventArgs e)
        {
            // Check all conditions
            if (!await CheckAllCondition()) return;
            LockDown();
            DecryptProgress.Visibility = Visibility.Visible;

            string psd = FilePsdBox.Password;
            int algorithm_index = SelectDecryptionAlgorithmBox.SelectedIndex;
            int num_of_files = DataList.Count;
            int current_progress = 0;
            int error_time = 0;
            string new_file_path = "";
            DecryptProgress.ShowPaused = false;
            DecryptProgress.ShowError = false;

            var tasks = new List<Task>();

            foreach (var item in DataList)
            {
                if (item is Modules.File file)
                {
                    var task = Task.Run(async () =>
                    {
                        bool result = false;
                        string file_path = file.Path;
                        if (!string.IsNullOrEmpty(targetPath) && isSaveInDifferentPathVar)
                        {
                            new_file_path = Path.Combine(targetPath, file.Name);
                        }
                        else
                        {
                            new_file_path = file_path.Substring(0, file_path.Length - 4);
                        }
                        switch (algorithm_index)
                        {
                            case 0:
                                if (!string.IsNullOrEmpty(psd))
                                {
                                    result = await Decrypt.AES_GCM_Decrypt(file_path, new_file_path, psd);
                                    if (result)
                                    {
                                        // Decrypt success
                                        if (!keepCurrentFile)
                                        {
                                            System.IO.File.Delete(file_path);
                                        }
                                    }
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
                isSaveInDifferentPathVar = true;
                SavePathSettingCard.IsEnabled = true;
                decryptSavePathSettingsCard.IsExpanded = true;
            }
            else
            {
                // Hide the path selection dialog
                isSaveInDifferentPathVar = false;
                SavePathSettingCard.IsEnabled = false;
                decryptSavePathSettingsCard.IsExpanded = false;
            }
        }

        private void keepOriginalFile_Toggled(object sender, RoutedEventArgs e)
        {
            // If ison is true, show the path selection dialog
            if (keepOriginalFile.IsOn)
            {
                keepCurrentFile = true;
            }
            else
            {
                // Hide the path selection dialog
                keepCurrentFile = false;
            }
        }

        private async void ChooseSavePathFolder_Click(object sender, RoutedEventArgs e)
        {
            ChooseSavePathFolder.IsEnabled = false;
            // Open a folder picker dialog to select the save path
            // Create a folder picker
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = App.m_window;

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                SavePathSettingCard.Description = "Selected Folder: " + folder.Name;
                targetPath = folder.Path; // Store the selected path
            }

            ChooseSavePathFolder.IsEnabled = true; // Re-enable the button
        }
    }
}
