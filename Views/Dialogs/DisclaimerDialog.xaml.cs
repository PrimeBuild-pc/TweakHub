using System.Windows;

namespace TweakHub.Views.Dialogs
{
    public partial class DisclaimerDialog : Window
    {
        public DisclaimerDialog() => InitializeComponent();

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

