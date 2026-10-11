using System.IO;
using System.Windows;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

public partial class ScriptEditorDialog : Window
{
    private readonly CustomScript _script;

    /// <summary>Edits <paramref name="script"/> in place only when the user saves valid content.</summary>
    public ScriptEditorDialog(CustomScript script, bool isNew)
    {
        InitializeComponent();
        _script = script;
        Title = HeadingText.Text = L.Get(isNew ? "Scripts:CreateCustomScript" : "Scripts:EditCustomScript");
        SaveButton.Content = L.Get(isNew ? "Scripts:Create" : "Scripts:Save");
        NameTextBox.Text = script.Name;
        PowerShellRadio.IsChecked = script.Language == ScriptLanguage.PowerShell;
        CmdRadio.IsChecked = script.Language == ScriptLanguage.Cmd;
        AdminCheckBox.IsChecked = script.RequiresAdministrator;
        ContentTextBox.Text = script.Content;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var edited = new CustomScript
        {
            Id = _script.Id,
            Name = NameTextBox.Text,
            Language = PowerShellRadio.IsChecked == true ? ScriptLanguage.PowerShell : ScriptLanguage.Cmd,
            Content = ContentTextBox.Text,
            RequiresAdministrator = AdminCheckBox.IsChecked == true
        };
        try
        {
            // Same rules as profile import, so anything saved here can be exported and imported again.
            UserDataService.ValidateCustomScript(edited);
        }
        catch (InvalidDataException)
        {
            ValidationText.Text = L.Get(edited.Name.Length == 0 ? "Scripts:NameRequired" : "Scripts:InvalidScript");
            return;
        }

        _script.Name = edited.Name;
        _script.Language = edited.Language;
        _script.Content = edited.Content;
        _script.RequiresAdministrator = edited.RequiresAdministrator;
        DialogResult = true;
    }
}
