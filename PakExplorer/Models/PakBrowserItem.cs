using PakLib;
using System.IO;
using Windows.UI.Xaml.Media;

namespace PakExplorer.Models
{
    public enum PakFileBrowserItemType
    {
        Folder,
        File
    }

    public abstract class PakFileBrowserItem
    {
        public string DisplayName { get; set; }

        public abstract PakFileBrowserItemType ItemType { get; }

        public ImageSource Icon { get; set; }

        public bool IsFile { get => ItemType == PakFileBrowserItemType.File; }
    }

    public class PakFileBrowserFileItem : PakFileBrowserItem
    {
        public override PakFileBrowserItemType ItemType => PakFileBrowserItemType.File;

        public PakEntryMetadata Metadata { get; set; }

        public string Extension { get => Path.GetExtension(DisplayName); }
    }

    public class PakFileBrowserFolderItem : PakFileBrowserItem
    {
        public override PakFileBrowserItemType ItemType => PakFileBrowserItemType.Folder;

        public string FolderPath { get; set; }
    }
}
