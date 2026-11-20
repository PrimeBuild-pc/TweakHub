using System.Windows;
using System.Windows.Controls;

namespace TweakHub.Views.Controls;

public partial class SectionHeading : UserControl
{
    public static readonly DependencyProperty PageTitleProperty =
        DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(SectionHeading));

    public static readonly DependencyProperty TabsProperty =
        DependencyProperty.Register(nameof(Tabs), typeof(TabControl), typeof(SectionHeading));

    public SectionHeading() => InitializeComponent();

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    public TabControl? Tabs
    {
        get => (TabControl?)GetValue(TabsProperty);
        set => SetValue(TabsProperty, value);
    }

    private void Root_Click(object sender, RoutedEventArgs e)
    {
        if (Tabs is not null)
            Tabs.SelectedIndex = 0;
    }
}
