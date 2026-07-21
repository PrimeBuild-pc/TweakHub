using System.Windows;
using TweakHub.Localization;

namespace TweakHub.Views
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string? title = null)
        {
            InitializeComponent();
            TitleText.Text = title ?? L.Get("UI:Processing");
            Owner = Application.Current.MainWindow;
        }

        public void UpdateProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percentage;
                StatusText.Text = L.Format("UI:PercentComplete", percentage);
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

    }
}
