using System.Collections.ObjectModel;
using System.Windows;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Views;

public partial class FunctionsWindow : Window
{
    private readonly ObservableCollection<AutomationFunction> _functions;
    private readonly IReadOnlyList<CallFunctionAction> _workflowCalls;
    public IReadOnlyList<AutomationFunction> Result => _functions;
    public FunctionsWindow(IEnumerable<AutomationFunction> functions, IEnumerable<AutomationAction> workflowActions)
    {
        InitializeComponent(); WindowAppearance.EnableDarkTitleBar(this);
        _workflowCalls = workflowActions.OfType<CallFunctionAction>().ToList();
        _functions = new(functions.Select(item => item.Clone())); FunctionsList.DataContext = _functions;
        _functions.CollectionChanged += (_, _) => RefreshEmptyState(); RefreshEmptyState();
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var function = new AutomationFunction { Name = NextName(), Actions = [new WaitAction { Milliseconds = 500 }] };
        var editor = new FunctionEditorWindow(function, _functions) { Owner = this };
        if (editor.ShowDialog() != true) return; _functions.Add(editor.Function); FunctionsList.SelectedItem = editor.Function;
    }
    private void Edit_Click(object sender, RoutedEventArgs e) => EditSelected();
    private void FunctionsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => EditSelected();
    private void EditSelected()
    {
        if (FunctionsList.SelectedItem is not AutomationFunction selected) return;
        var editor = new FunctionEditorWindow(selected, _functions) { Owner = this };
        if (editor.ShowDialog() != true) return; var index = _functions.IndexOf(selected); _functions[index] = editor.Function; RepairCallNames(); FunctionsList.SelectedItem = editor.Function;
    }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (FunctionsList.SelectedItem is not AutomationFunction selected) return;
        if (_workflowCalls.Any(call => call.FunctionId == selected.Id || call.FunctionId == Guid.Empty && call.FunctionName.Equals(selected.Name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(LocalizationService.Language == "vi" ? "Hàm này vẫn đang được quy trình chính gọi. Hãy xóa hoặc đổi bước CALL trước." : "The main workflow still calls this function. Remove or change that CALL step first.", LocalizationService.Get("FunctionsTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var message = LocalizationService.Language == "vi" ? $"Xóa hàm \"{selected.Name}\"? Các bước CALL đang dùng hàm này sẽ cần được sửa." : $"Delete \"{selected.Name}\"? CALL steps that use it will need to be updated.";
        if (MessageBox.Show(message, LocalizationService.Get("FunctionsTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) _functions.Remove(selected);
    }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        RepairCallNames();
        try { foreach (var function in _functions) WorkflowFunctionExpander.Expand(function.Actions, _functions); }
        catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, LocalizationService.Get("FunctionsTitle"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DialogResult = true;
    }
    private string NextName() { var index = 1; while (_functions.Any(item => item.Name.Equals($"Function {index}", StringComparison.OrdinalIgnoreCase))) index++; return $"Function {index}"; }
    private void RefreshEmptyState() => EmptyState.Visibility = _functions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    private void RepairCallNames()
    {
        foreach (var call in _functions.SelectMany(item => item.Actions).OfType<CallFunctionAction>())
        {
            var target = _functions.FirstOrDefault(item => item.Id == call.FunctionId) ?? _functions.FirstOrDefault(item => item.Name.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase));
            if (target is not null) { call.FunctionId = target.Id; call.FunctionName = target.Name; }
        }
    }
}
