using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

internal static class CsvExport
{
    /// <summary>Asks where to save the report and writes it; the BOM makes Excel read non-ASCII paths correctly.</summary>
    public static async Task SaveAsync(FrameworkElement owner, string fileName, string csv)
    {
        var window = Window.GetWindow(owner);
        var dialog = new SaveFileDialog { FileName = fileName, Filter = "CSV (*.csv)|*.csv" };
        if (dialog.ShowDialog(window) != true) return;
        try
        {
            await File.WriteAllTextAsync(dialog.FileName, csv, new UTF8Encoding(true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await AppDialog.ShowErrorAsync(window, L.Get("Security:ExportFailedTitle"), ex.Message);
        }
    }
}
