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
using Microsoft.Windows.ApplicationModel.Resources;
using System.IO.Compression;

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
        private bool first_time = true;
        private string algorithm_link = "https://www.tianyibrad.com";
        public DecryptPage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        private void InitUI()
        {
            var loader = new ResourceLoader();
            string news_title = loader.GetString("UnderDevelopmentTitle");
            string news_content = loader.GetString("UnderDevelopmentContent");
            // Set News
            DecryptInfoBar.IsOpen = true;
            DecryptInfoBar.Severity = InfoBarSeverity.Warning;
            DecryptInfoBar.Title = news_title;
            DecryptInfoBar.Message = news_content;

            // Set the default learn more algorithm
            AlgorithmDescryption.Description = loader.GetString("AES_GCM_Des_Des");
            AlgorithmDescryption.Header = loader.GetString("AES_GCM_Des_Header");
            // Go to the link
            algorithm_link = loader.GetString("AES_GCM_Des_Link");
        }

        // Display Information
        private bool isDialogOpen = false;

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
            // Check all conditions (assuming CheckAllCondition checks password, selection etc.)
            if (!await CheckAllCondition()) return;
            LockDown(); // Disable UI elements
            DecryptProgress.Visibility = Visibility.Visible;
            DecryptProgress.Value = 0;
            DecryptProgress.ShowPaused = false;
            DecryptProgress.ShowError = false;

            string psd = FilePsdBox.Password;
            int algorithm_index = SelectDecryptionAlgorithmBox.SelectedIndex;
            int total = DataList.Count;
            int completed = 0;
            int error_count = 0; // Changed from error_time for clarity
            string firstOutputPath = null; // To store path for "Open Folder"

            var tasks = new List<Task>();
            // Use SemaphoreSlim for concurrency control, matching encryption
            var throttler = new SemaphoreSlim(4); // Adjust concurrency level as needed

            foreach (var item in DataList)
            {
                // This assumes item has properties like Path and Name, adapt if needed
                string itemPath = item.Path; // Assuming item.Path holds the full path
                string itemName = item.Name; // Assuming item.Name holds the file/folder name

                if (string.IsNullOrEmpty(itemPath)) continue; // Skip invalid items

                await throttler.WaitAsync(); // Wait for semaphore

                var task = Task.Run(async () =>
                {
                    bool success = false;
                    string currentOutputPath = null; // Path for this specific item
                    string fileExtension = Path.GetExtension(itemPath).ToLowerInvariant();

                    try
                    {
                        // Determine target path base (where to save decrypted results)
                        string targetPathBase;
                        if (isSaveInDifferentPathVar && !string.IsNullOrEmpty(targetPath))
                        {
                            targetPathBase = targetPath; // Use specified target directory
                        }
                        else
                        {
                            targetPathBase = Path.GetDirectoryName(itemPath); // Save in the same directory as the encrypted file
                        }

                        // --- Handle based on file extension ---
                        if (fileExtension == ".enc") // Standard encrypted file
                        {
                            string decryptedFileName = Path.GetFileNameWithoutExtension(itemPath); // Remove .enc
                            currentOutputPath = Path.Combine(targetPathBase, decryptedFileName);

                            switch (algorithm_index)
                            {
                                case 0:
                                    success = await Decrypt.AES_GCM_Decrypt(itemPath, currentOutputPath, psd);
                                    break;
                                case 1:
                                    success = await Decrypt.ChaCha20_Poly1305_Decrypt(itemPath, currentOutputPath, psd);
                                    break;
                            }
                        }
                        else if (fileExtension == ".encfolder") // Zipped and encrypted folder
                        {
                            string originalFolderName = Path.GetFileNameWithoutExtension(itemPath); // Remove .encfolder
                            currentOutputPath = Path.Combine(targetPathBase, originalFolderName); // Target is a folder
                            string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

                            bool decryptZipSuccess = false;
                            try
                            {
                                switch (algorithm_index)
                                {
                                    case 0:
                                        decryptZipSuccess = await Decrypt.AES_GCM_Decrypt(itemPath, tempZipPath, psd);
                                        break;
                                    case 1:
                                        decryptZipSuccess = await Decrypt.ChaCha20_Poly1305_Decrypt(itemPath, tempZipPath, psd);
                                        break;
                                }

                                if (decryptZipSuccess)
                                {
                                    // Ensure target directory exists and is empty or handle overwrite
                                    if (Directory.Exists(currentOutputPath))
                                    {
                                        // Optional: Decide how to handle existing folder (delete, merge, error)
                                        // Simple approach: Delete existing before extraction
                                        Directory.Delete(currentOutputPath, true);
                                    }
                                    Directory.CreateDirectory(currentOutputPath);
                                    ZipFile.ExtractToDirectory(tempZipPath, currentOutputPath);
                                    success = true; // Mark success if decryption and extraction worked
                                }
                            }
                            finally
                            {
                                // Clean up temp zip file
                                if (System.IO.File.Exists(tempZipPath))
                                {
                                    try { System.IO.File.Delete(tempZipPath); } catch { /* Log error maybe */ }
                                }
                            }
                        }
                        else if (fileExtension == ".encrec") // Recursively encrypted folder
                        {
                            // Call the recursive decryption method
                            // It returns the path of the decrypted folder on success, empty on failure
                            currentOutputPath = await DecryptFilesinFolder_Recursion(itemPath, algorithm_index, psd, (isSaveInDifferentPathVar && !string.IsNullOrEmpty(targetPath)) ? targetPath : null);
                            success = !string.IsNullOrEmpty(currentOutputPath);
                        }
                        else
                        {
                            // Unknown file type, treat as error
                            success = false;
                            await DispatcherQueue.EnqueueAsync(() =>
                            {
                                _ = ShowDialog("跳过", "OK", content: $"未知或不支持的文件类型: {itemName}");
                            });
                        }

                        // --- Post-processing ---
                        if (success)
                        {
                            // Store the first successful output path for "Open Folder"
                            Interlocked.CompareExchange(ref firstOutputPath, currentOutputPath, null);

                            // Delete original encrypted file/folder if needed and successful
                            if (!keepCurrentFile && System.IO.File.Exists(itemPath)) // Check File exists for .enc, .encfolder, .encrec
                            {
                                try
                                {
                                    System.IO.File.Delete(itemPath);
                                }
                                catch (Exception ex)
                                {
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog("清理失败", "OK", content: $"无法删除原始文件 {itemName}: {ex.Message}");
                                    });
                                }
                            }
                            else if (!keepCurrentFile && Directory.Exists(itemPath)) // Should not happen based on input, but defensive check
                            {
                                try { Directory.Delete(itemPath, true); } catch { /* Log */ }
                            }
                        }
                        else
                        {
                            // Decryption failed for this item
                            Interlocked.Increment(ref error_count);
                            await DispatcherQueue.EnqueueAsync(() =>
                            {
                                // Show specific error only if it wasn't shown inside DecryptFilesinFolder_Recursion etc.
                                // Generic failure message here if specific ones aren't sufficient.
                                _ = ShowDialog("解密失败", "OK", content: $"处理文件失败: {itemName}. 请检查密码或文件是否损坏。");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // General error during processing this item
                        Interlocked.Increment(ref error_count);
                        success = false;
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            _ = ShowDialog("错误", "OK", content: $"处理 {itemName} 时发生意外错误: {ex.Message}");
                        });
                    }
                    finally
                    {
                        // Update progress (always increment completed count)
                        Interlocked.Increment(ref completed);
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            DecryptProgress.Value = (int)((completed / (double)total) * 100);
                        });

                        throttler.Release(); // Release semaphore
                    }
                }); // End Task.Run

                tasks.Add(task);
            } // End foreach

            // Wait for all decryption tasks to complete
            await Task.WhenAll(tasks);

            UnlockPage(); // Re-enable UI

            // --- Final Dialog ---
            int successful_count = total - error_count;

            // Update progress bar appearance based on outcome
            if (successful_count == 0 && total > 0)
            {
                DecryptProgress.ShowError = true;
            }
            else if (error_count > 0)
            {
                DecryptProgress.ShowPaused = true; // Indicates partial success/failure
            }
            else
            {
                DecryptProgress.ShowPaused = false;
                DecryptProgress.ShowError = false;
            }

            var loader = new ResourceLoader();
            string decryption_complete_title = loader.GetString("DecryptionCompleteDialogTitle");
            string dialogOK = loader.GetString("DialogOK");
            string decryption_complete_content = loader.GetString("DecryptionCompleteDialogContent"); // "files decrypted successfully."
            string error_count_content = loader.GetString("DecryptionCompleteDialogContent"); // "files failed."
            string open_folder = loader.GetString("OpenFolderConfirm"); // "Open output location?"

            string dialogMessage = $"{successful_count} {decryption_complete_content}";
            if (error_count > 0)
            {
                dialogMessage += $"\n{error_count} {error_count_content}";
            }

            // Only offer to open folder if at least one item succeeded and we have a path
            string primaryButtonText = dialogOK;
            if (successful_count > 0 && !string.IsNullOrEmpty(firstOutputPath))
            {
                primaryButtonText = open_folder; // Change OK button text to "Open Folder" text
            }

            // Show dialog using ShowDialog helper method
            // Adjust ShowDialog parameters if needed (e.g., add a CloseButtonText parameter)
            ContentDialogResult result_from_dialog = await ShowDialog(decryption_complete_title, primaryButtonText, content: dialogMessage, closebtn: (primaryButtonText == open_folder) ? dialogOK : null);


            // if user clicks the primary button (which might be "Open Folder" or just "OK")
            if (result_from_dialog == ContentDialogResult.Primary && primaryButtonText == open_folder && !string.IsNullOrEmpty(firstOutputPath))
            {
                try
                {
                    // Get the directory to open. If firstOutputPath is a file, get its directory. If it's a folder, use it directly.
                    string folderToOpenPath;
                    System.IO.FileAttributes attr = System.IO.File.GetAttributes(firstOutputPath);
                    if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    {
                        folderToOpenPath = firstOutputPath;
                    }
                    else
                    {
                        folderToOpenPath = Path.GetDirectoryName(firstOutputPath);
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

            // Clear the input list only if all items were processed successfully (or based on user preference)
            if (error_count == 0)
            {
                DataList.Clear();
                FileList.Clear();
                FolderList.Clear();
            }
            // Optional: else, remove only successfully decrypted items from DataList

            DecryptProgress.Visibility = Visibility.Collapsed;
        }

        // Dummy 

        private async Task<string> DecryptFilesinFolder_Recursion(string encrecFilePath, int algorithmIndex, string password, string targetPathBase = null)
        {
            string finalOutputFolderPath = "";
            string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            string tempExtractedEncFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Determine the base path for the final decrypted folder
                string baseDirectory;
                if (!string.IsNullOrEmpty(targetPathBase))
                {
                    baseDirectory = targetPathBase;
                }
                else
                {
                    baseDirectory = Path.GetDirectoryName(encrecFilePath);
                }
                // Get the original folder name (remove .encrec extension)
                string originalFolderName = Path.GetFileNameWithoutExtension(encrecFilePath);
                finalOutputFolderPath = Path.Combine(baseDirectory, originalFolderName);

                // Ensure the final output directory exists
                Directory.CreateDirectory(finalOutputFolderPath);

                // 1. Decrypt the outer .encrec file to a temporary zip file
                bool outerDecryptSuccess = false;
                switch (algorithmIndex)
                {
                    case 0:
                        outerDecryptSuccess = await Decrypt.AES_GCM_Decrypt(encrecFilePath, tempZipPath, password);
                        break;
                    case 1:
                        outerDecryptSuccess = await Decrypt.ChaCha20_Poly1305_Decrypt(encrecFilePath, tempZipPath, password);
                        break;
                    default:
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            _ = ShowDialog("错误", "OK", content: $"未知解密算法索引 for {encrecFilePath}");
                        });
                        return ""; // Indicate failure
                }

                if (!outerDecryptSuccess)
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        _ = ShowDialog("解密失败", "OK", content: $"无法解密外层文件: {Path.GetFileName(encrecFilePath)}. 可能是密码错误或文件损坏。");
                    });
                    return ""; // Indicate failure
                }

                // 2. Extract the temporary zip file (containing .enc files) to a temporary folder
                try
                {
                    Directory.CreateDirectory(tempExtractedEncFolderPath);
                    // Important: ExtractRelativePath is a hypothetical helper needed here.
                    // ZipFile.ExtractToDirectory extracts the base directory from the zip.
                    // We need to handle the base directory within the zip correctly.
                    // Assuming the zip created by EncryptFilesinFolder_Recursion has a base directory matching the Guid.
                    ZipFile.ExtractToDirectory(tempZipPath, tempExtractedEncFolderPath, true); // true to overwrite if needed during testing
                }
                catch (Exception ex)
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        _ = ShowDialog("解压失败", "OK", content: $"无法解压临时文件 {Path.GetFileName(tempZipPath)}: {ex.Message}");
                    });
                    return ""; // Indicate failure
                }

                // 3. Find all .enc files in the temporary extracted folder
                var encryptedFiles = Directory.GetFiles(tempExtractedEncFolderPath, "*.enc", SearchOption.AllDirectories);

                bool allInnerDecryptionsSucceeded = true;

                // 4. Decrypt each .enc file to its final location, preserving structure
                foreach (var encryptedFilePathInTemp in encryptedFiles)
                {
                    // Get the relative path inside the temp extracted folder
                    string relativeEncPath = Path.GetRelativePath(tempExtractedEncFolderPath, encryptedFilePathInTemp);

                    // Remove the ".enc" extension to get the relative path for the final decrypted file
                    string relativeDecryptedPath = relativeEncPath.Substring(0, relativeEncPath.Length - ".enc".Length);

                    // Construct the final decrypted file path
                    string finalDecryptedFilePath = Path.Combine(finalOutputFolderPath, relativeDecryptedPath);

                    // Ensure the target directory exists for the decrypted file
                    Directory.CreateDirectory(Path.GetDirectoryName(finalDecryptedFilePath)!);

                    // Decrypt the individual .enc file
                    bool innerDecryptSuccess = false;
                    switch (algorithmIndex)
                    {
                        case 0:
                            innerDecryptSuccess = await Decrypt.AES_GCM_Decrypt(encryptedFilePathInTemp, finalDecryptedFilePath, password);
                            break;
                        case 1:
                            innerDecryptSuccess = await Decrypt.ChaCha20_Poly1305_Decrypt(encryptedFilePathInTemp, finalDecryptedFilePath, password);
                            break;
                            // No default needed as algorithm was checked earlier
                    }

                    if (!innerDecryptSuccess)
                    {
                        allInnerDecryptionsSucceeded = false;
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            // Log specific file failure, but continue processing others
                            _ = ShowDialog("部分解密失败", "OK", content: $"无法解密内部文件: {relativeDecryptedPath}. 可能是密码错误或文件损坏。");
                        });
                        // Decide if one failure should abort all: If so, add 'return "";' here.
                        // Current logic logs error and continues.
                    }
                }

                // 5. Optionally delete the original .encrec file
                if (allInnerDecryptionsSucceeded && !keepCurrentFile && System.IO.File.Exists(encrecFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(encrecFilePath);
                    }
                    catch (Exception ex)
                    {
                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            _ = ShowDialog("清理失败", "OK", content: $"无法删除原始 .encrec 文件 {Path.GetFileName(encrecFilePath)}: {ex.Message}");
                        });
                    }
                }

                // Return the path to the folder containing decrypted files, even if some inner files failed
                // Or return "" if the outer decryption or extraction failed, or if you want strict all-or-nothing success.
                return allInnerDecryptionsSucceeded ? finalOutputFolderPath : "";


            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    _ = ShowDialog("递归解密失败", "OK", content: $"处理 {Path.GetFileName(encrecFilePath)} 时发生错误: {ex.Message}");
                });
                return ""; // Indicate failure
            }
            finally
            {
                // 6. Clean up temporary files and folders
                try
                {
                    if (System.IO.File.Exists(tempZipPath))
                    {
                        System.IO.File.Delete(tempZipPath);
                    }
                    if (Directory.Exists(tempExtractedEncFolderPath))
                    {
                        Directory.Delete(tempExtractedEncFolderPath, true);
                    }
                }
                catch (Exception cleanupEx)
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        _ = ShowDialog("清理失败", "OK", content: $"无法清理临时文件/文件夹: {cleanupEx.Message}");
                    });
                    // Log cleanup error, but don't change the success/failure outcome of the decryption itself
                }
            }
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
                        if (fileType == ".enc" || fileType == ".encfolder" || fileType == ".encrec")
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
                UploadBanner.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                UploadBanner.BorderThickness = new Thickness(1);
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
            UploadBanner.BorderThickness = new Thickness(1);
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

        private void SelectDecryptionAlgorithmBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if it is the first time
            if (first_time)
            {
                first_time = false;
                return;
            }
            // Change the Header and Description of the Encryption Algorithm Card based on the selected algorithm
            var loader = new ResourceLoader();
            int algorithm_index = SelectDecryptionAlgorithmBox.SelectedIndex;
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
    }
}
