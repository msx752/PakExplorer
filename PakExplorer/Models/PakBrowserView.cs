using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PakExplorer.Models
{
    public sealed class PakBrowserView : ObservableObject
    {
        public static ImageSource DefaultFolderIcon { get; } = new BitmapImage(new Uri("ms-appx:///Assets/BrowserIcons/folder.png"));
        public static ImageSource DefaultFileIcon { get; } = new BitmapImage(new Uri("ms-appx:///Assets/BrowserIcons/file.png"));
        public static ImageSource EncryptedFileIcon { get; } = new BitmapImage(new Uri("ms-appx:///Assets/BrowserIcons/encryptedfile.png"));

        private ObservableCollection<PakFileBrowserItem> _items;

        public PakBrowserView(VirtualFolder folder)
        {
            VirtualFolder = folder;
            Items = new ObservableCollection<PakFileBrowserItem>();

            foreach (var subfolder in folder.Subfolders)
            {
                Items.Add(new PakFileBrowserFolderItem
                {
                    DisplayName = subfolder.Key,
                    FolderPath = subfolder.Value.Path,
                    Icon = DefaultFolderIcon
                });
            }

            foreach (var file in folder.Files)
            {
                Items.Add(new PakFileBrowserFileItem
                {
                    DisplayName = file.Key,
                    Metadata = file.Value,
                    Icon = file.Value.IsEncrypted ? EncryptedFileIcon : DefaultFileIcon
                });
            }
        }

        public VirtualFolder VirtualFolder { get; }

        public ObservableCollection<PakFileBrowserItem> Items
        {
            get { return _items; }
            set { SetValue(ref _items, value); }
        }
    }
}
