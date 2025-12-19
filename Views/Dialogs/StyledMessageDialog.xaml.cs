using System;
using System.Windows;

namespace TweakHub.Views.Dialogs
{
    public partial class StyledMessageDialog : Window
    {
        private StyledMessageDialog(string title, string message, string primaryButtonText, string? secondaryButtonText)
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;

            PrimaryButtonText.Text = primaryButtonText;

            if (!string.IsNullOrWhiteSpace(secondaryButtonText))
            {
                SecondaryButton.Content = secondaryButtonText;
                SecondaryButton.Visibility = Visibility.Visible;
            }
        }

        public static void ShowOk(Window? owner, string title, string message)
        {
            var dialog = new StyledMessageDialog(title, message, "OK", secondaryButtonText: null)
            {
                Owner = owner ?? Application.Current.MainWindow
            };

            dialog.ShowDialog();
        }

        public static bool ShowYesNo(Window? owner, string title, string message)
        {
            var dialog = new StyledMessageDialog(title, message, primaryButtonText: "Yes", secondaryButtonText: "No")
            {
                Owner = owner ?? Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DialogResult == null)
            {
                DialogResult = false;
            }

            base.OnClosed(e);
        }
    }
}
