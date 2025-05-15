using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Documents;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using DataLock.Functions;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DisguiseAsImgPage : Page
    {
        public ObservableCollection<DataLock.Modules.File> FileList { get; } = new ObservableCollection<DataLock.Modules.File>();
        public ObservableCollection<DataLock.Modules.Folder> FolderList { get; } = new ObservableCollection<DataLock.Modules.Folder>();
        public ObservableCollection<DataLock.Modules.DataType> DataList { get; set; } = new ObservableCollection<DataLock.Modules.DataType>();

        public DisguiseAsImgPage()
        {
            this.InitializeComponent();
            Init();
        }

        internal void Init()
        {
            InitBanner();
        }

        // Update the InitBanner method to convert the string path to an ImageSource
        internal void InitBanner()
        {
            if (!string.IsNullOrEmpty(SettingManager.DisguiseImagePath))
            {
                DisguiseBannerImg.Source = new BitmapImage(new Uri(SettingManager.DisguiseImagePath));
            }
            else
            {
                DisguiseBannerImg.Source = null; // Handle the case where the path is null or empty
            }
        }

        internal void UpdateList()
        {
            DataList = new ObservableCollection<DataLock.Modules.DataType>(
                FileList.Cast<DataLock.Modules.DataType>().Concat(FolderList.Cast<DataLock.Modules.DataType>())
            );

            DisguiseFileDataGrid.ItemsSource = DataList;
            NumofFilesRecord.Inlines.Clear(); // Clear current content counts

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
            UploadBanner.BorderThickness = new Thickness(1);
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
                        //string folderName = folder.Name;
                        //string folderPath = folder.Path;
                        //DateTime createdDate = folder.DateCreated.DateTime;
                        //FolderList.Add(new Modules.Folder(folderName, createdDate, folderPath));
                    }
                }
                UpdateList();
                UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                UploadBanner.BorderThickness = new Thickness(1);
            }
        }

        private async void ChangePictureBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangePictureBtn.IsEnabled = false;
            Button senderButton = (Button)sender;
            senderButton.IsEnabled = false;

            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = App.m_window;

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a file
            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                string file_path = file.Path;
                SettingManager.DisguiseImagePath = file_path; 
                DisguiseBannerImg.Source = new BitmapImage(new Uri(file_path));
            }
            else
            {

            }

            ChangePictureBtn.IsEnabled = true;
        }

        private async void DisguiseRun_Run(object sender, RoutedEventArgs e)
        {
            DisguiseRun.IsEnabled = false;

            DisguisionProgress.Visibility = Visibility.Visible;
            DisguisionProgress.Value = 0;
            DisguisionProgress.ShowPaused = false;
            DisguisionProgress.ShowError = false;

            // Image Path 
            string image_path = SettingManager.DisguiseImagePath;
            string image_name = Path.GetFileName(image_path);
            string image_extension = Path.GetExtension(image_path);

            var tasks = new List<Task>();
            // Use SemaphoreSlim for concurrency control, matching encryption
            var throttler = new SemaphoreSlim(4); // Adjust concurrency level as needed

            int total = DataList.Count;
            int completed = 0;

            foreach (var item in DataList)
            {
                string itemPath = item.Path; // item.Path holds the full path
                string itemName = item.Name; // item.Name holds the file/folder name

                if (string.IsNullOrEmpty(itemPath)) continue; // Skip invalid items
                await throttler.WaitAsync(); // Wait for semaphore 

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string outputPath = Path.Combine(Path.GetDirectoryName(itemPath), itemName + image_extension);
                        // Output is the same as the input file folder
                        string outputFolderPath = Path.GetDirectoryName(itemPath);
                        string dataExtension = image_extension;
                        // Call the disguise function
                        bool result = await Disguise.DisguiseToImagePublic(itemPath, SettingManager.DisguiseImagePath, outputPath, outputFolderPath, dataExtension);
                        if (result)
                        {
                            completed += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                    finally
                    {
                        // Update progress (always increment completed count)
                        Interlocked.Increment(ref completed);
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            DisguisionProgress.Value = (int)((completed / (double)total) * 100);
                        });

                        throttler.Release(); // Release semaphore
                    }
                }));
                
            }
            await Task.WhenAll(tasks);

            DisguiseRun.IsEnabled = true;
        }

        private async void ChooseFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangePictureBtn.IsEnabled = false;
            Button senderButton = (Button)sender;
            senderButton.IsEnabled = false;

            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = App.m_window;

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a file
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();

            if (files.Count > 0)
            {
                foreach (var item in files)
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
                }
                UpdateList();
            }
            else
            {

            }
        }
    }
}
