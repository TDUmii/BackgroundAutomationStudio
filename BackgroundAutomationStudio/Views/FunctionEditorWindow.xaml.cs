using System.Windows;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Views;

public partial class FunctionEditorWindow : Window
{
    private readonly ScriptParser _parser = new();
    private readonly IReadOnlyList<AutomationFunction> _catalog;
    private readonly IReadOnlyCollection<string> _otherNames;
    public AutomationFunction Function { get; private set; }

    public FunctionEditorWindow(AutomationFunction function, IEnumerable<AutomationFunction> catalog)
    {
        InitializeComponent(); WindowAppearance.EnableDarkTitleBar(this);
        Function = function.Clone(); _catalog = catalog.ToList();
        _otherNames = _catalog.Where(item => item.Id != Function.Id).Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        NameBox.Text = Function.Name; ScriptBox.Text = _parser.Serialize(Function.Actions);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ShowError(LocalizationService.Language == "vi" ? "Tên hàm là bắt buộc." : "Function name is required."); NameBox.Focus(); return; }
        if (_otherNames.Contains(name)) { ShowError(LocalizationService.Language == "vi" ? "Tên hàm đã tồn tại. Hãy dùng tên khác." : "That function name already exists. Choose another name."); NameBox.Focus(); NameBox.SelectAll(); return; }
        var parsed = _parser.Parse(ScriptBox.Text);
        if (!parsed.IsValid) { ShowError(string.Join(Environment.NewLine, parsed.Errors)); ScriptBox.Focus(); return; }
        if (parsed.Actions.Count == 0) { ShowError(LocalizationService.Language == "vi" ? "Hàm cần ít nhất một thao tác." : "A function needs at least one action."); ScriptBox.Focus(); return; }
        foreach (var call in parsed.Actions.OfType<CallFunctionAction>())
        {
            var target = _catalog.FirstOrDefault(item => item.Name.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase));
            if (target is null && name.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase)) target = Function;
            if (target is null) { ShowError(LocalizationService.Language == "vi" ? $"Không tìm thấy hàm \"{call.FunctionName}\"." : $"Function \"{call.FunctionName}\" was not found."); ScriptBox.Focus(); return; }
            call.FunctionId = target.Id;
        }
        Function.Name = name; Function.Actions = new(parsed.Actions); DialogResult = true;
    }
    private void ShowError(string message) { ErrorText.Text = message; ErrorPanel.Visibility = Visibility.Visible; }
}
