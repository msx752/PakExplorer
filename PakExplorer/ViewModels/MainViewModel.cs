using GalaSoft.MvvmLight.Command;
using PakExplorer.Dialogs;
using PakExplorer.Models;
using PakLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PakExplorer.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private string _statusText;
        private Pak _currentPak;
        private PakEntryTable _entries;
        private PakBrowserView _currentView;
        private VirtualFileSystem _graph;
        private Stack<PakBrowserView> _navigationStack;
        private string _navigationPath;
        private ObservableCollection<PakFileBrowserFileItem> _selectedItems;

        public MainViewModel()
        {
            _navigationStack = new Stack<PakBrowserView>();
            _navigationPath = "";

            StatusText = "No pak loaded";
            CurrentView = new PakBrowserView(new VirtualFolder(""));
            SelectedItems = new ObservableCollection<PakFileBrowserFileItem>();
            SelectedItems.CollectionChanged += delegate { OnPropertyChanged(nameof(HasSelectedItems)); };

            OpenItemCommand = new RelayCommand<PakFileBrowserItem>(OpenItem);
            NavigateBackCommand = new RelayCommand(NavigateBack);
            NavigateToParentCommand = new RelayCommand(NavigateParent);
            ExportFileListCommand = new RelayCommand(async () => await ExportFileListAsync(), () => VirtualFileSystem != null);
            ExportFileCommand = new RelayCommand(async () => await ExportFileAsync());
        }

        public ICommand OpenItemCommand { get; }

        public ICommand NavigateBackCommand { get; }

        public ICommand NavigateToParentCommand { get; }

        public ICommand ExportFileListCommand { get; }

        public ICommand ExportFileCommand { get; }

        public string EncryptionKey { get; set; }

        public bool CanNavigateToParent
        {
            get { return CurrentView != null && VirtualFileSystem != null && CurrentView.VirtualFolder != VirtualFileSystem.Root; }
        }

        public bool CanNavigateBack
        {
            get { return _navigationStack.Count > 1; }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { SetValue(ref _statusText, value); }
        }

        public Pak CurrentPak
        {
            get { return _currentPak; }
            set
            {
                SetValue(ref _currentPak, value);
                OnPropertyChanged(nameof(IsPakLoaded));
            }
        }

        public bool IsPakLoaded { get { return CurrentPak != null; } }

        public PakEntryTable Entries
        {
            get { return _entries; }
            set
            {
                SetValue(ref _entries, value);
                OnPropertyChanged(nameof(TotalFileCount));
                VirtualFileSystem = VirtualFileSystem.CreateGraph(value);
                CurrentView = new PakBrowserView(VirtualFileSystem.Root);
                _navigationStack.Push(CurrentView);
            }
        }

        public int TotalFileCount { get { return Entries.Count; } }

        public PakBrowserView CurrentView
        {
            get { return _currentView; }
            set
            {
                SetValue(ref _currentView, value, suppressEqualityCheck: true);
                NavigationPath = value.VirtualFolder.Path + '/';
                OnPropertyChanged(nameof(CanNavigateToParent));
                OnPropertyChanged(nameof(CanNavigateBack));
            }
        }

        public VirtualFileSystem VirtualFileSystem
        {
            get { return _graph; }
            set
            {
                SetValue(ref _graph, value);
                OnPropertyChanged(nameof(ExportFileListCommand));
            }
        }

        public string NavigationPath
        {
            get { return _navigationPath; }
            set { SetValue(ref _navigationPath, value); }
        }

        public ObservableCollection<PakFileBrowserFileItem> SelectedItems
        {
            get { return _selectedItems; }
            private set
            {
                SetValue(ref _selectedItems, value, suppressEqualityCheck: true);
                OnPropertyChanged(nameof(HasSelectedItems));
            }
        }

        public bool HasSelectedItems
        {
            get { return SelectedItems?.Count > 0; }
        }

        public async Task OpenPakAsync(StorageFile file)
        {
            try
            {
                var fileStream = (await file.OpenReadAsync()).AsStreamForRead();
                Pak pak = await Pak.OpenAsync(fileStream);

                if (pak.IsIndexEncrypted)
                {
                    // If the pak is encrypted, ask for the key and attempt to decrypt it.
                    bool decryptSuccessful = false;
                    bool isSubsequentAttempt = false;
                    while (!decryptSuccessful)
                    {
                        var decryptDialog = new DecryptPakDialog(isSubsequentAttempt);
                        if (await decryptDialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            try
                            {
                                Entries = await pak.GetEntriesAsync(decryptDialog.EncryptionKey);
                                decryptSuccessful = true;
                                EncryptionKey = decryptDialog.EncryptionKey;
                            }
                            catch (IncorrectEncryptionKeyException)
                            {
                                isSubsequentAttempt = true;
                            }
                        }
                        else
                        {
                            return; // No encryption key provided, we're not opening this file.
                        }
                    }
                }
                else
                {
                    Entries = await pak.GetEntriesAsync();
                }

                CurrentPak = pak;
                ApplicationView.GetForCurrentView().Title = file.Path;
                StatusText = $"{Entries.Count} files";
            }
            catch (MagicNumberMismatchException)
            {
                await (new MessageDialog("This isn't a pak file.")).ShowAsync();
            }
            catch (UnsupportedVersionException)
            {
                await (new MessageDialog("This pak is based on a version that isn't supported at this time.")).ShowAsync();
            }
        }

        private void OpenItem(PakFileBrowserItem item)
        {
            if (item is PakFileBrowserFolderItem)
            {
                var folder = (item as PakFileBrowserFolderItem);
                CurrentView = new PakBrowserView(VirtualFileSystem.GetFolder(folder.FolderPath));
                _navigationStack.Push(CurrentView);
                OnPropertyChanged(nameof(CanNavigateBack));
            }
        }

        private void NavigateBack()
        {
            if (_navigationStack.Count == 0)
                return;

            _navigationStack.Pop();
            var previousView = _navigationStack.Peek();
            CurrentView = previousView;
        }

        private void NavigateParent()
        {
            if (CurrentView.VirtualFolder == VirtualFileSystem.Root)
                return;

            string parentPath = CurrentView.VirtualFolder.Path.Substring(0, CurrentView.VirtualFolder.Path.LastIndexOf('/'));
            CurrentView = new PakBrowserView(VirtualFileSystem.GetFolder(parentPath));
            _navigationStack.Push(CurrentView);
        }

        private async Task ExportFileListAsync()
        {
            var outputList = new FileSavePicker { SuggestedFileName = "filelist" };
            outputList.FileTypeChoices.Add("Text Documents", new List<string> { ".txt" });

            var sortedList = new SortedSet<string>();
            foreach (PakEntryMetadata entry in Entries)
            {
                sortedList.Add(entry.FileName);
            }

            var stringBuilder = new StringBuilder();
            foreach (string filename in sortedList)
            {
                stringBuilder.AppendLine(filename);
            }

            var file = await outputList.PickSaveFileAsync();
            if (file != null)
            {
                using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                    await stream.WriteAsync(buffer.AsBuffer());
                }

                var dialog = new MessageDialog($"Saved file list to {file.Path}");
                await dialog.ShowAsync();
            }
        }

        private async Task ExportFileAsync()
        {
            PakFileBrowserFileItem file = SelectedItems.First();

            var picker = new FileSavePicker { SuggestedFileName = file.DisplayName };
            picker.FileTypeChoices.Add($"{file.Extension} files", new List<string> { file.Extension });

            var saveFile = await picker.PickSaveFileAsync();
            if (saveFile != null)
            {
                Stream input;
                if (EncryptionKey == null)
                {
                    input = await CurrentPak.GetEntryStreamAsync(file.Metadata);
                }
                else
                {
                    input = await CurrentPak.GetEntryStreamAsync(file.Metadata, EncryptionKey);
                }

                using (input)
                using (var output = await saveFile.OpenStreamForWriteAsync())
                {
                    await input.CopyToAsync(output);
                }

                var flyout = new Flyout()
                {
                    Content = new TextBlock { Text = $"File saved!" },
                    Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
                };
                flyout.ShowAt((FrameworkElement)Window.Current.Content);
            }
        }
    }
}
