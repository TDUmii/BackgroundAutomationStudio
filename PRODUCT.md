# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

This product is a Windows 10/11 desktop application. The schema has no Windows-only value; the explicit WPF platform requirement is authoritative.

## Stack

C#, .NET 8, WPF, MVVM, Windows API through P/Invoke, and System.Text.Json.

## Users

Windows users who need to capture, inspect, correct, save, and replay a small sequence of clicks and keyboard actions against one chosen application window without losing control of their physical mouse during playback.

## Product Purpose

Background Automation Studio V2 validates a stable recorder, workflow editor, and covered-window playback foundation. Success means that a user can select one window, record or manually author a workflow, edit it visually or as a small DSL with undo/redo history, move or cover the target when needed, and replay the workflow predictably without surrendering foreground focus.

## Positioning

The workflow remains inspectable, reversible, and editable in both a direct-manipulation list and a concise script, while click coordinates stay relative to the target's current client window.

## Operating Context

The app runs visibly on Windows 10/11. Recording is always user initiated and observes real input in the selected target. Version 2.1 keeps Background Engine v2 and adds a unified dark window shell, center-out command feedback, line-free menus, native window controls, and a persisted Always on top pin. Potentially focus-taking UI Automation remains an explicitly selected compatibility mode. Projects are local JSON files.

## Capabilities and Constraints

Version 2.1 includes window selection and resolution, explicit recording, click/keyboard/held-key/drag capture, workflow editing, shared visual/DSL undo and redo history, client-relative playback after moving the target, strict semantic Windows Calculator playback, activation-shielded Win32 fallback, target-local child-window resolution while covered, improved pointer and system-key message fidelity, synthetic focus messages for experimental game-background playback, an opt-in focus-unsafe UI Automation compatibility mode, foreground Game Macro input with focus-loss auto-pause and held-input release, count/infinite/duration/clock-time schedules, repeat counts up to 1,000,000, Clear all, English/Vietnamese UI, separate global Run/Emergency Stop and Pause/Resume hotkeys, persisted Always on top control, a fully dark window shell, line-free menus, center-out command feedback, diagnostic queued-versus-verified playback status, and project save/load. It intentionally excludes guaranteed raw-input background control, a second hardware cursor, fully hidden-window automation, multiple concurrent tasks, calendar recurrence, image/OCR/pixel logic, complex conditions, cloud, accounts, databases, plugins, process injection, drivers, packet manipulation, memory modification, and anti-cheat bypass.

## Brand Commitments

The product name is Background Automation Studio. The version label is “Version 2.1.1 - Dark Shell Stability”. The interface must be clean, minimal, modern, professional, readable, and may default to a restrained dark theme; it must not use neon, gaming styling, strong gradients, or distracting animation.

## Evidence on Hand

The supplied product brief contains the complete V1 workflow, architecture, DSL, UI outline, acceptance tests, and definition of done. No logo, imagery, customer claims, or external brand assets were supplied and none should be fabricated.

## Product Principles

- Record only explicit user actions against the selected target.
- Never record pointer movement.
- Make every workflow step inspectable and reversible before playback.
- Resolve client coordinates against the target's current location for every replayed action.
- Keep editor models independent from the current background-message runner so later engines can be added.
- Game foreground automation must never raise or reactivate the selected game. Losing focus releases held synthetic input and pauses at the nearest safe boundary.
- Experimental game-background delivery must never claim success merely because Windows accepted a message; target acceptance is unknowable from outside the game.

## Accessibility & Inclusion

All core actions must be labeled in text, keyboard-focusable, and communicate disabled, error, recording, paused, and running states without relying on color alone.
