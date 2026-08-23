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

Background Automation Studio V1 validates a stable recorder-and-workflow-editor foundation. Success means that a user can select one window, record or manually author a workflow, edit it visually or as a small DSL, restore the recorded target layout, and replay the workflow predictably.

## Positioning

The workflow remains inspectable and editable in both a direct-manipulation list and a concise script, while click coordinates stay relative to a reproducible recorded client window.

## Operating Context

The app runs visibly on Windows 10/11. Recording is always user initiated and observes real input in the selected target. Version 1.2 replays through a focus-safe hybrid UI Automation and Win32 background engine without moving the physical cursor. Projects are local JSON files.

## Capabilities and Constraints

Version 1.2 includes window selection and resolution, layout capture/restore, explicit recording, click and keyboard capture without mouse movement, workflow editing, DSL synchronization and validation, focus-safe hybrid background playback, Automatic/UI Automation/Win32 compatibility modes, count/infinite/duration/clock-time schedules, Clear all, English/Vietnamese UI, a configurable global Run/Stop hotkey, diagnostic playback status, and project save/load. It intentionally excludes fully hidden-window automation, multiple concurrent tasks, calendar recurrence, image/OCR/pixel logic, complex conditions, cloud, accounts, databases, plugins, process injection, drivers, and anti-cheat bypass.

## Brand Commitments

The product name is Background Automation Studio. The version label is “Version 1.2 - Focus-safe Scheduling”. The interface must be clean, minimal, modern, professional, readable, and may default to a restrained dark theme; it must not use neon, gaming styling, strong gradients, or distracting animation.

## Evidence on Hand

The supplied product brief contains the complete V1 workflow, architecture, DSL, UI outline, acceptance tests, and definition of done. No logo, imagery, customer claims, or external brand assets were supplied and none should be fabricated.

## Product Principles

- Record only explicit user actions against the selected target.
- Never record pointer movement.
- Make every workflow step inspectable and reversible before playback.
- Restore spatial context before replaying coordinate-based actions.
- Keep editor models independent from the current background-message runner so later engines can be added.

## Accessibility & Inclusion

All core actions must be labeled in text, keyboard-focusable, and communicate disabled, error, recording, paused, and running states without relying on color alone.
