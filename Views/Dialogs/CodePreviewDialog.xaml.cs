using System.Windows;
using System.Windows.Controls;

namespace TweakHub.Views.Dialogs
{
    public class CodePreviewDialog : Window
    {
        private readonly TextBox _contentBox;

        public CodePreviewDialog(string title, string content)
        {
            Title = title;
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0,0,0,12)
            };
            Grid.SetRow(titleBlock, 0);
            root.Children.Add(titleBlock);

            var border = new Border
            {
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(border, 1);
            _contentBox = new TextBox
            {
                Text = content,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent
            };
            var scroll = new ScrollViewer { Content = _contentBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            border.Child = scroll;
            root.Children.Add(border);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0,12,0,0)
            };
            Grid.SetRow(buttonsPanel, 2);

            var copyBtn = new Button { Content = "Copy", Margin = new Thickness(0,0,8,0), Padding = new Thickness(12,6,12,6) };
            copyBtn.Click += (s,e) =>
            {
                try { Clipboard.SetText(_contentBox.Text ?? string.Empty); } catch { }
            };
            var closeBtn = new Button { Content = "Close", Padding = new Thickness(12,6,12,6) };
            closeBtn.Click += (s,e) => { DialogResult = true; Close(); };
            buttonsPanel.Children.Add(copyBtn);
            buttonsPanel.Children.Add(closeBtn);
            root.Children.Add(buttonsPanel);

            Content = root;
        }
    }
}