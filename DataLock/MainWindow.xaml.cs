using DataLock.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public static readonly string[] navTagList = { "Home", "Encrypt", "Decrypt", "DynamicsLock", "Settings" };
        private int currentNavTag = 0;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void AppNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                string tag = args.SelectedItemContainer.Tag.ToString();
                int index = Array.IndexOf(navTagList, tag);
                dynamic effect;
                if (index > currentNavTag)
                {
                    effect = new EntranceNavigationTransitionInfo();
                    currentNavTag = index;
                }
                else if (index < currentNavTag)
                {
                    effect = new EntranceNavigationTransitionInfo();
                    currentNavTag = index;
                }
                else
                {
                    effect = new DrillInNavigationTransitionInfo();
                }

                switch (tag)
                {
                    case "Home":
                        contentFrame.Navigate(typeof(HomePage), null, effect);
                        AppNav.Header = "Home"; // TODO Will add translated text
                        break;

                    case "DynamicsLock":
                        contentFrame.Navigate(typeof(DynamicsLockPage), null, effect);
                        AppNav.Header = "Registered Files / Folders"; // TODO Will add translated text
                        break;

                    case "Encrypt":
                        contentFrame.Navigate(typeof(EncryptPage), null, effect);
                        AppNav.Header = "Encrypt"; // TODO Will add translated text
                        break;

                    case "Decrypt":
                        contentFrame.Navigate(typeof(DecryptPage), null, effect);
                        AppNav.Header = "Decrypt"; // TODO Will add translated text
                        break;

                    case "Settings":
                        contentFrame.Navigate(typeof(SettingPage), null, effect);
                        AppNav.Header = "Setting"; // TODO Will add translated text
                        break;

                    default:

                        break;
                }
            }
        }
    }
}
