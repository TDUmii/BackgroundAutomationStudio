# Background Automation Studio

Build repeatable Windows workflows around a selected target window while keeping your own desktop usable. Background Automation Studio combines focus-safe playback, client-relative coordinates, a visual editor, a concise DSL, and explicit compatibility modes in one local-first tool.

## Download for Windows

**[Download BackgroundAutomationStudio.exe](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/v2.3.4/BackgroundAutomationStudio.exe)**

The release is a self-contained Windows x64 executable; no separate .NET installation is required. Checksums are published on the [v2.3.4 release page](https://github.com/TDUmii/BackgroundAutomationStudio/releases/tag/v2.3.4).

## What makes it different

| Capability | Background Automation Studio |
| --- | --- |
| Focus ownership | Background engines target one window without taking over the pointer or foreground focus. |
| Stable placement | Pointer steps use client coordinates, so moving the target window does not invalidate the workflow. |
| Honest compatibility | Five clearly named engines expose the focus, pointer, and raw-input tradeoffs before you run. |
| Two editing styles | The visual timeline and readable DSL always represent the same workflow. |
| Reuse without clutter | Named functions turn repeated sequences into a single `CALL` step and support safe nesting. |
| Spatial overview | A click-through overlay draws the live client-coordinate grid and every current point directly over the foreground target. |
| Local-first | Projects are ordinary JSON files. There are no accounts, telemetry, cloud services, or background agents. |

## Key features

- Record clicks, right clicks, double clicks, wheel scrolling, text, key presses, held keys, and drags from the selected target only.
- Add precise click, drag, scroll, wait, key, text, pointer-move, and function-call steps manually.
- Inspect points directly on the target with a denser 25-pixel Screen Grid and an `X | Y` label above the live pointer; the target-owned overlay stays above topmost or borderless-style targets while they are foreground.
- Toggle grid lines and choose blue, red, green, amber, or purple pin, diamond, or crosshair markers.
- Create reusable functions from the same DSL, call them from any workflow position, nest them, and receive a clear error for missing or circular calls.
- Edit, reorder, annotate, copy, cut, paste, duplicate, skip, delete, undo, and redo workflow steps.
- Run a fixed count, continuously until stopped, for a duration, or until a clock time.
- Configure independent global Run/Emergency Stop and Pause/Resume shortcuts.
- Tune how long Engine 2 holds each mouse button or key, with a reliable 45 ms default for frame-polled input.
- Keep the target covered or move it while compatible background playback continues.
- Switch the complete interface between English and Vietnamese; English is the default.
- Save and reopen portable local project files.

## Playback modes

| Mode | Use it when | Behavior and limit |
| --- | --- | --- |
| **1 — Strict background** | The target uses standard controls and your focus must remain untouched. | Sends semantic or Win32 input without activating the target. Raw-input games normally ignore this mode. |
| **2 — Foreground input** | Maximum raw-input game compatibility matters more than background use. | Run activates the selected target once, settles the pointer before each click, and holds mouse buttons or keys for the configured duration. It pauses safely if the target later loses focus. |
| **3 — Background Engine v2** | You want to try covered-window delivery for a target that accepts window messages. | Resolves child controls and sends synthetic focus messages without activation. Queued delivery is reported separately because raw-input software may ignore every action. |
| **4 — Modern controls** | A custom modern control does not respond to strict background delivery. | Adds UI Automation compatibility, but the target may activate and interrupt typing or IME composition. |
| **5 — Classic Win32 messages** | The target uses legacy desktop controls. | Sends direct window messages while leaving your pointer and focus free. Raw-input games normally ignore these messages. |

No mode injects code, modifies memory or network traffic, installs a driver, creates a second hardware cursor, bypasses elevation, or bypasses security and service rules.

## Workflow commands

The visual editor and DSL are interchangeable. Available commands are `CLICK`, `RIGHT_CLICK`, `DOUBLE_CLICK`, `DRAG`, `SCROLL`, `MOVE`, `TYPE`, `KEY`, `HOLD`, `WAIT`, and `CALL`. Add `# NOTE` before a step to keep an inline description.

```text
MOVE 240 180
CLICK 240 180
CALL "Confirm sequence"
WAIT 500
```

## Requirements

- Windows 10 or Windows 11, x64.
- The downloadable executable has no external runtime dependency.
- Building from source requires the .NET 8 SDK.

```powershell
dotnet build .\BackgroundAutomationStudio.sln -c Release
dotnet test .\BackgroundAutomationStudio.sln -c Release
dotnet run --project .\BackgroundAutomationStudio\BackgroundAutomationStudio.csproj -c Release
```

## Compatibility boundaries

- A target can ignore background messages when it requires foreground, raw, injected, or hardware input.
- Foreground input shares the physical desktop pointer and keyboard stream; Windows does not provide an isolated second cursor for ordinary applications.
- Minimized targets are restored before playback; fully hidden targets are not automated.
- The selected target must run at an equal or lower Windows integrity level.
- Moving the target is supported. Resizing it or changing its internal layout can move controls away from recorded client coordinates.
- Use automation only where the target software and service rules permit it.

## Privacy and security

- Workflows and settings stay on the local computer.
- Project files are readable JSON saved wherever you choose.
- Recording hooks exist only during an explicit recording session and are removed when recording stops, is cancelled, or the studio exits.
- Avoid storing sensitive text in workflows you intend to share.

Security reports should follow [SECURITY.md](SECURITY.md).
