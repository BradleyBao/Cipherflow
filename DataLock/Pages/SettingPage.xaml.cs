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
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private int hello_result = -1; // 0: Available,
                                       // 1: DeviceNotPresent,
                                       // 2: DeviceBusy,
                                       // 3: DisabledByPolicy,
                                       // 4: NotConfiguredForUser,
                                       // -1: NotAvailable
        private bool hello_auth_result = false;
        public SettingPage()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
            
        }

        private async Task InitializeAsync()
        {
            hello_result = await CheckBiometricSupport();
            InitUI();
        }

        private void InitUI()
        {
            // If biometric authentication is available or is busy, show the button
            switch (hello_result)
            {
                case 0:
                    // Biometric authentication is available
                    windows_hello_setting.IsEnabled = true;
                    break;
                case 1:
                    // No biometric device is present
                    windows_hello_setting.IsEnabled = false;
                    windows_hello_setting.Description = "Not Available: No authentication device is present";
                    break;
                case 2:
                    // Biometric device is busy
                    windows_hello_setting.IsEnabled = true;
                    break;
                case 3:
                    // Biometric authentication is disabled by policy
                    windows_hello_setting.IsEnabled = false;
                    windows_hello_setting.Description = "Not Available: Windows Hello is disabled by policy";
                    break;
                case 4:
                    // Biometric authentication is not configured for the current user
                    windows_hello_setting.IsEnabled = false;
                    windows_hello_setting.Description = "Not Configured: Please go to setting to setup windows hello.";
                    break;
                default:
                    // Biometric authentication is not available
                    windows_hello_setting.IsEnabled = false;
                    windows_hello_setting.Description = "Not Available: Windows Hello is not available";
                    break;
            }
        }

        private async Task<int> CheckBiometricSupport()
        {
            // Check if the device supports biometric authentication
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();

            switch (availability)
            {
                case UserConsentVerifierAvailability.Available:
                    // Biometric authentication is supported and enabled
                    return 0;

                case UserConsentVerifierAvailability.DeviceNotPresent:
                    // No biometric device is present
                    return 1;

                case UserConsentVerifierAvailability.DeviceBusy:
                    // Biometric device is busy
                    return 2;

                case UserConsentVerifierAvailability.DisabledByPolicy:
                    // Biometric authentication is disabled by policy
                    return 3;

                case UserConsentVerifierAvailability.NotConfiguredForUser:
                    // Biometric authentication is not configured for the current user
                    return 4;

                default:
                    // Biometric authentication is not available
                    return -1;
            }
        }

        private async void SetupWindowsHello_Click(object sender, RoutedEventArgs e)
        {
            // Perform Windows Hello Authentication
            // Check if the device supports biometric authentication
            if (hello_result == 0)
            {
                // Show the Windows Hello authentication dialog
                var result = await UserConsentVerifier.RequestVerificationAsync("Please verify your identity using Windows Hello");
                // Check the result of the authentication
                if (result == UserConsentVerificationResult.Verified)
                {
                    // Authentication was successful
                    windows_hello_setting.HeaderIcon = new SymbolIcon(Symbol.Accept);
                    windows_hello_setting.Description = "Windows Hello is set up successfully.";
                    hello_auth_result = true;
                }
                else
                {
                    // Authentication failed
                    windows_hello_setting.HeaderIcon = new SymbolIcon(Symbol.Cancel);
                    windows_hello_setting.Description = "Windows Hello authentication failed.";
                    hello_auth_result = false;
                }
            }
            else
            {
                // Show an error message if biometric authentication is not available
                windows_hello_setting.Description = "Windows Hello is not available.";
                hello_auth_result = false;
            }
        }
    }
}
