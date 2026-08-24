using System.Text.Json.Serialization;
using BackgroundAutomationStudio.Infrastructure;

namespace BackgroundAutomationStudio.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ClickAction), "click")]
[JsonDerivedType(typeof(RightClickAction), "rightClick")]
[JsonDerivedType(typeof(DoubleClickAction), "doubleClick")]
[JsonDerivedType(typeof(TypeTextAction), "typeText")]
[JsonDerivedType(typeof(KeyPressAction), "keyPress")]
[JsonDerivedType(typeof(KeyHoldAction), "keyHold")]
[JsonDerivedType(typeof(DragAction), "drag")]
[JsonDerivedType(typeof(ScrollAction), "scroll")]
[JsonDerivedType(typeof(MovePointerAction), "movePointer")]
[JsonDerivedType(typeof(CallFunctionAction), "callFunction")]
[JsonDerivedType(typeof(WaitAction), "wait")]
[JsonDerivedType(typeof(WaitForImageAction), "waitForImage")]
[JsonDerivedType(typeof(ClickImageAction), "clickImage")]
public abstract class AutomationAction : ObservableObject
{
    private bool _enabled = true;
    private int _delayBefore;
    private string _note = string.Empty;
    private bool _isCurrent;

    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract string ActionType { get; }
    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    public int DelayBefore { get => _delayBefore; set => SetProperty(ref _delayBefore, Math.Max(0, value)); }
    public string Note { get => _note; set => SetProperty(ref _note, value?.Trim() ?? string.Empty); }
    [JsonIgnore] public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }
    [JsonIgnore] public abstract string Summary { get; }
    public abstract AutomationAction Clone();
    protected void CopyCommonTo(AutomationAction target)
    {
        target.Enabled = Enabled;
        target.DelayBefore = DelayBefore;
        target.Note = Note;
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

public sealed class KeyHoldAction : AutomationAction
{
    private string _keyName = "E";
    private int _milliseconds = 1000;
    public string KeyName { get => _keyName; set { if (SetProperty(ref _keyName, value?.ToUpperInvariant() ?? "E")) OnPropertyChanged(nameof(Summary)); } }
    public int Milliseconds { get => _milliseconds; set { if (SetProperty(ref _milliseconds, Math.Max(1, value))) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Hold Key";
    public override string Summary => $"{KeyName}  {Milliseconds:N0} ms";
    public override AutomationAction Clone() { var a = new KeyHoldAction { KeyName = KeyName, Milliseconds = Milliseconds }; CopyCommonTo(a); return a; }
}

public sealed class DragAction : AutomationAction
{
    private int _startX;
    private int _startY;
    private int _endX = 100;
    private int _endY = 100;
    private int _milliseconds = 500;
    public int StartX { get => _startX; set { if (SetProperty(ref _startX, value)) OnPropertyChanged(nameof(Summary)); } }
    public int StartY { get => _startY; set { if (SetProperty(ref _startY, value)) OnPropertyChanged(nameof(Summary)); } }
    public int EndX { get => _endX; set { if (SetProperty(ref _endX, value)) OnPropertyChanged(nameof(Summary)); } }
    public int EndY { get => _endY; set { if (SetProperty(ref _endY, value)) OnPropertyChanged(nameof(Summary)); } }
    public int Milliseconds { get => _milliseconds; set { if (SetProperty(ref _milliseconds, Math.Max(1, value))) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Drag";
    public override string Summary => $"{StartX},{StartY} → {EndX},{EndY}  {Milliseconds:N0} ms";
    public override AutomationAction Clone() { var a = new DragAction { StartX = StartX, StartY = StartY, EndX = EndX, EndY = EndY, Milliseconds = Milliseconds }; CopyCommonTo(a); return a; }
}

public sealed class ScrollAction : PointerAction
{
    private int _delta = -120;
    public int Delta { get => _delta; set { if (SetProperty(ref _delta, value == 0 ? -120 : Math.Clamp(value, -12000, 12000))) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Scroll";
    public override string Summary => $"X: {ClientX}  Y: {ClientY}  {(Delta > 0 ? "↑" : "↓")} {Math.Abs(Delta) / 120d:0.#}";
    public override AutomationAction Clone() { var a = new ScrollAction { ClientX = ClientX, ClientY = ClientY, Delta = Delta }; CopyCommonTo(a); return a; }
}

public sealed class MovePointerAction : PointerAction
{
    public override string ActionType => "Move Pointer";
    public override AutomationAction Clone() { var a = new MovePointerAction { ClientX = ClientX, ClientY = ClientY }; CopyCommonTo(a); return a; }
}

public sealed class CallFunctionAction : AutomationAction
{
    private Guid _functionId;
    private string _functionName = string.Empty;
    public Guid FunctionId { get => _functionId; set { if (SetProperty(ref _functionId, value)) OnPropertyChanged(nameof(Summary)); } }
    public string FunctionName { get => _functionName; set { if (SetProperty(ref _functionName, value?.Trim() ?? string.Empty)) OnPropertyChanged(nameof(Summary)); } }
    [JsonIgnore] public IEnumerable<AutomationFunction> AvailableFunctions { get; set; } = [];
    public override string ActionType => "Call Function";
    public override string Summary => string.IsNullOrWhiteSpace(FunctionName) ? "No function selected" : FunctionName;
    public override AutomationAction Clone() { var a = new CallFunctionAction { FunctionId = FunctionId, FunctionName = FunctionName, AvailableFunctions = AvailableFunctions }; CopyCommonTo(a); return a; }
}

public sealed class WaitAction : AutomationAction
{
    private int _milliseconds = 500;
    public int Milliseconds { get => _milliseconds; set { if (SetProperty(ref _milliseconds, Math.Max(0, value))) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Wait";
    public override string Summary => $"{Milliseconds:N0} ms";
    public override AutomationAction Clone() { var a = new WaitAction { Milliseconds = Milliseconds }; CopyCommonTo(a); return a; }
}

public abstract class ImageScanAction : AutomationAction
{
    private string _templateName = string.Empty;
    private byte[] _templatePng = [];
    private int _similarityPercent = 88;
    private int _timeoutMilliseconds = 10000;
    private int _pollIntervalMilliseconds = 150;
    private int _regionX;
    private int _regionY;
    private int _regionWidth;
    private int _regionHeight;

    public string TemplateName { get => _templateName; set { if (SetProperty(ref _templateName, value?.Trim() ?? string.Empty)) OnPropertyChanged(nameof(Summary)); } }
    public byte[] TemplatePng { get => _templatePng; set { if (SetProperty(ref _templatePng, value ?? [])) { OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(HasTemplate)); } } }
    public int SimilarityPercent { get => _similarityPercent; set { if (SetProperty(ref _similarityPercent, Math.Clamp(value, 1, 100))) OnPropertyChanged(nameof(Summary)); } }
    public int TimeoutMilliseconds { get => _timeoutMilliseconds; set { if (SetProperty(ref _timeoutMilliseconds, Math.Clamp(value, 0, 3_600_000))) OnPropertyChanged(nameof(Summary)); } }
    public int PollIntervalMilliseconds { get => _pollIntervalMilliseconds; set { if (SetProperty(ref _pollIntervalMilliseconds, Math.Clamp(value, 50, 10_000))) OnPropertyChanged(nameof(Summary)); } }
    public int RegionX { get => _regionX; set => SetProperty(ref _regionX, Math.Max(0, value)); }
    public int RegionY { get => _regionY; set => SetProperty(ref _regionY, Math.Max(0, value)); }
    public int RegionWidth { get => _regionWidth; set { if (SetProperty(ref _regionWidth, Math.Max(0, value))) OnPropertyChanged(nameof(Summary)); } }
    public int RegionHeight { get => _regionHeight; set { if (SetProperty(ref _regionHeight, Math.Max(0, value))) OnPropertyChanged(nameof(Summary)); } }
    [JsonIgnore] public bool HasTemplate => TemplatePng.Length > 0;
    [JsonIgnore] public bool UsesFullClient => RegionWidth == 0 || RegionHeight == 0;

    protected void CopyImageScanTo(ImageScanAction target)
    {
        target.TemplateName = TemplateName;
        target.TemplatePng = TemplatePng.ToArray();
        target.SimilarityPercent = SimilarityPercent;
        target.TimeoutMilliseconds = TimeoutMilliseconds;
        target.PollIntervalMilliseconds = PollIntervalMilliseconds;
        target.RegionX = RegionX;
        target.RegionY = RegionY;
        target.RegionWidth = RegionWidth;
        target.RegionHeight = RegionHeight;
        CopyCommonTo(target);
    }

    protected string ImageSummary(string behavior) => $"{behavior}  {SimilarityPercent}%  {(UsesFullClient ? "Full client" : $"{RegionWidth} x {RegionHeight} at {RegionX},{RegionY}")}";
}

public sealed class WaitForImageAction : ImageScanAction
{
    private bool _waitForDisappear;
    public bool WaitForDisappear { get => _waitForDisappear; set { if (SetProperty(ref _waitForDisappear, value)) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Wait for Image";
    public override string Summary => ImageSummary(WaitForDisappear ? "Disappear" : "Appear");
    public override AutomationAction Clone() { var action = new WaitForImageAction { WaitForDisappear = WaitForDisappear }; CopyImageScanTo(action); return action; }
}

public sealed class ClickImageAction : ImageScanAction
{
    private int _offsetX;
    private int _offsetY;
    private bool _rightClick;
    public int OffsetX { get => _offsetX; set { if (SetProperty(ref _offsetX, Math.Clamp(value, -10000, 10000))) OnPropertyChanged(nameof(Summary)); } }
    public int OffsetY { get => _offsetY; set { if (SetProperty(ref _offsetY, Math.Clamp(value, -10000, 10000))) OnPropertyChanged(nameof(Summary)); } }
    public bool RightClick { get => _rightClick; set { if (SetProperty(ref _rightClick, value)) OnPropertyChanged(nameof(Summary)); } }
    public override string ActionType => "Click Image";
    public override string Summary => ImageSummary(RightClick ? "Right click" : "Left click");
    public override AutomationAction Clone() { var action = new ClickImageAction { OffsetX = OffsetX, OffsetY = OffsetY, RightClick = RightClick }; CopyImageScanTo(action); return action; }
}
