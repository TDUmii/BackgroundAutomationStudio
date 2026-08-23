using System.Windows;

namespace BackgroundAutomationStudio.Services;

public static class LocalizationService
{
    private static string _language = "en";
    public static string Language => _language;
    public static event EventHandler? LanguageChanged;

    private static readonly Dictionary<string, (string En, string Vi)> Text = new()
    {
        ["MenuFile"] = ("File", "Tệp"), ["MenuEdit"] = ("Edit", "Chỉnh sửa"), ["MenuHotkey"] = ("Hotkey", "Phím tắt"), ["MenuSettings"] = ("Settings", "Cài đặt"),
        ["NewProject"] = ("New project", "Dự án mới"), ["Open"] = ("Open...", "Mở..."), ["Save"] = ("Save", "Lưu"), ["SaveAs"] = ("Save as...", "Lưu thành..."),
        ["EditAction"] = ("Edit action", "Sửa thao tác"), ["DuplicateAction"] = ("Duplicate action", "Nhân bản thao tác"), ["DeleteAction"] = ("Delete action", "Xóa thao tác"), ["MoveUp"] = ("Move up", "Di chuyển lên"), ["MoveDown"] = ("Move down", "Di chuyển xuống"),
        ["VersionLabel"] = ("Version 1.1 - Background Input & Settings", "Phiên bản 1.1 - Đầu vào nền & Cài đặt"),
        ["TargetWindow"] = ("Target window", "Cửa sổ đích"), ["ClientCoordinates"] = ("CLIENT COORDINATES", "TỌA ĐỘ VÙNG KHÁCH"), ["TargetHelp"] = ("Choose the exact window that receives recorded and replayed actions.", "Chọn đúng cửa sổ sẽ nhận thao tác ghi và phát lại."), ["SelectWindow"] = ("Select window", "Chọn cửa sổ"),
        ["Application"] = ("APPLICATION", "ỨNG DỤNG"), ["WindowTitle"] = ("WINDOW TITLE", "TIÊU ĐỀ CỬA SỔ"), ["Class"] = ("CLASS", "LỚP"), ["NotSelected"] = ("Not selected", "Chưa chọn"), ["NotCaptured"] = ("Not captured", "Chưa ghi nhận"), ["RecordedLayout"] = ("RECORDED WINDOW LAYOUT", "BỐ CỤC CỬA SỔ ĐÃ GHI"),
        ["CapturePlayback"] = ("Capture & playback", "Ghi & phát lại"), ["Recording"] = ("RECORDING", "ĐANG GHI"), ["Record"] = ("Record", "Ghi"), ["StopRecord"] = ("Stop record", "Dừng ghi"), ["CancelRecord"] = ("Cancel record", "Hủy ghi"), ["RunWorkflow"] = ("Run workflow", "Chạy quy trình"), ["Pause"] = ("Pause", "Tạm dừng"), ["Resume"] = ("Resume", "Tiếp tục"), ["StopRun"] = ("Stop run", "Dừng chạy"),
        ["RepeatCount"] = ("REPEAT COUNT", "SỐ LẦN LẶP"), ["RepeatHelp"] = ("Runs 1 to 999 times without taking your physical mouse.", "Chạy từ 1 đến 999 lần mà không chiếm chuột thật."),
        ["Project"] = ("Project", "Dự án"), ["ModifiedHelp"] = ("An asterisk marks changes that have not been saved.", "Dấu hoa thị đánh dấu thay đổi chưa được lưu."),
        ["Workflow"] = ("Workflow", "Quy trình"), ["WorkflowHelp"] = ("Drag steps to reorder, or edit the same workflow as script.", "Kéo để đổi thứ tự, hoặc sửa cùng quy trình bằng mã lệnh."), ["AddAction"] = ("Add action", "Thêm thao tác"), ["VisualEditor"] = ("Visual editor", "Trình sửa trực quan"), ["ScriptEditor"] = ("Script editor", "Trình sửa mã lệnh"),
        ["NoActions"] = ("No actions yet", "Chưa có thao tác"), ["NoActionsHelp"] = ("Select a target, then Record - or use Add action to build the workflow manually.", "Chọn cửa sổ đích, sau đó Ghi - hoặc dùng Thêm thao tác để tạo thủ công."), ["Edit"] = ("Edit", "Sửa"), ["Duplicate"] = ("Duplicate", "Nhân bản"), ["Delete"] = ("Delete", "Xóa"), ["Current"] = ("CURRENT", "HIỆN TẠI"),
        ["ScriptHelp"] = ("Commands: CLICK, RIGHT_CLICK, DOUBLE_CLICK, TYPE, KEY, WAIT. Valid edits update the visual workflow immediately.", "Lệnh: CLICK, RIGHT_CLICK, DOUBLE_CLICK, TYPE, KEY, WAIT. Mã hợp lệ cập nhật quy trình trực quan ngay lập tức."),
        ["BackgroundMode"] = ("Hybrid background playback - physical input stays free", "Phát nền kết hợp - chuột và phím thật vẫn tự do"),
        ["Click"] = ("Click", "Nhấp trái"), ["RightClick"] = ("Right click", "Nhấp phải"), ["DoubleClick"] = ("Double click", "Nhấp đúp"), ["TypeText"] = ("Type text", "Nhập văn bản"), ["KeyPress"] = ("Key press", "Nhấn phím"), ["Wait"] = ("Wait", "Chờ"),
        ["SettingsTitle"] = ("Settings", "Cài đặt"), ["Language"] = ("Language", "Ngôn ngữ"), ["English"] = ("English", "Tiếng Anh"), ["Vietnamese"] = ("Vietnamese", "Tiếng Việt"), ["RunHotkey"] = ("Run / stop hotkey", "Phím tắt chạy / dừng"), ["PressShortcut"] = ("Click the field, then press a shortcut.", "Nhấp vào ô rồi nhấn tổ hợp phím."), ["SaveSettings"] = ("Save settings", "Lưu cài đặt"), ["Cancel"] = ("Cancel", "Hủy"), ["InputMode"] = ("Playback compatibility", "Khả năng tương thích phát lại"),
        ["Compatibility"] = ("A covered target can keep running without activation. If it is minimized, the app restores its recorded layout first. Fully hidden, elevated, raw-input, canvas, game, or anti-cheat targets may still reject background automation.", "Cửa sổ đích bị che vẫn có thể chạy mà không cần kích hoạt. Nếu bị thu nhỏ, ứng dụng sẽ khôi phục bố cục đã ghi trước. Cửa sổ bị ẩn hoàn toàn, chạy quyền cao, raw-input, canvas, game hoặc anti-cheat vẫn có thể từ chối tự động hóa nền."),
        ["PlaybackModeHelp"] = ("Choose how recorded pointer clicks are delivered. Keyboard and right-click actions use classic background messages.", "Chọn cách gửi các lần nhấp đã ghi. Bàn phím và nhấp phải sử dụng thông điệp nền cổ điển."),
        ["AutomaticMode"] = ("Automatic (recommended)", "Tự động (khuyên dùng)"), ["AutomaticModeHelp"] = ("Try modern UI Automation first, then fall back to Win32 messages.", "Thử UI Automation hiện đại trước, sau đó chuyển sang thông điệp Win32."),
        ["UiAutomationMode"] = ("Modern controls (UI Automation)", "Điều khiển hiện đại (UI Automation)"), ["UiAutomationModeHelp"] = ("Require an actionable modern control at every left or double-click point.", "Yêu cầu có điều khiển hiện đại có thể thao tác tại mọi điểm nhấp trái hoặc nhấp đúp."),
        ["Win32Mode"] = ("Classic Win32 messages", "Thông điệp Win32 cổ điển"), ["Win32ModeHelp"] = ("Send pointer clicks directly as window messages for classic desktop applications.", "Gửi lần nhấp trực tiếp dưới dạng thông điệp cửa sổ cho ứng dụng desktop cổ điển."),
        ["ClientPoint"] = ("Client point", "Điểm vùng khách"), ["XCoordinate"] = ("X coordinate", "Tọa độ X"), ["YCoordinate"] = ("Y coordinate", "Tọa độ Y"), ["PickNewPoint"] = ("Pick new point", "Chọn điểm mới"), ["TestPoint"] = ("Test point", "Kiểm tra điểm"), ["CoordinateHelp"] = ("Coordinates are relative to the target's client area, not the screen.", "Tọa độ tính theo vùng khách của cửa sổ đích, không phải màn hình."),
        ["TextToType"] = ("Text to type", "Văn bản cần nhập"), ["KeyOrShortcut"] = ("Key or shortcut", "Phím hoặc tổ hợp phím"), ["ShortcutHelp"] = ("Shortcuts such as CTRL+C and ALT+F4 are also accepted.", "Cũng chấp nhận tổ hợp như CTRL+C và ALT+F4."), ["WaitDuration"] = ("Wait duration", "Thời gian chờ"), ["Milliseconds"] = ("Milliseconds", "Mili giây"), ["MillisecondHelp"] = ("1000 milliseconds equals one second.", "1000 mili giây bằng một giây."),
        ["EditActionHelp"] = ("Edit the action, then save it back to the workflow.", "Sửa thao tác rồi lưu lại vào quy trình."), ["DelayBefore"] = ("Delay before this action (ms)", "Độ trễ trước thao tác (ms)"), ["ActionEnabled"] = ("Action enabled", "Bật thao tác"), ["SaveAction"] = ("Save action", "Lưu thao tác"),
        ["Add"] = ("Add", "Thêm"), ["OpenProjectDialog"] = ("Open automation project", "Mở dự án tự động hóa"), ["SaveProjectDialog"] = ("Save automation project", "Lưu dự án tự động hóa"),
        ["Ready"] = ("Ready - Select a target window to begin", "Sẵn sàng - Chọn cửa sổ đích để bắt đầu"), ["NoTarget"] = ("No target selected", "Chưa chọn cửa sổ đích")
    };

    public static string Get(string key) => Text.TryGetValue(key, out var value) ? (_language == "vi" ? value.Vi : value.En) : key;

    public static void Apply(string language)
    {
        _language = language.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
        if (Application.Current is { } app) foreach (var item in Text) app.Resources[item.Key] = _language == "vi" ? item.Value.Vi : item.Value.En;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }
}
