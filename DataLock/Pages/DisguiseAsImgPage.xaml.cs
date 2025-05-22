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
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage.AccessCache;

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

        private bool isChangeableExtensionOnVar = false;
        private bool isDialogOpen = false;
        private bool isSaveInDifferentPathVar = false;
        private string targetPath = string.Empty;
        private bool isDeModeVar = false;

        public DisguiseAsImgPage()
        {
            this.InitializeComponent();
            Init();
        }

        internal void Init()
        {
            InitBanner();
            //Debug_Function();
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
                string path = "ms-appx:///Assets/CipherflowBanner.png";
                SettingManager.DisguiseImagePath = path;
                DisguiseBannerImg.Source = new BitmapImage(new Uri(path)); // Handle the case where the path is null or empty
            }
        }

        internal void Debug_Function()
        {
            string path = "ms-appx:///Assets/CipherflowBanner.png";
            DisguiseBannerImg.Source = new BitmapImage(new Uri(path));

            //DisguiseBannerImg.Source = new BitmapImage(new Uri(SettingManager.DisguiseImagePath));
            //DisguiseBannerImg.Source = null;
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
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".gif");

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
            int error_count = 0;
            string global_path = ""; // Initialize global_path variable
            if (isSaveInDifferentPathVar && !targetPath.Equals(String.Empty))
            {
                global_path = targetPath;
            }

            foreach (var item in DataList)
            {
                string itemPath = item.Path; // item.Path holds the full path
                string itemName = item.Name; // item.Name holds the file/folder name

                // Check if the item is a file or folder
                // If it is a folder, skip it
                if (item is DataLock.Modules.Folder)
                {
                    continue; // Skip folders
                }

                if (string.IsNullOrEmpty(itemPath)) continue; // Skip invalid items
                await throttler.WaitAsync(); // Wait for semaphore 

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string outputPath = "";
                        if (isDeModeVar)
                        {
                            if (isSaveInDifferentPathVar && !targetPath.Equals(String.Empty))
                            {
                                // Remove the .jpg and .rar from the file name
                                string indexPath = targetPath;
                                string indexItemName = Path.GetFileNameWithoutExtension(itemPath);
                                outputPath = Path.Combine(indexPath, indexItemName);
                            }
                            else
                            {
                                // Remove the .jpg and .rar from the file name
                                string indexPath = Path.GetDirectoryName(itemPath);
                                string indexItemName = Path.GetFileNameWithoutExtension(itemPath);
                                outputPath = Path.Combine(indexPath, indexItemName);
                            }
                                

                        }
                        else
                        {
                            // If the user has selected to save in a different path
                            if (isSaveInDifferentPathVar && !targetPath.Equals(String.Empty))
                            {
                                // Use the target path set by the user
                                // if user has switched the mode to unmask mode, use the original path
                                // Otherwise, use the target path
                                outputPath = Path.Combine(targetPath, itemName + image_extension);
                            }
                            else
                            {
                                // Use the original path of the file
                                outputPath = Path.Combine(Path.GetDirectoryName(itemPath), itemName + image_extension);
                            }
                        }
                        

                        global_path = outputPath; // Update global_path variable

                        // Output is the same as the input file folder
                        string outputFolderPath = Path.GetDirectoryName(itemPath);
                        string dataExtension = image_extension;
                        // Call the disguise function
                        // If the file is compressed files, use the DisguiseToImageCompress function
                        // Otherwise, use the DisguiseToImageAny function
                        bool result = false;

                        // If the mode is unmask mode 
                        if (isDeModeVar)
                        {
                            result = await Disguise.ExtractFromDisguisedImageAny(itemPath, outputPath, image_path);
                        }
                        else
                        {
                            if (isChangeableExtensionOnVar && Disguise.IsSupportedCompressedFile(itemPath))
                            {
                                result = await Disguise.DisguiseToImageCompress(itemPath, SettingManager.DisguiseImagePath, outputPath);
                            }
                            else
                            {
                                // For other file types, use the DisguiseToImageAny function
                                result = await Disguise.DisguiseToImageAny(itemPath, SettingManager.DisguiseImagePath, outputPath, outputFolderPath, dataExtension);
                            }
                        }

                        if (result)
                        {
                            completed += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        error_count += 1;
                        // Handle the error (e.g., log it, show a message, etc.)

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

            // --- Final Dialog ---
            int successful_count = total - error_count;

            // Update progress bar appearance based on outcome
            if (successful_count == 0 && total > 0)
            {
                DisguisionProgress.ShowError = true;
            }
            else if (error_count > 0)
            {
                DisguisionProgress.ShowPaused = true; // Indicates partial success/failure
            }
            else
            {
                DisguisionProgress.ShowPaused = false;
                DisguisionProgress.ShowError = false;
            }

            var loader = new ResourceLoader();
            string disguise_complete_title = loader.GetString("DisguisionCompleteDialogTitle");
            string dialogOK = loader.GetString("DialogOK");
            string disguise_complete_content = loader.GetString("DisguisionCompleteDialogContent");
            string unmask_complete_content = loader.GetString("UnmaskCompleteDialogContent"); // "files decrypted."
            string error_count_content = loader.GetString("DisguisionCompleteDialogContentErr"); // "files failed."
            string open_folder = loader.GetString("OpenFolderConfirm"); // "Open output location?"

            string dialogMessage = $"{successful_count} {disguise_complete_content}";
            if (error_count > 0)
            {
                dialogMessage += $"\n{error_count} {error_count_content}";
            }

            // Only offer to open folder if at least one item succeeded and we have a path
            string primaryButtonText = dialogOK;
            if (successful_count > 0 && !string.IsNullOrEmpty(global_path))
            {
                primaryButtonText = open_folder; // Change OK button text to "Open Folder" text
            }

            // Show dialog using ShowDialog helper method
            // Adjust ShowDialog parameters if needed (e.g., add a CloseButtonText parameter)



            ContentDialogResult result_from_dialog = await ShowDialog(disguise_complete_title, primaryButtonText, content: dialogMessage, closebtn: (primaryButtonText == open_folder) ? dialogOK : null);
            // if user clicks the primary button (which might be "Open Folder" or just "OK")
            if (result_from_dialog == ContentDialogResult.Primary && primaryButtonText == open_folder && !string.IsNullOrEmpty(global_path))
            {
                try
                {
                    // Get the directory to open. If firstOutputPath is a file, get its directory. If it's a folder, use it directly.
                    string folderToOpenPath;
                    System.IO.FileAttributes attr = System.IO.File.GetAttributes(global_path);
                    if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    {
                        folderToOpenPath = global_path;
                    }
                    else
                    {
                        folderToOpenPath = Path.GetDirectoryName(global_path);
                    }

                    if (!string.IsNullOrEmpty(folderToOpenPath) && Directory.Exists(folderToOpenPath))
                    {
                        var folder = await StorageFolder.GetFolderFromPathAsync(folderToOpenPath);
                        await Windows.System.Launcher.LaunchFolderAsync(folder);
                    }
                    else
                    {
                        _ = ShowDialog("错误", "OK", content: "无法找到或打开输出文件夹。");
                    }
                }
                catch (Exception ex)
                {
                    _ = ShowDialog("错误", "OK", content: $"无法打开文件夹: {ex.Message}");
                }
            }
        }

        private async Task<ContentDialogResult> ShowDialog(string title, string btn1, string btn2 = "", string closebtn = "Cancel", ContentDialogButton DefaultButton = ContentDialogButton.Primary, string content = "")
        {
            if (isDialogOpen) return ContentDialogResult.None; // 防止重复显示
            var loader = new ResourceLoader();
            isDialogOpen = true;

            if (closebtn.Equals("Cancel"))
            {
                closebtn = loader.GetString("DialogCancel");
            }

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

        private void isChangeableExtensionOn_Toggled(object sender, RoutedEventArgs e)
        {
            // Toggle the isChangeableExtensionOnVar variable
            if (sender is ToggleSwitch toggleSwitch)
            {
                isChangeableExtensionOnVar = toggleSwitch.IsOn;
            }
        }

        private void isSaveInDifferentPath_Toggled(object sender, RoutedEventArgs e)
        {
            // Toggle the isSaveInDifferentPath variable
            if (sender is ToggleSwitch toggleSwitch)
            {
                isSaveInDifferentPathVar = toggleSwitch.IsOn;
            }

            // If isSaveInDifferentPath is true, enable the SavePathSettingCard
            if (isSaveInDifferentPathVar)
            {
                SavePathSettingCard.IsEnabled = true;
                // Expand the card
                disguiseSavePathSettingsCard.IsExpanded = true;
            }
            else
            {
                SavePathSettingCard.IsEnabled = false;
                // Collapse the card
                disguiseSavePathSettingsCard.IsExpanded = false;
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

        private async void DeBrowseAndAllFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button to prevent multiple clicks
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

            //re-enable the button
            senderButton.IsEnabled = true;
        }

        private void isDeMode_Toggled(object sender, RoutedEventArgs e)
        {
            var loader = new ResourceLoader();
            string dsguiseMode = loader.GetString("DisguiseMode");
            string unmaskMode = loader.GetString("UnmaskMode");

            // Toggle the isDeMode variable
            if (sender is ToggleSwitch toggleSwitch)
            {
                isDeModeVar = toggleSwitch.IsOn;
            }
            // If isDeMode is true, change the text of description of UnmaskModeSetting
            if (isDeModeVar)
            {
                UnmaskModeSetting.Description = unmaskMode;
                ChangeableExtensionsetting.IsEnabled = false;
            }
            else
            {
                UnmaskModeSetting.Description = dsguiseMode;
                ChangeableExtensionsetting.IsEnabled = true;
            }
        }
    }
}
