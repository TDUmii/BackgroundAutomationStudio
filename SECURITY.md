# Security policy

## Reporting a vulnerability

Avoid opening a public issue for a vulnerability that could expose local workflow content, input data, or Windows user information. Use GitHub's private vulnerability-reporting feature when it is available for this repository.

Include the affected version, reproduction steps, expected behavior, and the smallest safe example needed to demonstrate the issue. Do not include passwords, tokens, private workflow files, personal paths, or third-party data.

## Security boundaries

Background Automation Studio is a local desktop utility. It does not provide process injection, kernel drivers, virtual HID devices, elevation bypasses, anti-cheat bypasses, hidden-window automation, remote control, or a network service. Background playback is limited to UI Automation patterns and ordinary Win32 window messages supported by the selected target application.
