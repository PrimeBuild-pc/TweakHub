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
            ChoicePanel.Children.Add(new CheckBox { Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap }, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 8) });
        ChoicePanel.Visibility = Visibility.Visible;
    }

    private Func<int, int, bool>? _hiddenByFilter;

    /// <summary>
    /// A "hide ... in the last N days" filter above the choices: while it is on, choices for which
    /// <paramref name="hidden"/>(index, days) is true are collapsed and never returned as checked.
    /// </summary>
    public void SetChoiceFilter(string label, string suffix, int days, Func<int, int, bool> hidden)
    {
        _hiddenByFilter = hidden;
        FilterBox.Content = label;
        FilterSuffixText.Text = suffix;
        AutomationProperties.SetName(FilterDaysBox, $"{label} {suffix}");
        FilterDaysBox.Text = days.ToString();
        FilterPanel.Visibility = Visibility.Visible;
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (_hiddenByFilter is null) return;
        var days = FilterBox.IsChecked == true && int.TryParse(FilterDaysBox.Text, out var value) && value > 0 ? value : 0;
        var boxes = ChoicePanel.Children.OfType<CheckBox>().ToList();
        for (var index = 0; index < boxes.Count; index++)
            boxes[index].Visibility = days > 0 && _hiddenByFilter(index, days) ? Visibility.Collapsed : Visibility.Visible;
    }

    // Read from the rows themselves when the user confirms, so what is removed is exactly what is shown ticked.
    public IReadOnlyList<int> CheckedChoices =>
        [.. ChoicePanel.Children.OfType<CheckBox>().Select((box, index) => (box, index))
            .Where(item => item.box.IsChecked == true && item.box.Visibility == Visibility.Visible).Select(item => item.index)];

    // A click that lands after the dialog closed (queued double click, Enter, UI Automation) would throw and crash the app.
    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible) DialogResult = true;
    }

    private void Body_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
