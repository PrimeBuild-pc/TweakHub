using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace TweakHub.Views.Dialogs;

public enum DialogKind { Info, Warning, Error, Shield }

/// <summary>Windows 11 style message dialog: icon, title, message, then a footer band with equal-width buttons.</summary>
public partial class AppDialogWindow : Window
{
    public AppDialogWindow(string title, string message, string primaryText, string? secondaryText, DialogKind kind, string? input = null)
    {
        InitializeComponent();
        Title = title;
        AutomationProperties.SetName(this, title);
        TitleText.Text = title;
        MessageText.Text = message;
        PrimaryButton.Content = primaryText;
        (IconText.Text, var brush) = kind switch
        {
            DialogKind.Warning => ("", "WarningBrush"),
            DialogKind.Error => ("", "DangerBrush"),
            DialogKind.Shield => ("", "AccentBrush"),
            _ => ("", "AccentBrush")
        };
        IconText.SetResourceReference(TextBlock.ForegroundProperty, brush);

        if (secondaryText is null)
        {
            // A lone button sits where Windows puts Close: the right half of the footer.
            SecondaryButton.Visibility = Visibility.Collapsed;
            Grid.SetColumn(PrimaryButton, 2);
            PrimaryButton.IsCancel = true;
        }
        else SecondaryButton.Content = secondaryText;
        if (input is not null)
        {
            InputBox.Text = input;
            InputBox.Visibility = Visibility.Visible;
            AutomationProperties.SetName(InputBox, message);
        }
        Loaded += (_, _) =>
        {
            if (input is null) PrimaryButton.Focus();
            else
            {
                InputBox.Focus();
                InputBox.SelectAll();
            }
        };
    }

    public string InputText => InputBox.Text.Trim();

    /// <summary>Shows one check box per choice below the message.</summary>
    public void SetChoices(IEnumerable<(string Label, bool IsChecked)> choices)
    {
        foreach (var (label, isChecked) in choices)
            ChoicePanel.Children.Add(new CheckBox { Content = label, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 8) });
        ChoicePanel.Visibility = Visibility.Visible;
    }

    public IReadOnlyList<int> CheckedChoices =>
        [.. ChoicePanel.Children.OfType<CheckBox>().Select((box, index) => (box, index)).Where(item => item.box.IsChecked == true).Select(item => item.index)];

    private void Primary_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Body_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
