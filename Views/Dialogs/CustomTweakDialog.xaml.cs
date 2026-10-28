using System.IO;
using System.Windows;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

public partial class CustomTweakDialog : Window
{
    /// <summary>The validated result; null until the user saves.</summary>
    public CustomRegistryTweak? Result { get; private set; }

    private readonly string _id;

    public CustomTweakDialog(CustomRegistryTweak? existing = null)
    {
        InitializeComponent();
        Title = L.Get(existing == null ? "Tweaks:AddCustomRegistryTweak" : "Tweaks:EditCustomRegistryTweak");
        HeadingText.Text = L.Get(existing == null ? "Tweaks:CreateCustomTweak" : "Tweaks:EditCustomTweak");
        SaveButton.Content = L.Get(existing == null ? "Tweaks:Create" : "Tweaks:Save");
        _id = existing?.Id ?? Guid.NewGuid().ToString();
        NameTextBox.Text = existing?.Name ?? string.Empty;
        DescriptionTextBox.Text = existing?.Description ?? string.Empty;
        PathTextBox.Text = existing?.RegistryPath ?? string.Empty;
        ValueNameTextBox.Text = existing?.RegistryKey ?? string.Empty;
        TypeComboBox.SelectedValue = existing?.ValueType.ToUpperInvariant() ?? "REG_SZ";
        if (TypeComboBox.SelectedIndex < 0) TypeComboBox.SelectedIndex = 0;
        DataTextBox.Text = existing?.Data ?? string.Empty;
        RiskComboBox.SelectedValue = existing?.RiskClass ?? "Moderate";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var tweak = new CustomRegistryTweak
        {
            Id = _id,
            Name = NameTextBox.Text,
            Description = DescriptionTextBox.Text,
            RegistryPath = PathTextBox.Text,
            RegistryKey = ValueNameTextBox.Text,
            ValueType = TypeComboBox.SelectedValue as string ?? "REG_SZ",
            Data = DataTextBox.Text,
            RiskClass = RiskComboBox.SelectedValue as string ?? "Moderate"
        };
        try
        {
            // Same rules as profile import, so anything saved here can be exported and imported again.
            UserDataService.ValidateCustomTweak(tweak);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidDataException or OverflowException)
        {
            ValidationText.Text = string.IsNullOrWhiteSpace(tweak.Name) ? L.Get("Tweaks:RequiredCustomFields") : ex.Message;
            return;
        }
        Result = tweak;
        DialogResult = true;
    }
}
