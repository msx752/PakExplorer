using PakExplorer.Models;
using PakExplorer.ViewModels;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace PakExplorer
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            DataContext = ViewModel = new MainViewModel();

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            var accentColor = (Color)App.Current.Resources["AccentColor"];
            titleBar.ForegroundColor = Colors.White;
            titleBar.BackgroundColor = accentColor;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonBackgroundColor = accentColor;
            titleBar.InactiveForegroundColor = Colors.White;
            titleBar.InactiveBackgroundColor = accentColor;
            titleBar.ButtonInactiveForegroundColor = Colors.White;
            titleBar.ButtonInactiveBackgroundColor = accentColor;
        }

        public MainViewModel ViewModel { get; }

        async void OpenPackage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pak");
            picker.ViewMode = PickerViewMode.List;

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ViewModel.OpenPakAsync(file);
            }
        }

        void PakBrowser_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (PakFileBrowserItem)e.ClickedItem;
            if (!item.IsFile)
            {
                ViewModel.OpenItemCommand.Execute(item);
            }
        }

        void PakBrowser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedItems.Clear();
            foreach (PakFileBrowserItem item in e.AddedItems)
            {
                if (item is PakFileBrowserFileItem)
                {
                    var file = item as PakFileBrowserFileItem;
                    ViewModel.SelectedItems.Add(file);
                }
            }

            if (ViewModel.HasSelectedItems)
            {
                this.pivot.SelectedIndex = 2; // File Actions
            }
        }

        void CreditsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var button = (Control)sender;
            FlyoutBase.ShowAttachedFlyout(button);
        }

        void FilePropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            var metadata = ViewModel.SelectedItems.First().Metadata;
            this.propertiesWindow.DataContext = metadata;
            FlyoutBase.ShowAttachedFlyout(this.propertiesButton);
        }
    }
}