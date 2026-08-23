using System.Windows;
using System.IO;
using Microsoft.Win32;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Views;

namespace BackgroundAutomationStudio.Services;

public interface IDialogService
{
    string? OpenProject();
    string? SaveProject(string suggestedName);
    RecordChoice ChooseRecordMode();
    AutomationAction? EditAction(AutomationAction action, WindowTarget? target, bool isNew = false);
    void Info(string title, string message);
    void Error(string title, string message);
}

public enum RecordChoice { Cancel, Replace, Append }

public sealed class DialogService(WindowPickerService picker, IWindowManager windowManager) : IDialogService
{
    public string? OpenProject()
    {
        var dialog = new OpenFileDialog { Title = LocalizationService.Get("OpenProjectDialog"), Filter = "Automation project (*.json)|*.json|All files (*.*)|*.*" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveProject(string suggestedName)
    {
        var dialog = new SaveFileDialog { Title = LocalizationService.Get("SaveProjectDialog"), Filter = "Automation project (*.json)|*.json", DefaultExt = ".json", AddExtension = true, FileName = Sanitize(suggestedName) };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public RecordChoice ChooseRecordMode() => MessageBox.Show("The workflow already contains actions.\n\nYes: replace it with this recording\nNo: append this recording\nCancel: keep the workflow unchanged", "Record workflow", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
    {
        MessageBoxResult.Yes => RecordChoice.Replace,
        MessageBoxResult.No => RecordChoice.Append,
        _ => RecordChoice.Cancel
    };

    public AutomationAction? EditAction(AutomationAction action, WindowTarget? target, bool isNew = false)
    {
        var clone = action.Clone();
        var type = clone.ActionType switch { "Click" => LocalizationService.Get("Click"), "Right click" => LocalizationService.Get("RightClick"), "Double click" => LocalizationService.Get("DoubleClick"), "Type text" => LocalizationService.Get("TypeText"), "Key press" => LocalizationService.Get("KeyPress"), _ => LocalizationService.Get("Wait") };
        var dialog = new ActionEditorWindow(clone, target, picker, windowManager) { Owner = Application.Current.MainWindow, Title = $"{LocalizationService.Get(isNew ? "Add" : "Edit")} {type}" };
        return dialog.ShowDialog() == true ? dialog.Action : null;
    }

    public void Info(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    public void Error(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static string Sanitize(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '-');
        return string.IsNullOrWhiteSpace(name) ? "automation-project.json" : name + ".json";
    }
}
