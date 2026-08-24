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
    AutomationAction? EditAction(AutomationAction action, WindowTarget? target, IEnumerable<AutomationFunction>? functions = null, bool isNew = false);
    IReadOnlyList<AutomationFunction>? ManageFunctions(IEnumerable<AutomationFunction> functions, IEnumerable<AutomationAction> workflowActions);
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

    public AutomationAction? EditAction(AutomationAction action, WindowTarget? target, IEnumerable<AutomationFunction>? functions = null, bool isNew = false)
    {
        var clone = action.Clone();
        var available = functions?.ToList() ?? [];
        if (clone is CallFunctionAction call) { call.AvailableFunctions = available; if (call.FunctionId == Guid.Empty && available.Count > 0) { call.FunctionId = available[0].Id; call.FunctionName = available[0].Name; } }
        var type = clone switch { ClickAction => LocalizationService.Get("Click"), RightClickAction => LocalizationService.Get("RightClick"), DoubleClickAction => LocalizationService.Get("DoubleClick"), BackgroundAutomationStudio.Models.DragAction => LocalizationService.Get("Drag"), ScrollAction => LocalizationService.Get("Scroll"), MovePointerAction => LocalizationService.Get("MovePointer"), CallFunctionAction => LocalizationService.Get("CallFunction"), TypeTextAction => LocalizationService.Get("TypeText"), KeyPressAction => LocalizationService.Get("KeyPress"), KeyHoldAction => LocalizationService.Get("KeyHold"), WaitForImageAction => LocalizationService.Get("WaitForImage"), ClickImageAction => LocalizationService.Get("ClickImage"), WaitForColorAction => LocalizationService.Get("WaitForColor"), ClickColorAction => LocalizationService.Get("ClickColor"), _ => LocalizationService.Get("Wait") };
        var dialog = new ActionEditorWindow(clone, target, picker, windowManager) { Owner = Application.Current.MainWindow, Title = $"{LocalizationService.Get(isNew ? "Add" : "Edit")} {type}" };
        if (dialog.ShowDialog() != true) return null;
        if (dialog.Action is CallFunctionAction editedCall) editedCall.FunctionName = available.FirstOrDefault(item => item.Id == editedCall.FunctionId)?.Name ?? editedCall.FunctionName;
        return dialog.Action;
    }

    public IReadOnlyList<AutomationFunction>? ManageFunctions(IEnumerable<AutomationFunction> functions, IEnumerable<AutomationAction> workflowActions)
    {
        var dialog = new FunctionsWindow(functions, workflowActions) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public void Info(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    public void Error(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static string Sanitize(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '-');
        return string.IsNullOrWhiteSpace(name) ? "automation-project.json" : name + ".json";
    }
}
