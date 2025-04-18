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
    public sealed partial class EncryptPage : Page
    {
        public ObservableCollection<DataLock.Modules.File> FileList { get; } = new ObservableCollection<DataLock.Modules.File>();

        public ObservableCollection<DataLock.Modules.Folder> FolderList { get; } = new ObservableCollection<DataLock.Modules.Folder>();

        public ObservableCollection<DataLock.Modules.DataType> DataList { get; set; } = new ObservableCollection<DataLock.Modules.DataType>();

        public string targetPath = string.Empty;
        private bool isSaveInDifferentPathVar = false;
        private bool keepCurrentFile = false;

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

        private void isSaveInDifferentPath_Toggled(object sender, RoutedEventArgs e)
        {
            // If ison is true, show the path selection dialog
            if (isSaveInDifferentPath.IsOn)
            {
                isSaveInDifferentPathVar = true;
                SavePathSettingCard.IsEnabled = true;
                encryptSavePathSettingsCard.IsExpanded = true;
            }
            else
            {
                isSaveInDifferentPathVar = false;
                // Hide the path selection dialog
                SavePathSettingCard.IsEnabled = false;
                encryptSavePathSettingsCard.IsExpanded = false;
            }
        }

        // DIY Function
        //public delegate void Operation();

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


        private void LockDown()
        {
            EncryptRun.IsEnabled = false;
        }

        private void UnlockPage()
        {
            EncryptRun.IsEnabled = true;
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

        private async void EncryptRun_Click(object sender, RoutedEventArgs e)
        {
            if (!await CheckAllCondition()) // Fix: Await the Task<bool> returned by CheckAllCondition
            {
                return;
            }
            LockDown();
            EncryptProgress.Visibility = Visibility.Visible;
            EncryptProgress.Value = 0;

            string psd = FilePsdBox.Password;
            int algorithm_index = SelectEncryptionAlgorithmBox.SelectedIndex;
            int total = DataList.Count;
            int completed = 0;
            EncryptProgress.ShowPaused = false;
            EncryptProgress.ShowError = false;
            var tasks = new List<Task>();
            string new_file_path = "";

            // 最大并发数，可调整
            var throttler = new SemaphoreSlim(4);

            foreach (var item in DataList)
            {
                await throttler.WaitAsync(); // 控制并发量

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (item is Modules.File file)
                        {
                            string file_path = file.Path;
                            // If "Save in different path" is selected and targetPath is not empty, use the target path 
                            if (isSaveInDifferentPathVar && !string.IsNullOrEmpty(targetPath))
                            {
                                new_file_path = Path.Combine(targetPath, file.Name + ".enc");
                            }
                            else
                            {
                                new_file_path = file_path + ".enc";
                            }
                                

                            switch (algorithm_index)
                            {
                                case 0:
                                    if (string.IsNullOrEmpty(psd))
                                    {
                                         //await Encrypt.AES_GCM_Encrypt(file_path, new_file_path); // implement if needed
                                    }
                                    else
                                    {
                                        await Encrypt.AES_GCM_Encrypt(file_path, new_file_path, psd);
                                        if (!keepCurrentFile)
                                        {
                                            System.IO.File.Delete(file_path); // Delete the original file if not keeping it
                                        }
                                    }
                                    break;
                            }
                        }

                        // Update Progress in UI Thread
                        Interlocked.Increment(ref completed);
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            EncryptProgress.Value = (int)((completed / (float)total) * 100);
                        });
                    }
                    catch (Exception ex)
                    {
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            _ = ShowDialog("Error", "OK", content: $"File: {item.Name}\nError: {ex.Message}");
                        });
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            UnlockPage();
            await ShowDialog("Encryption Complete", "OK", content: $"{total} file(s) have been encrypted.");
            EncryptProgress.Visibility = Visibility.Collapsed;
        }

        // Fix the return type of the method to Task instead of Task<null>
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

        private void keepOriginalFile_Toggled(object sender, RoutedEventArgs e)
        {
            if(keepOriginalFile.IsOn)
            {
                keepCurrentFile = true;
            }
            else
            {
                keepCurrentFile = false;
            }
        }
    }
}
