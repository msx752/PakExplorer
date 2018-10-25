using System;
using Windows.UI.Xaml.Controls;

namespace PakExplorer.Dialogs
{
    public enum FilePadding
    {
        NullBytes,
        Spaces
    }

    public sealed partial class SwapPaddingDialog : ContentDialog
    {
        public SwapPaddingDialog()
        {
            this.InitializeComponent();

            foreach (string name in Enum.GetNames(typeof(FilePadding)))
            {
                this.paddingTypeComboBox.Items.Add(name);
            }
        }

        public FilePadding FilePadding { get; private set; }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FilePadding = (FilePadding)Enum.Parse(typeof(FilePadding), this.paddingTypeComboBox.SelectedItem.ToString());
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}