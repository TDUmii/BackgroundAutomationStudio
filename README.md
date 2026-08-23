# Background Automation Studio

Background Automation Studio is a Windows 10/11 desktop application for recording, inspecting, editing, saving, and replaying small automation workflows against one selected application window. Playback runs in the background without moving the physical pointer or sending global input.

## Download for Windows

Download the ready-to-run, self-contained Windows x64 application:

**[Download BackgroundAutomationStudio.exe](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/v1.3.0/BackgroundAutomationStudio.exe)**

No separate .NET installation is required. Release details and the SHA-256 checksum are available on the [v1.3.0 release page](https://github.com/TDUmii/BackgroundAutomationStudio/releases/tag/v1.3.0).

## Features

- Select and re-resolve one visible target window, then capture its client layout as recording metadata.
- Record explicit clicks and keyboard actions while ignoring pointer movement and actions on other windows.
- Edit, reorder, duplicate, enable, disable, and delete actions in the visual editor or the built-in DSL.
- Undo and redo workflow edits from buttons, the Edit menu, `Ctrl+Z`, `Ctrl+Y`, or `Ctrl+Shift+Z`.
- Run, pause, resume, stop, highlight the current action, and choose a fixed repeat count, infinite run, duration timer, or clock stop time.
- Clear the complete workflow with one confirmed action.
- Replay through UI Automation patterns for modern controls such as Windows Calculator, with a `PostMessage` fallback for classic Win32 controls.
- Choose Automatic, Modern controls (UI Automation), or Classic Win32 messages in Settings.
- See diagnostic playback status for the engine used, fallback behavior, minimized-target restoration, and actionable compatibility errors.
- Keep using the physical mouse and keyboard during playback. The runner never calls `SetCursorPos` or `SendInput`.
- Keep the target covered, type in another application, or drag a visible target to a new position while playback continues. Client coordinates are resolved again for every action.
- Switch the interface between English and Vietnamese. English is the first-run default.
- Configure a global Run/Stop hotkey, defaulting to `Ctrl+Shift+F9`.
- Save projects as local JSON. Language, hotkey, and playback compatibility are stored in the current Windows user's local application-data folder.

## Background playback compatibility

Automatic mode searches the selected target's UI Automation tree for the smallest actionable element at the recorded client coordinate and calls its supported `Invoke`, `Toggle`, `SelectionItem`, or `ExpandCollapse` pattern. If no actionable element exists, it falls back to ordinary Win32 background messages.

Modern-controls mode requires UI Automation for left and double clicks and reports a clear error when no actionable control exists. Classic mode sends pointer actions as Win32 messages. Keyboard and right-click actions use classic background messages in every mode.

Neither playback path intentionally activates the target or moves the physical cursor. During playback, an activation shield temporarily prevents the target from becoming the foreground window and a continuous guard handles UI Automation providers that request activation asynchronously. The original window style is always restored when playback stops, is cancelled, or fails. Minimized targets are shown without activation; a normal target may remain covered while the user selects and types in another application. A fully hidden window must be shown first.

Playback no longer forces the target back to its recorded desktop position or size. Coordinates remain client-relative and are converted against the target's current position for every action, so moving a visible target does not break the running workflow.

Games, elevated applications when the studio is not elevated, custom browser or canvas surfaces, raw-input software, and anti-cheat software may still ignore both mechanisms. The application does not use process injection, drivers, virtual HID devices, elevation bypasses, or anti-cheat bypasses.

Recording remains an explicit, user-initiated observation of real actions in the selected target window. The non-interference guarantee applies to playback.

## Requirements

- Windows 10 or Windows 11, x64.
- .NET 8 SDK for building from source.
- No Python, database, cloud account, browser extension, or background service is required.

## Build and test

```powershell
dotnet restore .\BackgroundAutomationStudio.sln
dotnet build .\BackgroundAutomationStudio.sln -c Release --no-restore
dotnet test .\BackgroundAutomationStudio.sln -c Release --no-build
dotnet run --project .\BackgroundAutomationStudio\BackgroundAutomationStudio.csproj -c Release
```

To create a self-contained, single-file Windows build:

```powershell
dotnet publish .\BackgroundAutomationStudio\BackgroundAutomationStudio.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\artifacts\BackgroundAutomationStudio-win-x64
```

## Workflow script

The visual editor and script editor operate on the same workflow. Supported commands are:

```text
CLICK 200 150
RIGHT_CLICK 220 180
DOUBLE_CLICK 300 240
TYPE "Hello World"
KEY ENTER
WAIT 500
```

Invalid commands are reported with line-specific errors and cannot be run until corrected.

## Privacy and security

- Workflows and settings stay on the local computer.
- Project files are ordinary JSON saved to a location chosen by the user.
- The application contains no telemetry, analytics, account system, cloud synchronization, network client, process injection, driver, or anti-cheat bypass.
- Low-level input hooks are installed only during an explicit recording session and are removed when recording stops, is cancelled, or the application exits.
- Do not automate passwords, recovery codes, payment data, or other sensitive text in workflows you plan to share.

## Project structure

- `BackgroundAutomationStudio/Models` - project, target, settings, and action models.
- `BackgroundAutomationStudio/Services` - recorder, hybrid background runner, localization, settings, hotkey, window, script, project, and dialog services.
- `BackgroundAutomationStudio/ViewModels` - main application state and commands.
- `BackgroundAutomationStudio/Views` - action editor, settings, and coordinate marker windows.
- `BackgroundAutomationStudio/Native` - bounded Win32 interop.
- `BackgroundAutomationStudio.Tests` - parser, model, persistence, run-schedule, hotkey, and settings tests.

## Known limitations

- UI Automation depends on controls exposed by the target application.
- Win32 background messages can be ignored by applications that require foreground, raw, injected, or hardware input.
- Minimized targets are restored before playback; fully hidden targets are not automated.
- The selected target must run at an equal or lower Windows integrity level. If the target runs as Administrator, run the studio at the same level.
- Coordinate actions are client-relative. Moving the target is supported, but resizing it or changing its responsive layout can move controls away from their recorded client coordinates.
