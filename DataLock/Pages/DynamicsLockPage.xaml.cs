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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataLock.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DynamicsLockPage : Page
    {

        private ObservableCollection<DataLock.Modules.DynamicLock> DynamicLockFileLists = new ObservableCollection<DynamicLock>();

        public DynamicsLockPage()
        {
            this.InitializeComponent();
            this.add_example_files();
        }

        private void add_example_files()
        {
            DynamicLock newFolder = new DynamicLock("Test Folder", 0, "E:\\Apps\\BuZhen");
            DynamicLock newFolder1 = new DynamicLock("Test Folder1", 1, "E:\\Apps\\BuZhen");
            DynamicLock newFolder2 = new DynamicLock("Test Folder2", 2, "E:\\Apps\\BuZhen");
            DynamicLock newFile = new DynamicLock("Test File", 1, "E:\\WindowsFolder\\Downloads\\PyQt-Fluent-Widgets-Gallery\\PyQt-Fluent-Widgets-Gallery\\gallery.exe");

            DynamicLockFileLists.Add(newFolder);
            DynamicLockFileLists.Add(newFolder1);
            DynamicLockFileLists.Add(newFolder2);
            DynamicLockFileLists.Add(newFile);

            DynamicFileViewer.ItemsSource = DynamicLockFileLists;
        }

        private void DynamicFileViewer_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {

        }

        private void DynamicFileViewer_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
        {

        }
    }
}
