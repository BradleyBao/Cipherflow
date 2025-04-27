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
using System.Security.Principal;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage;
using DataLock.Functions;
using DataLock.Modules;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            this.InitUI();
            this.Loaded += HomePage_Loaded;
            
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            Auth_user();
        }

        public async void Auth_user()
        {
            // If password is enabled, show dialog and lock down the page
            if (SettingManager.Password && !SettingManager.Unlocked)
            {
                string username = WindowsIdentity.GetCurrent().Name;
                var loader = new ResourceLoader();
                string password_msg = loader.GetString("SetupAppPassword");
                string dialog_ok = loader.GetString("DialogOK");
                string dialog_cancel = loader.GetString("DialogCancel");

                // 创建一个 ContentDialog
                ContentDialog passwordDialog = new ContentDialog
                {
                    Title = password_msg,
                    PrimaryButtonText = dialog_ok,
                    CloseButtonText = dialog_cancel
                };

                // 创建 PasswordBox 并添加到对话框的内容中
                PasswordBox passwordBox = new PasswordBox
                {
                    PlaceholderText = password_msg,
                    Width = 300
                };
                passwordDialog.Content = passwordBox;
                passwordDialog.XamlRoot = this.XamlRoot;

                // 显示对话框并等待用户操作
                var result = await passwordDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // 用户点击了 OK 按钮
                    string password = passwordBox.Password;

                    if (!string.IsNullOrEmpty(password))
                    {
                        var vault = new Windows.Security.Credentials.PasswordVault();
                        var credential = vault.Retrieve("Cipherflow", username);
                        credential.RetrievePassword(); // This will get the plain password
                        string storedPassword = credential.Password;
                        if (password == storedPassword)
                        {
                            SettingManager.Unlocked = true;
                        }
                        else
                        {
                            // 密码错误，提示用户
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = "Incorrect password. Software Lock Down",
                                CloseButtonText = "OK"
                            };
                            errorDialog.XamlRoot = this.XamlRoot;
                            await errorDialog.ShowAsync();
                            // Lock Down Page
                            App.m_window.LockDown();
                        }
                    }
                    else
                    {
                        // 提示用户密码不能为空
                        App.m_window.LockDown();
                    }
                } else
                {
                    // Lock Down Page 
                    // Disable Navbar 
                    App.m_window.LockDown();
                }
            }
        }

        public void InitUI()
        {
            // Set up Hello {username} text
            // Get the username from Windows credentials
            string username = System.Net.Dns.GetHostName();
            var loader = new ResourceLoader();
            string welcomeMsg = loader.GetString("WelcomeText");
            WelcomeMsg.Text = welcomeMsg + " " + username + "!";

            // Security Setting 
            int total_security = 2;
            int security_count = 0;
            if (SettingManager.Password)
            {
                security_count++;
            }
            if (SettingManager.MFA)
            {
                security_count++;
            }
            int bar_width = (int)((security_count / (float)total_security) * 100);
            SecuritySettingBar.Value = bar_width;

            // Set Text 
            switch (security_count)
            {
                case 0:
                    SecuritySettingTip.Text = loader.GetString("SettingSevere");
                    SecuritySettingBar.Value = 10;
                    SecuritySettingBar.ShowError = true;
                    break;
                case 1:
                    SecuritySettingTip.Text = loader.GetString("SettingModerate");
                    SecuritySettingBar.ShowPaused = true;
                    break;
                case 2:
                    SecuritySettingTip.Text = loader.GetString("SettingSafe");
                    break;
            }

            // Get the version 
            Version.Text = GetAppVersion();
        }

        private string GetAppVersion()
        {
            // 获取应用程序的Assembly版本信息
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private void cryptography_encryptPage_Click(object sender, RoutedEventArgs e)
        {
            // Go to Encrypt page
            App.m_window.GoToPage("Encrypt", new DrillInNavigationTransitionInfo());
        }

        private void cryptography_decryptPage_Click(object sender, RoutedEventArgs e)
        {
            App.m_window.GoToPage("Decrypt", new DrillInNavigationTransitionInfo());
        }

        private void configureSetting_Click(object sender, RoutedEventArgs e)
        {
            App.m_window.GoToPage("Settings", new DrillInNavigationTransitionInfo());
            configureSetting.IsEnabled = false;
        }
    }
}
