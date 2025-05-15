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
        private bool keepCurrentFile = false;
        private string algorithm_link = "https://www.tianyibrad.com";
        private bool first_time = true;

        public EncryptPage()
        {
            this.InitializeComponent();
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
            var loader = new ResourceLoader();
            string tempFolder = SettingManager.TempFilePath;

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
                                // AES-GCM
                                case 0:
                                    if (string.IsNullOrEmpty(psd))
                                    {
                                        //await Encrypt.AES_GCM_Encrypt(file_path, new_file_path); // implement if needed
                                    }
                                    else if (new FileInfo(file_path).Length >= 2L * 1024 * 1024 * 1024)
                                    {
                                        // Use Stream Encryption
                                        await Encrypt.AES_GCM_Encrypt_Stream(file_path, new_file_path, psd);
                                    }
                                    else
                                    {
                                        await Encrypt.AES_GCM_Encrypt(file_path, new_file_path, psd);
                                    }
                                    break;
                                // ChaCha20-Poly1305
                                case 1:
                                    if (string.IsNullOrEmpty(psd))
                                    {

                                    }
                                    else
                                    {
                                        // Check if the file is larger than 2GB
                                        if (new FileInfo(file_path).Length >= 2L * 1024 * 1024 * 1024)
                                        {
                                            // Use Stream Encryption
                                            await Encrypt.ChaCha20_Poly1305_Encrypt_Stream(file_path, new_file_path, psd);
                                        }
                                        else
                                        {
                                            await Encrypt.ChaCha20_Poly1305_Encrypt(file_path, new_file_path, psd);
                                        }
                                            
                                    }
                                    break;
                            }

                            if (!keepCurrentFile)
                            {
                                System.IO.File.Delete(file_path); // Delete the original file if not keeping it
                            }

                        }
                        else if (item is Modules.Folder folder)
                        {
                            string folder_path = folder.Path;

                            if (EncryptEveryFileinFolderVar)
                            {
                                // 如果是“逐文件加密”模式，则递归加密所有文件
                                new_file_path = await EncryptFilesinFolder_Recursion(folder_path, algorithm_index, psd, targetPath);
                            }
                            else
                            {
                                try
                                {
                                    // 创建临时 zip 文件路径
                                    //string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                                    string tempZipPath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + ".zip");

                                    // Step 1: 压缩目录
                                    await Task.Run(() =>
                                    {
                                        ZipFile.CreateFromDirectory(folder_path, tempZipPath, CompressionLevel.Fastest, false);
                                    });

                                    // Step 2: 生成目标加密文件路径
                                    if (isSaveInDifferentPathVar && !string.IsNullOrEmpty(targetPath))
                                    {
                                        new_file_path = Path.Combine(targetPath, folder.Name + ".encfolder");
                                    }
                                    else
                                    {
                                        new_file_path = folder_path + ".encfolder";
                                    }

                                    // Step 3: 加密
                                    switch (algorithm_index)
                                    {
                                        case 0: // AES-GCM

                                            // Check if the file is larger than 2GB 
                                            

                                            if (!string.IsNullOrEmpty(psd))
                                            {
                                                if (new FileInfo(tempZipPath).Length >= 2L * 1024 * 1024 * 1024)
                                                {
                                                    // Use Stream Encryption
                                                    await Encrypt.AES_GCM_Encrypt_Stream(tempZipPath, new_file_path, psd);
                                                }
                                                else
                                                {
                                                    // Use File Encryption
                                                    await Encrypt.AES_GCM_Encrypt(tempZipPath, new_file_path, psd);
                                                }
                                                    
                                            }
                                            break;
                                        case 1: // ChaCha20-Poly1305
                                            if (!string.IsNullOrEmpty(psd))
                                            {
                                                if (new FileInfo(tempZipPath).Length >= 2L * 1024 * 1024 * 1024)
                                                {
                                                    // Use Stream Encryption
                                                    await Encrypt.ChaCha20_Poly1305_Encrypt_Stream(tempZipPath, new_file_path, psd);
                                                }
                                                else
                                                {
                                                    // Use File Encryption
                                                    await Encrypt.ChaCha20_Poly1305_Encrypt(tempZipPath, new_file_path, psd);
                                                }
                                            }
                                                //await Encrypt.ChaCha20_Poly1305_Encrypt(tempZipPath, new_file_path, psd);
                                            break;
                                        default:
                                            // Handle unknown algorithm index
                                            string error_title = loader.GetString("error");
                                            string error_content = loader.GetString("unknown_algorithm_index");
                                            await DispatcherQueue.EnqueueAsync(() =>
                                            {
                                                _ = ShowDialog(error_title, "OK", content: error_content);
                                            });
                                            break;
                                    }

                                    
                                    

                                    // Step 4: 删除中间文件和原始文件夹（如未设置保留）
                                    try
                                    {
                                        if (System.IO.File.Exists(tempZipPath))
                                            System.IO.File.Delete(tempZipPath);

                                        if (!keepCurrentFile && Directory.Exists(folder_path))
                                            Directory.Delete(folder_path, true);
                                    }
                                    catch (UnauthorizedAccessException uaEx)
                                    {
                                        string access_denied_tr = loader.GetString("access_denied");
                                        string access_denied_des_tr = loader.GetString("access_denied_des");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(access_denied_tr, "OK", content: $"{access_denied_des_tr}: {uaEx.Message}");
                                        });
                                    }
                                    catch (DirectoryNotFoundException dirEx)
                                    {
                                        string directory_not_found_tr = loader.GetString("directory_not_found");
                                        string directory_not_found_des_tr = loader.GetString("directory_not_found_des");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(directory_not_found_tr, "OK", content: $"{directory_not_found_des_tr}: {dirEx.Message}");
                                        });
                                    }
                                    catch (PathTooLongException pathEx)
                                    {
                                        string path_too_long_tr = loader.GetString("path_too_long");
                                        string path_too_long_des_tr = loader.GetString("path_too_long_des");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(path_too_long_tr, "OK", content: $"{path_too_long_des_tr}： {pathEx.Message}");
                                        });
                                    }
                                    catch (ArgumentException argEx)
                                    {
                                        string argument_error_tr = loader.GetString("arg_except");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(argument_error_tr, "OK", content: $"{argument_error_tr}: {argEx.Message}");
                                        });
                                    }
                                    catch (OutOfMemoryException memEx)
                                    {
                                        string memory_error_tr = loader.GetString("memory_except");
                                        string memory_error_des_tr = loader.GetString("memory_except_des"); 
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(memory_error_tr, "OK", content: $"{memory_error_des_tr}: {memEx.Message}");
                                        });
                                    }
                                    catch (IOException ioEx)
                                    {
                                        string io_error_tr = loader.GetString("IOError");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(io_error_tr, "OK", content: $"{io_error_tr}: {ioEx.Message}");
                                        });
                                    }
                                    catch (SecurityException secEx)
                                    {
                                        string security_error_tr = loader.GetString("security_except");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(security_error_tr, "OK", content: $"{security_error_tr}: {secEx.Message}");
                                        });
                                    }
                                    catch (Exception delEx)
                                    {
                                        string delete_error_tr = loader.GetString("cleaning_cache_error");
                                        string delete_error_des_tr = loader.GetString("cleaning_cache_error_descript");
                                        await DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            _ = ShowDialog(delete_error_tr, "OK", content: $"{delete_error_des_tr}: {delEx.Message}");
                                        });
                                    }

                                    // Step 5: UI 更新
                                    Interlocked.Increment(ref completed);
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        EncryptProgress.Value = (int)((completed / (float)total) * 100);
                                    });
                                }
                                
                                catch (UnauthorizedAccessException uaEx)
                                {
                                    // Handle UnauthorizedAccessException
                                    string access_denied_tr = loader.GetString("access_denied");
                                    string access_denied_des_tr = loader.GetString("access_denied_des");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(access_denied_tr, "OK", content: $"{access_denied_des_tr}: {uaEx.Message}");
                                    });
                                }
                                catch (PathTooLongException pathEx)
                                {
                                    // Handle PathTooLongException
                                    string path_too_long_tr = loader.GetString("path_too_long");
                                    string path_too_long_des_tr = loader.GetString("path_too_long_des");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(path_too_long_tr, "OK", content: $"{path_too_long_des_tr}： {pathEx.Message}");
                                    });
                                }
                                catch (ArgumentException argEx)
                                {
                                    // Handle ArgumentException
                                    string argument_error_tr = loader.GetString("arg_except");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(argument_error_tr, "OK", content: $"{argument_error_tr}: {argEx.Message}");
                                    });
                                }
                                catch (OutOfMemoryException memEx)
                                {
                                    // Handle OutOfMemoryException
                                    string memory_error_tr = loader.GetString("memory_except");
                                    string memory_error_des_tr = loader.GetString("memory_except_des");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(memory_error_tr, "OK", content: $"{memory_error_des_tr}: {memEx.Message}");
                                    });
                                }
                                catch (IOException ioEx)
                                {
                                    // Handle IOException
                                    string io_error_tr = loader.GetString("IOError");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(io_error_tr, "OK", content: $"{io_error_tr}: {ioEx.Message}");
                                    });
                                }
                                catch (SecurityException secEx)
                                {
                                    string security_error_tr = loader.GetString("security_except");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(security_error_tr, "OK", content: $"{security_error_tr}: {secEx.Message}");
                                    });
                                }
                                catch (Exception ex)
                                {
                                    string encryption_error_tr = loader.GetString("encrypt_failed");
                                    string encryption_error_des_tr = loader.GetString("encrypt_failed_descript");
                                    await DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        _ = ShowDialog(encryption_error_tr, "OK", content: $"{encryption_error_des_tr}: {ex.Message}");
                                    });
                                }
                                
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
            string encryption_complete_title = loader.GetString("EncryptionCompleteDialogTitle");
            string dialogOK = loader.GetString("DialogOK");
            string encryption_complete_content = loader.GetString("EncryptionCompleteDialogContent");
            string open_folder = loader.GetString("OpenFolder");

            ContentDialogResult result_from_dialog = await ShowDialog(encryption_complete_title, dialogOK, content: $"{total} {encryption_complete_content}\n{open_folder}");

            // if user clicks "OK" button
            if (result_from_dialog == ContentDialogResult.Primary)
            {
                // Open the folder where the encrypted files are saved
                string _targetPath = System.IO.Path.GetDirectoryName(new_file_path);
                var folder = await StorageFolder.GetFolderFromPathAsync(_targetPath);
                await Windows.System.Launcher.LaunchFolderAsync(folder); // Fix: Pass FolderLauncherOptions as the second argument
            }

            // Clear the input files 
            DataList.Clear();
            FileList.Clear();
            FolderList.Clear();

            EncryptProgress.Visibility = Visibility.Collapsed;
        }

        private async Task<string> EncryptFilesinFolder_Recursion(string folderPath, int algorithmIndex, string password, string targetPath = null)
        {
            try
            {
                // 获取原始文件夹名
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                string tempFolder = SettingManager.TempFilePath;

                // 1. 创建临时加密文件夹（用于存放加密后的 .enc 文件）
                //string tempEncryptedFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                string tempEncryptedFolderPath = Path.Combine(tempFolder, Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempEncryptedFolderPath);

                // 2. 获取所有文件（包括子目录）
                var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                foreach (var filePath in allFiles)
                {
                    // 3. 获取相对路径，保持目录结构
                    string relativePath = Path.GetRelativePath(folderPath, filePath);
                    string targetEncryptedFilePath = Path.Combine(tempEncryptedFolderPath, relativePath + ".enc");

                    // 4. 创建目标目录
                    Directory.CreateDirectory(Path.GetDirectoryName(targetEncryptedFilePath)!);

                    // 5. 执行加密
                    switch (algorithmIndex)
                    {
                        case 0:
                            // Check if the file is larger than 2GB
                            if (new FileInfo(filePath).Length >= 2L * 1024 * 1024 * 1024)
                            {
                                // Use Stream Encryption
                                await Encrypt.AES_GCM_Encrypt_Stream(filePath, targetEncryptedFilePath, password);
                            }
                            else
                            {
                                // Use File Encryption
                                await Encrypt.AES_GCM_Encrypt(filePath, targetEncryptedFilePath, password);
                            }
                            //await Encrypt.AES_GCM_Encrypt(filePath, targetEncryptedFilePath, password);
                            break;
                        case 1:
                            // Check if the file is larger than 2GB
                            if (new FileInfo(filePath).Length >= 2L * 1024 * 1024 * 1024)
                            {
                                // Use Stream Encryption
                                await Encrypt.ChaCha20_Poly1305_Encrypt_Stream(filePath, targetEncryptedFilePath, password);
                            }
                            else
                            {
                                // Use File Encryption
                                await Encrypt.ChaCha20_Poly1305_Encrypt(filePath, targetEncryptedFilePath, password);
                            }
                            //await Encrypt.ChaCha20_Poly1305_Encrypt(filePath, targetEncryptedFilePath, password);
                            break;
                        default:
                            await DispatcherQueue.EnqueueAsync(() =>
                            {
                                _ = ShowDialog("错误", "OK", content: "未知加密算法索引。");
                            });
                            return "";
                    }
                }

                // 6. 压缩临时加密文件夹为 zip
                //string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                string tempZipPath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + ".zip");
                ZipFile.CreateFromDirectory(tempEncryptedFolderPath, tempZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

                // 7. 判断保存路径
                string new_file_path;
                if (isSaveInDifferentPathVar && !string.IsNullOrEmpty(targetPath))
                {
                    new_file_path = Path.Combine(targetPath, folder.Name + ".encrec");
                }
                else
                {
                    new_file_path = folderPath + ".encrec";
                }

                // 8. 对 zip 文件加密为最终的 .encrec 文件
                switch (algorithmIndex)
                {
                    case 0:
                        // Check if the file is larger than 2GB
                        if (new FileInfo(tempZipPath).Length >= 2L * 1024 * 1024 * 1024)
                        {
                            // Use Stream Encryption
                            await Encrypt.AES_GCM_Encrypt_Stream(tempZipPath, new_file_path, password);
                        }
                        else
                        {
                            // Use File Encryption
                            await Encrypt.AES_GCM_Encrypt(tempZipPath, new_file_path, password);
                        }
                        //await Encrypt.AES_GCM_Encrypt(tempZipPath, new_file_path, password);
                        break;
                    case 1:
                        if (new FileInfo(tempZipPath).Length >= 2L * 1024 * 1024 * 1024)
                        {
                            // Use Stream Encryption
                            await Encrypt.ChaCha20_Poly1305_Encrypt_Stream(tempZipPath, new_file_path, password);
                        }
                        else
                        {
                            // Use File Encryption
                            await Encrypt.ChaCha20_Poly1305_Encrypt(tempZipPath, new_file_path, password);
                        }
                            //await Encrypt.ChaCha20_Poly1305_Encrypt(tempZipPath, new_file_path, password);
                        break;
                }

                // 9. 清理临时文件
                Directory.Delete(tempEncryptedFolderPath, true);
                System.IO.File.Delete(tempZipPath);

                // 10. 删除原始文件夹（如需）
                if (!keepCurrentFile && Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                return new_file_path;

            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    _ = ShowDialog("加密失败", "OK", content: $"文件夹递归加密失败：{ex.Message}");
                });
                return "";
            }
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
