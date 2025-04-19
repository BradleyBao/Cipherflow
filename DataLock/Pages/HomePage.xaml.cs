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
        }

        public void InitUI()
        {
            // Set up Hello {username} text
            // Get the username from Windows credentials
            string username = WindowsIdentity.GetCurrent().Name;
            WelcomeMsg.Text = "Hello " + username.Split('\\')[1] + "!";
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
    }
}
