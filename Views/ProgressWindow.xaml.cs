using System.Windows;

namespace TweakHub.Views
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string title = "Processing...")
        {
            InitializeComponent();
            TitleText.Text = title;
            Owner = Application.Current.MainWindow;
        }

        public void UpdateProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percentage;
                StatusText.Text = $"{percentage:F0}% complete";
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        public void UpdateTitle(string title)
        {
            Dispatcher.Invoke(() =>
            {
                TitleText.Text = title;
            });
        }
    }
}
