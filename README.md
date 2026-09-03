# Background Automation Studio

Build repeatable Windows workflows around a selected target window while keeping your own desktop usable. Background Automation Studio combines focus-safe playback, client-relative coordinates, a visual editor, a concise DSL, and explicit compatibility modes in one local-first tool.

## Download for Windows

**[Download BackgroundAutomationStudio.exe](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/v2.6.0/BackgroundAutomationStudio.exe)**

The release is a self-contained Windows x64 executable; no separate .NET installation is required. Checksums are published on the [v2.6.0 release page](https://github.com/TDUmii/BackgroundAutomationStudio/releases/tag/v2.6.0).

## Mini editions

Choose a mini edition when the complete workflow editor and visual matching engine are unnecessary:

- **[Foreground Recorder Mini](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/mini-v2.1.0/BackgroundAutomationRecorderMini.exe)** records clicks, right clicks, wheel input, key presses, and an optional sampled pointer path from one focused window, then plays that recording once through the normal physical Windows input stream. It keeps only the focused Engine 2 behavior and can also export portable JSON. Its lightweight recorder does not convert a drag gesture into a held-button `Drag` action; use the complete Studio when drag recording is required.
- **[One Click Mini](https://github.com/TDUmii/BackgroundAutomationStudio/releases/download/mini-v2.1.0/BackgroundAutomationClickRepeaterMini.exe)** is the smallest edition. It focuses one selected window, moves the physical pointer to one client-relative point, and repeats that click with an interval, count, infinite mode, press duration, point picker, and global start or stop shortcut. Cancel point selection with its button or `Escape`; completed, stopped, and error results remain visible until the next operation.

Each mini download is one self-contained Windows x64 EXE that needs no separate .NET installation or adjacent runtime files. Both default to English, include Vietnamese, use `CTRL+SHIFT+F9` for playback start or stop when the shortcut is available, and omit OpenCV and the full Studio editor to reduce download size. Both Mini editions intentionally take foreground focus and use the physical mouse or keyboard. They pause when the selected target loses focus and continue after it regains focus. They are not background automation and cannot leave the pointer free during playback. They do not replace the complete Studio release.

## What makes it different

| Capability | Background Automation Studio |
| --- | --- |
| Focus ownership | Background engines target one window without taking over the pointer or foreground focus. |
| Stable placement | Pointer steps use client coordinates, so moving the target window does not invalidate the workflow. |
| Honest compatibility | Five clearly named engines expose the focus, pointer, and raw-input tradeoffs before you run. |
| Two editing styles | The visual timeline and readable DSL always represent the same workflow. |
| Reuse without clutter | Named functions turn repeated sequences into a single `CALL` step and support safe nesting. |
| Spatial overview | A click-through overlay draws the live client-coordinate grid and every current point directly over the foreground target. |
| Visual decisions | Embedded PNG templates let a workflow wait for a visual state or click the center of a matched element without machine-specific paths. |
| Color decisions | HEX or RGB targets can wait for a color or click its largest matching region with adjustable tolerance. |
| Block workflow | A categorized block palette can be clicked or dragged into the workflow while preserving the readable DSL. |
| Local-first | Projects are ordinary JSON files. There are no accounts, telemetry, cloud services, or background agents. |

## Key features

- Record clicks, right clicks, double clicks, wheel scrolling, text, key presses, held keys, and drags from the selected target only.
- Optionally record a sampled client-relative mouse path for Engine 2, then replay it by moving the physical pointer through the captured points. Movement while the left button is held remains one editable drag action instead of being split into path points.
- Add precise click, drag, scroll, wait, key, text, pointer-move, and function-call steps manually.
- Inspect points directly on the target with a denser 25-pixel Screen Grid and an `X | Y` label above the live pointer; the target-owned overlay stays above topmost or borderless-style targets while they are foreground.
- Toggle grid lines and choose blue, red, green, amber, or purple pin, diamond, or crosshair markers.
- Create and edit reusable functions in the main window, call them from any workflow position, nest them, and receive a clear error for missing or circular calls.
- Drag categorized pointer, vision, input, flow, and reuse blocks directly into the workflow.
- Edit, reorder, annotate, copy, cut, paste, duplicate, skip, delete, undo, and redo workflow steps.
- Run a fixed count, continuously until stopped, for a duration, or until a clock time.
- Configure independent global Run/Emergency Stop and Pause/Resume shortcuts.
- Tune how long Engine 2 holds each mouse button or key, with a reliable 45 ms default for frame-polled input.
- Keep the target covered or move it while compatible background playback continues.
- Switch the complete interface between English and Vietnamese; English is the default.
- Save and reopen portable local project files.
- Wait for an image to appear or disappear, or click a matched image with left or right click and optional X/Y offsets.
- Tune similarity, timeout, scan interval, and a client-relative search region. The matcher checks several nearby scales and performs best with a small region.
- Keep PNG templates embedded inside the project JSON, so moving or sharing a project does not expose a local file path.
- Choose a target from the color strip or enter HEX or RGB, then tune tolerance, minimum area, timeout, polling, offsets, and a client-relative search region.

## Playback modes

| Mode | Use it when | Behavior and limit |
| --- | --- | --- |
| **1 - Strict background** | The target uses standard controls and your focus must remain untouched. | Sends semantic or Win32 input without activating the target. Raw-input games normally ignore this mode. |
| **2 - Foreground input** | Maximum raw-input game compatibility matters more than background use. | Run activates the selected target once, can record and replay normal pointer movement, settles the pointer before each click, and holds mouse buttons or keys for the configured duration. It pauses safely if the target later loses focus. |
| **3 - Background Engine v2** | You want to try covered-window delivery for a target that accepts window messages. | Resolves child controls and sends synthetic focus messages without activation. Queued delivery is reported separately because raw-input software may ignore every action. |
| **4 - Modern controls** | A custom modern control does not respond to strict background delivery. | Adds UI Automation compatibility, but the target may activate and interrupt typing or IME composition. |
| **5 - Classic Win32 messages** | The target uses legacy desktop controls. | Sends direct window messages while leaving your pointer and focus free. Raw-input games normally ignore these messages. |

No mode injects code, modifies memory or network traffic, installs a driver, creates a second hardware cursor, bypasses elevation, or bypasses security and service rules.

## Workflow commands

The visual editor and DSL are interchangeable. Available commands include `CLICK`, `RIGHT_CLICK`, `DOUBLE_CLICK`, `DRAG`, `SCROLL`, `MOVE`, `TYPE`, `KEY`, `HOLD`, `WAIT_IMAGE`, `CLICK_IMAGE`, `WAIT_COLOR`, `CLICK_COLOR`, `WAIT`, and `CALL`. Add `# NOTE` before a step to keep an inline description. Image commands include their PNG data so they remain portable when the script is edited.

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
dotnet run --project .\Editions\RecorderMini\BackgroundAutomationRecorderMini.csproj -c Release
dotnet run --project .\Editions\ClickRepeaterMini\BackgroundAutomationClickRepeaterMini.csproj -c Release
```

## Compatibility boundaries

- A target can ignore background messages when it requires foreground, raw, injected, or hardware input.
- Foreground input shares the physical desktop pointer and keyboard stream; Windows does not provide an isolated second cursor for ordinary applications.
- Pointer-path recording is opt-in, sampled to limit workflow size, and active only while the selected target owns foreground focus. Pointer-path playback takes control of the physical cursor. The complete Studio preserves left-button drags as dedicated drag actions; Foreground Recorder Mini records movement and clicks as lightweight steps and does not reproduce a held-button drag.
- The full Studio restores minimized targets before playback; fully hidden targets are not automated. The Mini editions restore and focus their selected target before sending physical input.
- The selected target must run at an equal or lower Windows integrity level.
- Moving the target is supported. Resizing it or changing its internal layout can move controls away from recorded client coordinates.
- Visual matching reads the real screen when the target is foreground. When the target is covered, it asks the target to render through the Windows `PrintWindow` path. Some hardware-rendered or protected surfaces return a blank or stale frame and must stay visible in foreground mode.
- Use automation only where the target software and service rules permit it.

## Privacy and security

- Workflows and settings stay on the local computer.
- Project files are readable JSON saved wherever you choose.
- Recording hooks exist only during an explicit recording session and are removed when recording stops, is cancelled, or the studio exits.
- Avoid storing sensitive text in workflows you intend to share.

Security reports should follow [SECURITY.md](SECURITY.md).
