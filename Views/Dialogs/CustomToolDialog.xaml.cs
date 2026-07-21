using System.IO;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

public partial class CustomToolDialog : Window
{
    private readonly string _id;
    public ExternalTool Tool { get; private set; }

    public CustomToolDialog(IEnumerable<string> categories, ExternalTool? existing = null)
    {
        InitializeComponent();
        _id = existing?.Id ?? Guid.NewGuid().ToString("N");
        Tool = existing ?? new ExternalTool();
        var categoryList = categories.ToList();
        CategoryComboBox.ItemsSource = categoryList.Select(ShortcutService.LocalizeCategory);
        NameTextBox.Text = existing?.Name ?? string.Empty;
        DescriptionTextBox.Text = existing?.Description ?? string.Empty;
        CategoryComboBox.Text = ShortcutService.LocalizeCategory(existing?.Category ?? categoryList.FirstOrDefault() ?? "Custom");
        RequiresAdministratorCheckBox.IsChecked = existing?.RequiresAdministrator == true;

        var action = existing?.PowerShellCommand.Length > 0 ? "PowerShell"
            : existing?.WingetId.Length > 0 ? "Winget"
            : "Website";
        ActionComboBox.SelectedValue = action;
        ActionValueTextBox.Text = action switch
        {
            "PowerShell" => existing?.PowerShellCommand,
            "Winget" => existing?.WingetId,
            _ => existing?.DownloadUrl
        } ?? string.Empty;
        UpdateActionHelp();
    }

    private void ActionType_Changed(object sender, SelectionChangedEventArgs e) => UpdateActionHelp();

    private void UpdateActionHelp()
    {
        if (ActionValueLabel == null) return;
        var action = ActionComboBox.SelectedValue as string;
        ActionValueLabel.Text = action switch
        {
            "Winget" => L.Get("Tools:WingetPackageId"),
            "PowerShell" => L.Get("Tools:PowerShellCommand"),
            _ => L.Get("Tools:HttpsUrl")
        };
        ActionHelpText.Text = action switch
        {
            "Winget" => L.Get("Tools:WingetHelp"),
            "PowerShell" => L.Get("Tools:PowerShellHelp"),
            _ => L.Get("Tools:WebsiteHelp")
        };
        RequiresAdministratorCheckBox.Visibility = action == "PowerShell" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var action = ActionComboBox.SelectedValue as string ?? "Website";
        var value = ActionValueTextBox.Text.Trim();
        var tool = new ExternalTool
        {
            Id = _id,
            Name = NameTextBox.Text,
            Description = DescriptionTextBox.Text,
            Category = ShortcutService.CategoryKey(CategoryComboBox.Text),
            IsCustom = true,
            IsFavorite = Tool.IsFavorite,
            RequiresAdministrator = action == "PowerShell" && RequiresAdministratorCheckBox.IsChecked == true,
            DownloadUrl = action == "Website" ? value : string.Empty,
            WingetId = action == "Winget" ? value : string.Empty,
            PowerShellCommand = action == "PowerShell" ? value : string.Empty
        };

        try
        {
            UserDataService.ValidateCustomTool(tool);
            Tool = tool;
            DialogResult = true;
        }
        catch (InvalidDataException ex)
        {
            ValidationText.Text = ex.Message;
        }
    }
}
