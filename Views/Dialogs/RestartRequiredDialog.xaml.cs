using System.Windows;

namespace TweakHub.Views.Dialogs
{
    public partial class RestartRequiredDialog : Window
    {
        public RestartRequiredDialog(string? message = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageText.Text = message;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
