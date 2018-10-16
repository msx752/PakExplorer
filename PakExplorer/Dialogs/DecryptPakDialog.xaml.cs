using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PakExplorer.Dialogs
{
    public sealed partial class DecryptPakDialog : ContentDialog
    {
        public DecryptPakDialog(bool isSubsequentAttempt = false)
        {
            InitializeComponent();

            if (isSubsequentAttempt)
            {
                this.initialMessage.Visibility = Visibility.Collapsed;
                this.subsequentMessage.Visibility = Visibility.Visible;
            }
        }

        public string EncryptionKey { get; private set; }

        void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            EncryptionKey = this.encryptionKey.Text;
        }

        void EncryptionKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(this.encryptionKey.Text);
        }
    }
}
