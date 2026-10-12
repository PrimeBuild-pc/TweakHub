using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace TweakHub.Views;

/// <summary>Theme-aware horizontal bar rows shared by the Security tabs.</summary>
internal static class SecurityCharts
{
    public sealed record Segment(double Value, string BrushKey, string ToolTip);

    public static UIElement Row(string label, IReadOnlyList<Segment> segments, double scale, string valueText, bool selected, Action? onClick)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 24, 8), Background = System.Windows.Media.Brushes.Transparent };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

        var name = new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = selected ? FontWeights.Bold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = label
        };
        name.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

        var bar = new Grid { Height = 10, VerticalAlignment = VerticalAlignment.Center };
        var track = new Rectangle { RadiusX = 4, RadiusY = 4 };
        track.SetResourceReference(Shape.FillProperty, "SubtleFillBrush");
        var fill = new Grid();
        var used = 0.0;
        foreach (var segment in segments.Where(segment => segment.Value > 0))
        {
            fill.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(segment.Value, GridUnitType.Star) });
            var part = new Rectangle { RadiusX = 3, RadiusY = 3, Margin = new Thickness(0, 0, 1, 0), ToolTip = segment.ToolTip };
            part.SetResourceReference(Shape.FillProperty, segment.BrushKey);
            Grid.SetColumn(part, fill.ColumnDefinitions.Count - 1);
            fill.Children.Add(part);
            used += segment.Value;
        }
        fill.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(scale - used, 0), GridUnitType.Star) });
        Grid.SetColumnSpan(track, 1);
        bar.Children.Add(track);
        bar.Children.Add(fill);
        Grid.SetColumn(bar, 1);

        var value = new TextBlock { Text = valueText, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        value.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
        Grid.SetColumn(value, 2);

        row.Children.Add(name);
        row.Children.Add(bar);
        row.Children.Add(value);
        if (onClick != null)
        {
            row.Cursor = Cursors.Hand;
            row.Focusable = true;
            System.Windows.Automation.AutomationProperties.SetName(row, $"{label}: {valueText}");
            row.MouseLeftButtonUp += (_, _) => onClick();
            row.KeyDown += (_, e) =>
            {
                if (e.Key is not (Key.Enter or Key.Space)) return;
                e.Handled = true;
                onClick();
            };
        }
        return row;
    }

    public static UIElement LegendItem(string text, string brushKey)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 4) };
        var swatch = new Rectangle { Width = 10, Height = 10, RadiusX = 2, RadiusY = 2, Margin = new Thickness(0, 0, 5, 0) };
        swatch.SetResourceReference(Shape.FillProperty, brushKey);
        var label = new TextBlock { Text = text, FontSize = 11 };
        label.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
        panel.Children.Add(swatch);
        panel.Children.Add(label);
        return panel;
    }
}
