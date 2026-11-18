using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TweakHub.Localization;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

/// <summary>
/// Lists ghost devices grouped by type, with the product name, last use and instance id, and a filter that hides
/// recently used ones. What is removed is read from the rows themselves when the user confirms.
/// </summary>
public partial class GhostDevicesDialog : Window
{
    private readonly IReadOnlyList<PnpDeviceInfo> _ghosts;
    private readonly DateTime _now;
    private readonly List<CheckBox> _rows = [];
    private int _days;
    private readonly List<(TextBlock Header, List<CheckBox> Rows)> _groups = [];

    /// <param name="ghosts">Already sorted for display; <see cref="CheckedRows"/> returns indices into it.</param>
    /// <param name="all">Every device, to find the product name a ghost's USB parent reported.</param>
    public GhostDevicesDialog(IReadOnlyList<PnpDeviceInfo> ghosts, IEnumerable<PnpDeviceInfo> all, DateTime utcNow, int days = 30)
    {
        InitializeComponent();
        AutomationProperties.SetName(this, Title);
        FitTo(SystemParameters.WorkArea);
        _ghosts = ghosts;
        _now = utcNow;
        MessageText.Text = L.Format("Devices:RemoveGhostsMessage", ghosts.Count);
        AutomationProperties.SetName(FilterDaysBox, $"{L.Get("Devices:GhostHideRecent")} {L.Get("Devices:GhostHideRecentDays")}");

        var allDevices = DeviceService.ById(all);
        string? group = null;
        for (var index = 0; index < ghosts.Count; index++)
        {
            var device = ghosts[index];
            if (GroupOf(device) != group)
            {
                group = GroupOf(device);
                var header = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, _groups.Count == 0 ? 4 : 14, 0, 4) };
                RowsPanel.Children.Add(header);
                _groups.Add((header, []));
            }
            var row = CreateRow(device, DeviceService.ProductName(device, allDevices));
            RowsPanel.Children.Add(row.Parent as UIElement);
            _rows.Add(row);
            _groups[^1].Rows.Add(row);
        }
        _days = days;
        FilterDaysBox.Text = days.ToString();
        ApplyFilter();
    }

    /// <summary>Keeps the dialog inside the work area (in DIPs): 1366 px at 200% leaves about 683 x 350.</summary>
    internal void FitTo(Rect workArea)
    {
        Width = Math.Min(860, Math.Max(320, workArea.Width - 24));
        MaxHeight = Math.Max(320, workArea.Height - 24);
    }

    internal static string GroupOf(PnpDeviceInfo device) => device.TypeName.Length > 0 ? device.TypeName : device.Class;

    /// <summary>Indices of the rows ticked and shown right now: a row hidden by the filter is never removed.</summary>
    public IReadOnlyList<int> CheckedRows =>
        [.. _rows.Select((box, index) => (box, index)).Where(item => item.box.IsChecked == true && item.box.Visibility == Visibility.Visible).Select(item => item.index)];

    /// <summary>The row's check box; its parent is the row (check box and names, then the last-use column).</summary>
    private CheckBox CreateRow(PnpDeviceInfo device, string product)
    {
        var title = product.Length > 0 ? product : device.Name.Length > 0 ? device.Name : L.Get("Devices:GhostUnnamed");
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = DateColumnWidth });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = DateColumnWidth });

        var names = new StackPanel();
        names.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        // The Windows name ("HID Keyboard Device") only when it adds something to the product name, then the manufacturer.
        var details = string.Join(" · ", new[] { device.Name, device.Manufacturer }
            .Where(part => part.Length > 0 && !part.Equals(title, StringComparison.CurrentCultureIgnoreCase)).Distinct(StringComparer.CurrentCultureIgnoreCase));
        if (details.Length > 0)
            names.Children.Add(Secondary(new TextBlock { Text = details, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = details }));
        names.Children.Add(Secondary(new TextBlock
        {
            Text = device.Id, FontSize = 11, FontFamily = new FontFamily("Cascadia Mono, Consolas"), Opacity = 0.8,
            TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = device.Id, Margin = new Thickness(0, 2, 0, 0)
        }));
        var lastUsed = Secondary(new TextBlock
        {
            Text = RelativeDate(device.LastUsed, _now), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0),
            ToolTip = device.LastUsed is { } used ? used.ToLocalTime().ToString("f", L.Culture) : L.Get("Devices:GhostLastUsedUnknownHint")
        });
        Grid.SetColumn(lastUsed, 2);
        grid.Children.Add(lastUsed);
        var installed = Secondary(new TextBlock
        {
            Text = device.InstalledOn is { } on ? on.ToLocalTime().ToString("d", L.Culture) : L.Get("Devices:GhostLastUsedUnknown"),
            FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0),
            ToolTip = device.InstalledOn?.ToLocalTime().ToString("f", L.Culture)
        });
        Grid.SetColumn(installed, 1);
        grid.Children.Add(installed);

        var box = new CheckBox
        {
            Content = names, IsChecked = DeviceService.GhostTickedByDefault(device), Padding = new Thickness(8, 0, 0, 0)
        };
        grid.Children.Insert(0, box);
        AutomationProperties.SetName(box, string.Join(", ", new[] { title, device.Name, device.Manufacturer, RelativeDate(device.LastUsed, _now), device.Id }
            .Where(part => part.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase)));
        box.Checked += Row_Changed;
        box.Unchecked += Row_Changed;
        return box;
    }

    private static TextBlock Secondary(TextBlock text)
    {
        text.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
        return text;
    }

    internal static string RelativeDate(DateTime? lastUsed, DateTime now) => lastUsed is not { } used ? L.Get("Devices:GhostLastUsedUnknown")
        : (now.ToLocalTime().Date - used.ToLocalTime().Date).Days switch
        {
            <= 0 => L.Get("Devices:GhostToday"),
            1 => L.Get("Devices:GhostYesterday"),
            var days => L.Format("Devices:GhostDaysAgo", days)
        };

    public static readonly GridLength DateColumnWidth = new(120);

    /// <summary>The "used in the last N days" filter at confirm time; 0 when it is off.</summary>
    public int ActiveFilterDays => FilterBox.IsChecked == true ? _days : 0;

    // Only rows on screen: a row hidden by the filter keeps its state and is never removed anyway.
    private void SelectAll_Click(object sender, RoutedEventArgs e) => SetVisibleRows(true);
    private void SelectNone_Click(object sender, RoutedEventArgs e) => SetVisibleRows(false);

    private void SetVisibleRows(bool ticked)
    {
        foreach (var row in _rows.Where(row => row.Visibility == Visibility.Visible)) row.IsChecked = ticked;
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (_rows.Count == 0 || _rows.Count < _ghosts.Count) return; // still building (XAML fires Checked/TextChanged early)
        // Blank, zero, negative or non-numeric text keeps the last valid value: the filter only turns off with its check box.
        var text = FilterDaysBox.Text.Trim();
        if (long.TryParse(text, out var value) && value > 0) _days = (int)Math.Min(value, int.MaxValue);
        else if (text.Length > 0 && text.All(char.IsAsciiDigit) && text.Any(digit => digit != '0')) _days = int.MaxValue; // too big for long
        var days = FilterBox.IsChecked == true ? _days : 0;
        for (var index = 0; index < _rows.Count; index++)
            _rows[index].Visibility = ((UIElement)_rows[index].Parent).Visibility =
                days > 0 && GhostDevices.UsedWithin(_ghosts[index].LastUsed, days, _now) ? Visibility.Collapsed : Visibility.Visible;
        foreach (var (header, rows) in _groups)
        {
            var shown = rows.Count(row => row.Visibility == Visibility.Visible);
            header.Visibility = shown > 0 ? Visibility.Visible : Visibility.Collapsed;
            header.Text = $"{GroupOf(_ghosts[_rows.IndexOf(rows[0])])} · {shown}";
        }
        UpdateCount();
    }

    private void Row_Changed(object sender, RoutedEventArgs e) => UpdateCount();

    // Display only; Remove_Click and CheckedRows read the rows again.
    private void UpdateCount()
    {
        var shown = _rows.Count(row => row.Visibility == Visibility.Visible);
        var ticked = CheckedRows.Count;
        SelectedText.Text = L.Format("Devices:GhostSelected", ticked, shown);
        RemoveButton.Content = L.Format("Devices:GhostRemoveButton", ticked);
        RemoveButton.IsEnabled = ticked > 0;
    }

    // A click that lands after the dialog closed (queued double click, Enter, UI Automation) would throw.
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible && CheckedRows.Count > 0) DialogResult = true;
    }

    private void Body_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not TextBox) DragMove();
    }
}
