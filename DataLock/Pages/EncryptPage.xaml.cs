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
using Microsoft.Windows.ApplicationModel.Resources;
using System.Text;
using System.IO.Compression;
using System.Security;

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
        private bool EncryptEveryFileinFolderVar = false;
        private bool keepCurrentFile = true;
        private bool isProcessing;
        private CancellationTokenSource operationCancellation;
        private string algorithm_link = "https://www.tianyibrad.com";
        private bool first_time = true;

        public EncryptPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            //LoadEncryptedFiles();
            InitUI();
        }

        private void InitUI()
        {
            var loader = new ResourceLoader();
            string news_title = loader.GetString("ChangeTempFolderHintTitle");
            string news_content = loader.GetString("ChangeTempFolderHintBody");
            // Set News
            EncryptInfoBar.IsOpen = true;
            EncryptInfoBar.Severity = InfoBarSeverity.Informational;
            EncryptInfoBar.Title = news_title;
            EncryptInfoBar.Message = news_content;

            // Set the default learn more algorithm
            AlgorithmDescryption.Description = loader.GetString("AES_GCM_Des_Des");
            AlgorithmDescryption.Header = loader.GetString("AES_GCM_Des_Header");
            // Go to the link
            algorithm_link = loader.GetString("AES_GCM_Des_Link");

        }

        private void LoadEncryptedFiles()
        {
            // 模拟：你可以改成从某个加密文件夹扫描
            string mockPath = @"C:\EncryptedFiles";

            FileList.Add(new Modules.File("Secret-Report.enc", DateTime.Now.AddDays(-3), ".enc", 24000, mockPath));
            FileList.Add(new Modules.File("Finance-2023.enc", DateTime.Now.AddMonths(-1), ".enc", 132000, mockPath));
            FileList.Add(new Modules.File("PhotosBackup.enc", DateTime.Now.AddMonths(-2), ".enc", 5242880, mockPath));
        }

        private async void UploadBanner_Drop(object sender, DragEventArgs e)
        {
            if (isProcessing) return;
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var storageItems = await e.DataView.GetStorageItemsAsync();
                if (isProcessing) return;
                foreach (var item in storageItems)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        // Fix: Retrieve file size using GetBasicPropertiesAsync
                        var basicProperties = await file.GetBasicPropertiesAsync();
                        if (isProcessing) return;
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
                UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                UploadBanner.BorderThickness = new Thickness(1);
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
            UploadBanner.BorderThickness = new Thickness(1);
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
            EncryptInfoBar.Title = title;
            EncryptInfoBar.Message = content;
            EncryptInfoBar.IsOpen = true;
            if (!ReferenceEquals(Frame?.Content, this)) return ContentDialogResult.None;
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


        private void LockDown()
        {
            isProcessing = true;
            UploadBanner.AllowDrop = false;
            BrowseAndAllFilesBtn.IsEnabled = false;
            OperationSettings.IsEnabled = false;
            EncryptFileDataGrid.IsEnabled = false;
            EncryptRun.IsEnabled = false;
        }

        private void UnlockPage()
        {
            isProcessing = false;
            UploadBanner.AllowDrop = true;
            BrowseAndAllFilesBtn.IsEnabled = true;
            OperationSettings.IsEnabled = true;
            EncryptFileDataGrid.IsEnabled = true;
            EncryptRun.IsEnabled = true;
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;
            if ((sender as FrameworkElement)?.DataContext is Modules.File file) FileList.Remove(file);
            else if ((sender as FrameworkElement)?.DataContext is Modules.Folder folder) FolderList.Remove(folder);
            UpdateList();
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

        private void CancelOperation_Click(object sender, RoutedEventArgs e)
        {
            if (operationCancellation == null) return;
            operationCancellation.Cancel();
            CancelOperation.IsEnabled = false;
            EncryptInfoBar.Severity = InfoBarSeverity.Informational;
            EncryptInfoBar.Title = new ResourceLoader().GetString("OperationCancelling");
            EncryptInfoBar.Message = new ResourceLoader().GetString("OperationCancellingDetail");
            EncryptInfoBar.IsOpen = true;
        }

        private async void EncryptRun_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing || !await CheckAllCondition()) return;
            using var cancellation = new CancellationTokenSource();
            operationCancellation = cancellation;
            var token = cancellation.Token;
            var operation = new CryptoOperation
            {
                Decrypting = false,
                Algorithm = SelectEncryptionAlgorithmBox.SelectedIndex,
                Password = FilePsdBox.Password,
                OutputDirectory = isSaveInDifferentPathVar ? targetPath : null,
                TempDirectory = SettingManager.TempFilePath,
                KeepOriginal = keepCurrentFile,
                EncryptFilesIndividually = EncryptEveryFileinFolderVar
            };
            FilePsdBox.Password = string.Empty;
            var inputs = DataList.ToList();
            int completed = 0;
            int failed = 0;
            string firstError = null;
            string firstOutputPath = null;
            LockDown();
            CancelOperation.Visibility = Visibility.Visible;
            CancelOperation.IsEnabled = true;
            EncryptProgress.Visibility = Visibility.Visible;
            EncryptProgress.Value = 0;
            EncryptProgress.ShowError = false;
            EncryptProgress.ShowPaused = false;
            EncryptInfoBar.IsOpen = false;
            try
            {
                await Parallel.ForEachAsync(inputs,
                    new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token },
                    async (item, workerToken) =>
                    {
                        bool succeeded = false;
                        try
                        {
                            string output = await operation.ProcessAsync(item.Path, item is Modules.Folder, workerToken);
                            Interlocked.CompareExchange(ref firstOutputPath, output, null);
                            succeeded = true;
                        }
                        catch (OperationCanceledException) when (workerToken.IsCancellationRequested) { throw; }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failed);
                            Interlocked.CompareExchange(ref firstError, $"{item.Name}: {ex.Message}", null);
                        }
                        // Cancelled or unstarted items remain selected; completed items do not run twice.
                        Interlocked.Increment(ref completed);
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            if (succeeded)
                            {
                                if (item is Modules.File file) FileList.Remove(file);
                                else if (item is Modules.Folder folder) FolderList.Remove(folder);
                                UpdateList();
                            }
                            EncryptProgress.Value = completed * 100.0 / inputs.Count;
                        });
                    });
                CancelOperation.Visibility = Visibility.Collapsed;
                var loader = new ResourceLoader();
                EncryptInfoBar.Severity = failed == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
                EncryptProgress.ShowError = failed == inputs.Count;
                EncryptProgress.ShowPaused = failed > 0 && failed < inputs.Count;
                var result = await ShowDialog(loader.GetString("EncryptionCompleteDialogTitle"),
                    loader.GetString(firstOutputPath == null ? "DialogOK" : "OpenFolder"), closebtn: loader.GetString("DialogOK"),
                    content: string.Format(loader.GetString("OperationResult"), completed - failed, failed, inputs.Count - completed)
                        + (firstError == null ? "" : "\n" + firstError));
                if (result == ContentDialogResult.Primary && firstOutputPath != null)
                {
                    string folderPath = Directory.Exists(firstOutputPath) ? firstOutputPath : Path.GetDirectoryName(firstOutputPath);
                    var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                var loader = new ResourceLoader();
                EncryptProgress.ShowPaused = true;
                EncryptInfoBar.Severity = InfoBarSeverity.Warning;
                EncryptInfoBar.Title = loader.GetString("OperationCancelled");
                EncryptInfoBar.Message = string.Format(loader.GetString("OperationResult"), completed - failed, failed, inputs.Count - completed)
                    + "\n" + loader.GetString("OperationCancellationDetail")
                    + (firstError == null ? "" : "\n" + firstError);
                EncryptInfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                EncryptProgress.ShowError = true;
                EncryptInfoBar.Severity = InfoBarSeverity.Error;
                await ShowDialog("Error", "OK", content: ex.Message);
            }
            finally
            {
                operationCancellation = null;
                CancelOperation.Visibility = Visibility.Collapsed;
                UnlockPage();
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

        private async void BrowseAndAllFilesBtn_Click(object sender, RoutedEventArgs e)
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
                        var basicProperties = await file.GetBasicPropertiesAsync();
                        if (isProcessing) return;
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

        private void SelectEncryptionAlgorithmBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if it is the first time
            if (first_time)
            {
                first_time = false;
                return;
            }
            // Change the Header and Description of the Encryption Algorithm Card based on the selected algorithm
            var loader = new ResourceLoader();
            int algorithm_index = SelectEncryptionAlgorithmBox.SelectedIndex;
            if (algorithm_index == 1)
            {
                AlgorithmDescryption.Description = loader.GetString("ChaCha20_Des_Des");
                AlgorithmDescryption.Header = loader.GetString("ChaCha20_Des_Header");
                // Go to the link
                algorithm_link = loader.GetString("ChaCha20_Des_Link");
            }
            else if (algorithm_index == 0)
            {
                AlgorithmDescryption.Description = loader.GetString("AES_GCM_Des_Des");
                AlgorithmDescryption.Header = loader.GetString("AES_GCM_Des_Header");
                // Go to the link
                algorithm_link = loader.GetString("AES_GCM_Des_Link");
            }
        }

        private async void AlgorithmDescryption_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(algorithm_link));

        }

        private void EncryptEveryFileinFolder_Toggled(object sender, RoutedEventArgs e)
        {
            // If the toggle is on, set the EncryptEveryFileinFolder variable to true
            if (EncryptEveryFileinFolder.IsOn)
            {
                EncryptEveryFileinFolderVar = true;
            }
            else
            {
                EncryptEveryFileinFolderVar = false;
            }
        }
    }
}
