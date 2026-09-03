# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

This product is a Windows 10/11 desktop application. The schema has no Windows-only value; the explicit WPF platform requirement is authoritative.

## Stack

C#, .NET 8, WPF, MVVM, Windows API through P/Invoke, System.Text.Json, and OpenCV through OpenCvSharp.

## Users

Windows users who need either a complete editable background-oriented workflow studio or a deliberately smaller foreground utility. The full Studio prioritizes retaining desktop control where the target accepts background delivery; the Mini editions trade that isolation for the compatibility of the physical Windows input path.

## Product Purpose

Background Automation Studio V2 validates a stable recorder, workflow editor, and covered-window playback foundation. Success means that a user can select one window, record or manually author a workflow, edit it visually or as a small DSL with undo/redo history, move or cover the target when needed, and replay the workflow predictably without surrendering foreground focus.

## Positioning

The workflow remains inspectable, reversible, and editable in both a direct-manipulation list and a concise script, while click coordinates stay relative to the target's current client window.

## Operating Context

The app runs visibly on Windows 10/11. Recording is always user initiated and observes real input in the selected target. Version 2.2 keeps Background Engine v2 and the unified dark shell, then adds wheel-scroll capture/playback, inline action notes, and keyboard-first clipboard editing. Potentially focus-taking UI Automation remains an explicitly selected compatibility mode. Projects are local JSON files.

## Capabilities and Constraints

Mini Editions v2.1.0 keeps the deliberate foreground-only split and adds opt-in sampled pointer-path recording to Foreground Recorder Mini. Recorded points stay relative to the selected target, appear as readable Move pointer steps, export in portable JSON, and replay through the physical Windows pointer. The lightweight recorder does not convert drag gestures into held-button Drag actions. One Click Mini remains the smallest edition and repeats only one selected point. Both activate the chosen target once, pause when that target loses focus, expose physical press duration, default to English with Vietnamese available, and use a lighter graphite dark theme with fully rounded dropdown and tooltip surfaces. They do not provide background pointer isolation.

Version 2.6.0 adds opt-in pointer-path recording for Engine 2. Normal mouse motion is sampled only inside the selected foreground target, stored as editable client-relative Move pointer actions, and replayed through the physical pointer with existing focus-loss protection. Movement while the left button is held remains one dedicated Drag action rather than being fragmented into pointer-path samples.

Mini Editions v1.0.0 adds two independently packaged, source-sharing utilities that omit OpenCV and the full workflow editor. Recorder Mini captures clicks, right clicks, wheel input, and key presses from one selected window and exports portable JSON. Click Repeater Mini sends one client-relative background click on a count, infinite schedule, and configurable start-to-start interval. Its point picker can be cancelled from the button or with Escape, and terminal completion, stop, error, and progress states remain visible until the next operation. Each mini ships as one self-contained Windows x64 EXE. Both retain English and Vietnamese UI, explicit raw-input limitations, local-only operation, and a global start or stop shortcut when available.

Version 2.5.0 adds HEX and RGB color matching, a spectrum picker, tolerance and minimum-area controls, wait-for-color and click-color actions, a categorized drag-and-drop block palette, and an inline reusable-function editor. Image and color capture do not guarantee usable frames from hardware-rendered or protected targets.

Version 2.4.0 adds embedded PNG templates, multi-scale grayscale matching, wait-for-appearance, wait-for-disappearance, matched-image clicking, similarity and polling controls, client-relative search regions, and foreground-screen or covered-window capture paths. Visual matching does not guarantee usable frames from hardware-rendered or protected targets.

Version 2.3.5 standardizes user-facing separators to the single ASCII hyphen in the app, documentation, and release copy.

Version 2.3.4 adds configurable Engine 2 press duration, a reliable multi-frame default, and a short pointer-settle interval before each click.

Version 2.3.3 includes window selection and resolution, explicit click/scroll/keyboard/held-key/drag recording, manual pointer movement, workflow editing, action notes, copy/cut/paste/duplicate/skip shortcuts scoped to the workflow list, shared visual/DSL undo and redo history, reusable named functions with safe nested calls, a click-through target-owned screen overlay with a denser 25-pixel grid, current points, and an edge-safe `X | Y` label above the pointer, customizable marker colors and shapes, numbered playback engines shown persistently in the main status bar and per-action run status, foreground game input that activates the explicitly selected target once before using the physical Windows input stream, client-relative playback after moving the target, strict semantic background playback, activation-shielded Win32 fallback, target-local child-window resolution while covered, improved pointer and system-key message fidelity, synthetic focus messages for experimental background playback, an opt-in focus-unsafe UI Automation compatibility mode, focus-loss auto-pause and held-input release, count/infinite/duration/clock-time schedules, repeat counts up to 1,000,000, Clear all, English/Vietnamese UI, separate global Run/Emergency Stop and Pause/Resume hotkeys, persisted Always on top control, a fully dark window shell and dialogs, line-free menus, center-out command feedback, diagnostic queued-versus-verified playback status, and project save/load. It intentionally excludes guaranteed raw-input background control, a second hardware cursor, fully hidden-window automation, multiple concurrent tasks, calendar recurrence, OCR, complex conditions, cloud, accounts, databases, plugins, process injection, drivers, packet manipulation, memory modification, and security bypasses.

## Brand Commitments

The product name is Background Automation Studio. The version label is "Version 2.6.0 - Pointer Paths". The interface must be clean, minimal, modern, professional, readable, and may default to a restrained dark theme; it must not use neon, gaming styling, strong gradients, or distracting animation.

## Evidence on Hand

The supplied product brief contains the complete V1 workflow, architecture, DSL, UI outline, acceptance tests, and definition of done. No logo, imagery, customer claims, or external brand assets were supplied and none should be fabricated.

## Product Principles

- Record only explicit user actions against the selected target.
- Record pointer movement only through an explicit opt-in control, only inside the selected focused target, and sample it to keep workflows inspectable.
- Make every workflow step inspectable and reversible before playback.
- Resolve client coordinates against the target's current location for every replayed action.
- Keep editor models independent from the current background-message runner so later engines can be added.
- Game foreground automation activates the explicitly selected target once when Run begins, because raw-input delivery requires foreground ownership. Losing focus afterward releases held synthetic input and pauses at the nearest safe boundary.
- Experimental game-background delivery must never claim success merely because Windows accepted a message; target acceptance is unknowable from outside the game.

## Accessibility & Inclusion

All core actions must be labeled in text, keyboard-focusable, and communicate disabled, error, recording, paused, and running states without relying on color alone.
