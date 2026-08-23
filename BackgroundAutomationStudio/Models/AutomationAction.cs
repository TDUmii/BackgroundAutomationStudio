using System.Text.Json.Serialization;
using BackgroundAutomationStudio.Infrastructure;

namespace BackgroundAutomationStudio.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ClickAction), "click")]
[JsonDerivedType(typeof(RightClickAction), "rightClick")]
[JsonDerivedType(typeof(DoubleClickAction), "doubleClick")]
[JsonDerivedType(typeof(TypeTextAction), "typeText")]
[JsonDerivedType(typeof(KeyPressAction), "keyPress")]
[JsonDerivedType(typeof(WaitAction), "wait")]
public abstract class AutomationAction : ObservableObject
{
    private bool _enabled = true;
    private int _delayBefore;
    private bool _isCurrent;

    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract string ActionType { get; }
    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    public int DelayBefore { get => _delayBefore; set => SetProperty(ref _delayBefore, Math.Max(0, value)); }
    [JsonIgnore] public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }
    [JsonIgnore] public abstract string Summary { get; }
    public abstract AutomationAction Clone();
    protected void CopyCommonTo(AutomationAction target)
    {
        target.Enabled = Enabled;
        target.DelayBefore = DelayBefore;
    }
}

public abstract class PointerAction : AutomationAction
{
    private int _clientX;
    private int _clientY;
    public int ClientX { get => _clientX; set { if (SetProperty(ref _clientX, value)) OnPropertyChanged(nameof(Summary)); } }
    public int ClientY { get => _clientY; set { if (SetProperty(ref _clientY, value)) OnPropertyChanged(nameof(Summary)); } }
    public override string Summary => $"X: {ClientX}  Y: {ClientY}";
}

public sealed class ClickAction : PointerAction
{
    public override string ActionType => "Click";
    public override AutomationAction Clone() { var a = new ClickAction { ClientX = ClientX, ClientY = ClientY }; CopyCommonTo(a); return a; }
}

public sealed class RightClickAction : PointerAction
{
    public override string ActionType => "Right Click";
    public override AutomationAction Clone() { var a = new RightClickAction { ClientX = ClientX, ClientY = ClientY }; CopyCommonTo(a); return a; }
}

public sealed class DoubleClickAction : PointerAction
{
    public override string ActionType => "Double Click";
    public override AutomationAction Clone() { var a = new DoubleClickAction { ClientX = ClientX, ClientY = ClientY }; CopyCommonTo(a); return a; }
}

public sealed class TypeTextAction : AutomationAction
{
    private string _text = string.Empty;
    public string Text { get => _text; set { if (SetProperty(ref _text, value ?? string.Empty)) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Type Text";
    public override string Summary => string.IsNullOrEmpty(Text) ? "Empty text" : Text;
    public override AutomationAction Clone() { var a = new TypeTextAction { Text = Text }; CopyCommonTo(a); return a; }
}

public sealed class KeyPressAction : AutomationAction
{
    private string _keyName = "ENTER";
    public string KeyName { get => _keyName; set { if (SetProperty(ref _keyName, value?.ToUpperInvariant() ?? "ENTER")) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Key Press";
    public override string Summary => KeyName;
    public override AutomationAction Clone() { var a = new KeyPressAction { KeyName = KeyName }; CopyCommonTo(a); return a; }
}

public sealed class WaitAction : AutomationAction
{
    private int _milliseconds = 500;
    public int Milliseconds { get => _milliseconds; set { if (SetProperty(ref _milliseconds, Math.Max(0, value))) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Wait";
    public override string Summary => $"{Milliseconds:N0} ms";
    public override AutomationAction Clone() { var a = new WaitAction { Milliseconds = Milliseconds }; CopyCommonTo(a); return a; }
}
