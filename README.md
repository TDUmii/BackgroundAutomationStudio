# Background Automation Studio

Background Automation Studio is a Windows 10/11 desktop application for recording, inspecting, editing, saving, and replaying automation workflows against one selected application window. Desktop engines preserve the physical pointer and foreground focus; the opt-in Game Macro engine uses the normal Windows input stream for compatibility and pauses safely when the game loses focus.

## Download for Windows

Download the ready-to-run, self-contained Windows x64 application:

**[Download BackgroundAutomationStudio.exe](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/v1.5.1/BackgroundAutomationStudio.exe)**

No separate .NET installation is required. Release details and the SHA-256 checksum are available on the [v1.5.1 release page](https://github.com/TDUmii/BackgroundAutomationStudio/releases/tag/v1.5.1).

## Features

- Select and re-resolve one visible target window, then capture its client layout as recording metadata.
- Record explicit clicks and keyboard actions while ignoring pointer movement and actions on other windows.
- Edit, reorder, duplicate, enable, disable, and delete actions in the visual editor or the built-in DSL.
- Undo and redo workflow edits from buttons, the Edit menu, `Ctrl+Z`, `Ctrl+Y`, or `Ctrl+Shift+Z`.
- Run, pause, resume, stop, highlight the current action, and choose a fixed repeat count, infinite run, duration timer, or clock stop time.
- Clear the complete workflow with one confirmed action.
- Replay Windows Calculator controls through focus-safe semantic keyboard messages without activating Calculator or resetting another app's IME composition.
- Choose Strict background, Modern controls (may take focus), or Classic Win32 messages in Settings.
- Choose Game Macro - foreground for games that require real Windows input. Switching to another app releases held input and auto-pauses without forcing the game back to the front.
- Try Game background - experimental for targeted Win32 key and pointer messages. The studio never activates the game and states plainly that raw-input games may ignore them.
- Record and edit held keys and pointer drags in either visual or script form (`HOLD` and `DRAG`). Game recording recognizes key holds of at least 150 ms and pointer drags beyond the Windows drag threshold.
- Configure separate global Run/Emergency Stop and Pause/Resume shortcuts, defaulting to `Ctrl+Shift+F9` and `Ctrl+Shift+F10`.
- Run up to 1,000,000 repeats, indefinitely, for a duration, or until a clock time.
- See diagnostic playback status for the engine used, fallback behavior, minimized-target restoration, and actionable compatibility errors.
- Keep using the physical mouse and keyboard in desktop modes. Those engines never call `SetCursorPos` or `SendInput`.
- Keep the target covered, type in another application, or drag a visible target to a new position while playback continues. Client coordinates are resolved again for every action.
- Switch the interface between English and Vietnamese. English is the first-run default.
- Open Settings reliably, scroll through every playback mode, select the final mode, and reopen directly at the saved selection.
- Use Run/Stop as an emergency stop. The runner releases any injected held key or mouse button when stopped, cancelled, paused, or interrupted.
- Save projects as local JSON. Language, hotkey, and playback compatibility are stored in the current Windows user's local application-data folder.

## Background playback compatibility

Strict background mode, the default, searches the selected target's UI Automation tree only to identify the smallest actionable element at the recorded client coordinate. It never calls a provider's focus-taking `Invoke` pattern. Supported semantic controls, including standard Windows Calculator buttons, are translated to targeted background keyboard messages; other controls fall back to ordinary Win32 background messages.

Modern-controls mode is an explicit compatibility option for controls without a strict-background adapter. It may call UI Automation `Invoke`, `Toggle`, `SelectionItem`, or `ExpandCollapse`; provider behavior is outside the studio's control and may take foreground focus or reset IME composition. Classic mode sends pointer actions as Win32 messages. Keyboard and right-click actions use classic background messages in every mode.

Strict background and Classic modes do not activate the target or move the physical cursor. During playback, an activation shield temporarily prevents ordinary activation and a continuous guard protects against unexpected target-family foreground changes. The original window style is always restored when playback stops, is cancelled, or fails. Minimized targets are shown without activation; a normal target may remain covered while the user selects, types, and uses an IME in another application. A fully hidden window must be shown first.

Playback no longer forces the target back to its recorded desktop position or size. Coordinates remain client-relative and are converted against the target's current position for every action, so moving a visible target does not break the running workflow.

Games, elevated applications when the studio is not elevated, custom browser or canvas surfaces, raw-input software, and anti-cheat software may still ignore both mechanisms. The application does not use process injection, drivers, virtual HID devices, elevation bypasses, or anti-cheat bypasses.

## Game Macro compatibility

**Game Macro - foreground** converts clicks, text, key presses, held keys, and drags into ordinary `SendInput` events. It does not activate or raise the selected game. Before every action and throughout waits or held input, it verifies that the selected top-level window is still foreground. If the user changes apps, held keys/buttons are released and playback waits. Returning to the selected game resumes the remaining schedule automatically. Because Windows exposes no atomic "send only if this HWND is still foreground" operation, the single action already crossing the OS input boundary during an exact focus transition can be cancelled; later iterations resume and input is not redirected intentionally.

**Game background - experimental** uses targeted window messages and preserves the user's foreground window and pointer. It also verifies that a focused child belongs to the selected top-level target before posting keys, preventing a sibling window in the same process/thread from receiving the macro. Acceptance cannot be detected from outside the target: Project Zomboid and other raw-input games may ignore every message while unfocused.

Game Macro does not inject code, modify memory, alter packets, accelerate server-side actions, bypass anti-cheat, or create a separate hardware cursor. Use automation only where the game and server rules permit it. In multiplayer, the server remains authoritative and an administrator may treat macros as prohibited automation even when every action uses normal timing.

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
HOLD E 1500
DRAG 200 150 500 350 800
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

- Strict semantic playback depends on recognizable controls and targeted background messages supported by the target application.
- The opt-in Modern controls mode can still take focus because UI Automation provider behavior belongs to the target application.
- Win32 and experimental game-background messages can be ignored by applications that require foreground, raw, injected, or hardware input.
- Foreground Game Macro uses the physical desktop pointer and active input stream. It auto-pauses on focus loss, but it cannot create an isolated second Windows cursor.
- A single atomic foreground action that is already crossing the Windows input boundary at the exact moment focus changes may be cancelled; playback resumes from the following boundary after the game is active again.
- Minimized targets are restored before playback; fully hidden targets are not automated.
- The selected target must run at an equal or lower Windows integrity level. If the target runs as Administrator, run the studio at the same level.
- Coordinate actions are client-relative. Moving the target is supported, but resizing it or changing its responsive layout can move controls away from their recorded client coordinates.
