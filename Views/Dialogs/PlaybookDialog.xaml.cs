using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

/// <summary>Creates or edits a playbook: steps detected from this PC, plus tweaks, apps and scripts added by hand.</summary>
public partial class PlaybookDialog : Window
{
    private sealed record AddChoice(string Name, string ReferenceId, string WingetId)
    {
        public override string ToString() => Name;
    }

    private readonly ObservableCollection<PlaybookStep> _steps;
    private readonly string _id;

    public Playbook? Result { get; private set; }

    /// <param name="detect">Fill the steps from this PC's configuration as soon as the dialog opens.</param>
    public PlaybookDialog(Playbook? existing, bool detect)
    {
        InitializeComponent();
        Title = HeadingText.Text = L.Get(existing == null ? "Scripts:CreatePlaybook" : "Scripts:EditPlaybook");
        _id = existing?.Id ?? Guid.NewGuid().ToString("N");
        NameBox.Text = existing?.Name
                       ?? (detect ? L.Format("Scripts:PlaybookCapturedName", Environment.MachineName, DateTime.Now.ToString("d", L.Culture)) : string.Empty);
        DescriptionBox.Text = existing?.Description ?? string.Empty;
        _steps = new(existing?.Steps.Select(Clone) ?? []);
        _steps.CollectionChanged += (_, _) => UpdateEmpty();
        StepsList.ItemsSource = _steps;
        UpdateEmpty();

        foreach (var kind in new[] { PlaybookStepType.Tweak, PlaybookStepType.Winget, PlaybookStepType.Script })
            AddKindBox.Items.Add(new ComboBoxItem { Content = L.Get($"Scripts:PlaybookType{kind}"), Tag = kind });
        AddKindBox.SelectedIndex = 0;
        if (detect) Loaded += async (_, _) => await DetectAsync();
    }

    private void UpdateEmpty() => EmptyText.Visibility = _steps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private async void Detect_Click(object sender, RoutedEventArgs e) => await DetectAsync();

    /// <summary>Adds what this PC has and the list does not: running it twice never duplicates steps.</summary>
    private async Task DetectAsync()
    {
        DetectButton.IsEnabled = false;
        DetectRing.IsActive = true;
        EmptyText.Text = L.Get("Scripts:PlaybookDetecting");
        try
        {
            var present = _steps.Select(Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var step in await PlaybookService.Instance.CaptureAsync())
                if (present.Add(Key(step))) _steps.Add(step);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Text.Json.JsonException)
        {
            await AppDialog.ShowErrorAsync(this, L.Get("Scripts:PlaybookDetect"), ex.Message);
        }
        finally
        {
            DetectRing.IsActive = false;
            DetectButton.IsEnabled = true;
            EmptyText.Text = L.Get("Scripts:PlaybookNoSteps");
            UpdateEmpty();
        }
    }

    private static string Key(PlaybookStep step) => $"{step.Type}|{step.ReferenceId}|{step.WingetId}";

    private PlaybookStepType SelectedKind => AddKindBox.SelectedItem is ComboBoxItem { Tag: PlaybookStepType kind } ? kind : PlaybookStepType.Tweak;

    private void AddKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var kind = SelectedKind;
        AddEnabledBox.Visibility = kind == PlaybookStepType.Tweak ? Visibility.Visible : Visibility.Collapsed;
        AddItemBox.Text = string.Empty;
        AddItemBox.ItemsSource = kind switch
        {
            PlaybookStepType.Tweak => TweakChoices(),
            PlaybookStepType.Winget => ApplicationChoices(),
            _ => UserDataService.Instance.LoadCustomScripts().Select(script => new AddChoice(script.Name, script.Id, string.Empty))
                .OrderBy(choice => choice.Name).ToList()
        };
    }

    private static List<AddChoice> TweakChoices()
    {
        if (TweakService.Instance.TweakCategories.Count == 0) TweakService.Instance.LoadTweaks();
        return TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .Select(tweak => new AddChoice(tweak.Name, $"builtin:{tweak.Id}", string.Empty))
            .Concat(UserDataService.Instance.LoadCustomTweaks().Select(tweak => new AddChoice(tweak.Name, $"custom:{tweak.Id}", string.Empty)))
            .OrderBy(choice => choice.Name).ToList();
    }

    private static List<AddChoice> ApplicationChoices()
    {
        if (ShortcutService.Instance.ExternalTools.Count == 0) ShortcutService.Instance.Initialize();
        return ShortcutService.Instance.ExternalTools.Where(tool => tool.WingetId.Length > 0)
            .Select(tool => new AddChoice(tool.Name, string.Empty, tool.WingetId))
            .OrderBy(choice => choice.Name).ToList();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var kind = SelectedKind;
        var choice = AddItemBox.SelectedItem as AddChoice;
        // Applications not in the catalogue: the typed text is taken as a winget id.
        if (choice is null && kind == PlaybookStepType.Winget && AddItemBox.Text.Trim() is { Length: > 0 } typed)
        {
            try { UserDataService.ValidateWingetId(typed); }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException)
            {
                await AppDialog.ShowWarningAsync(this, L.Get("Scripts:PlaybookInvalid"), ex.Message);
                return;
            }
            choice = new AddChoice(typed, string.Empty, typed);
        }
        if (choice is null) return;
        _steps.Add(new PlaybookStep
        {
            Type = kind,
            Name = choice.Name.Length > 100 ? choice.Name[..100] : choice.Name,
            ReferenceId = choice.ReferenceId,
            WingetId = choice.WingetId,
            TargetEnabled = kind != PlaybookStepType.Tweak || AddEnabledBox.IsChecked == true
        });
        StepsList.ScrollIntoView(_steps[^1]);
    }

    private void Move(object sender, int direction)
    {
        if ((sender as FrameworkElement)?.DataContext is not PlaybookStep step) return;
        var index = _steps.IndexOf(step);
        var target = index + direction;
        if (index >= 0 && target >= 0 && target < _steps.Count) _steps.Move(index, target);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(sender, -1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(sender, 1);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is PlaybookStep step) _steps.Remove(step);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var playbook = new Playbook
        {
            Id = _id,
            Name = NameBox.Text.Trim(),
            Description = DescriptionBox.Text.Trim(),
            Steps = _steps.Where(step => step.IsIncluded).ToList()
        };
        try
        {
            UserDataService.ValidatePlaybook(playbook);
        }
        catch (InvalidDataException ex)
        {
            await AppDialog.ShowWarningAsync(this, L.Get("Scripts:PlaybookInvalid"), ex.Message);
            return;
        }
        Result = playbook;
        DialogResult = true;
    }

    private static PlaybookStep Clone(PlaybookStep step) => new()
    {
        Id = step.Id,
        Type = step.Type,
        ReferenceId = step.ReferenceId,
        Name = step.Name,
        WingetId = step.WingetId,
        TargetEnabled = step.TargetEnabled,
        Data = step.Data
    };
}
