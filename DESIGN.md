---
name: Background Automation Studio
description: A calm, operator-focused Windows studio for recording, editing, and replaying hybrid background automation workflows.
colors:
  primary: "#2764C4"
  primary-hover: "#2F6DCC"
  primary-focus: "#9EC0FF"
  current-surface: "#1D355D"
  current-border: "#5A8FE4"
  danger: "#F05D68"
  danger-surface: "#2A1A1E"
  app-background: "#101216"
  panel-background: "#171A20"
  panel-raised: "#1E222A"
  field-background: "#12151A"
  selected-surface: "#292F39"
  selected-border: "#6E798A"
  border: "#303641"
  text-primary: "#F3F5F8"
  text-secondary: "#AEB6C4"
typography:
  headline:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "22px"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "19px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "13px"
  label:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "11px"
  code:
    fontFamily: "Consolas"
    fontSize: "14px"
rounded:
  control: "8px"
  row: "10px"
  panel: "12px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  xxl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "8px 14px"
    height: "36px"
  field:
    backgroundColor: "{colors.field-background}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.control}"
    padding: "8px 10px"
  workflow-row-selected:
    backgroundColor: "{colors.selected-surface}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.row}"
    padding: "10px 12px"
  workflow-row-current:
    backgroundColor: "{colors.current-surface}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.row}"
    padding: "10px 12px"
---

# Design System: Background Automation Studio

## Overview

**Creative North Star: "The Graphite Control Desk"**

Background Automation Studio is a dense but calm Windows operator workspace. Graphite surfaces keep target context, transport controls, project state, and the workflow editor visible together; darker accessible blue is reserved for primary action, keyboard focus, and live execution; red is reserved for recording, stop-record emphasis, and errors. Version 2.2 extends the editor with scroll actions, inline notes, and keyboard-first clipboard commands without adopting Remaku's tree composition, gaming neon, decorative HUD chrome, or false claims of guaranteed raw-input background control.

The hierarchy follows the work rather than decoration. Selection and current execution are visibly different states, important fields have visible names, and the empty editor provides authored next-step guidance instead of a blank canvas.

Recorder Mini and Click Repeater Mini inherit the same Graphite Control Desk tokens but distill the shell to one task. Each mini edition uses one window, one primary action, direct status text, the same field and button vocabulary, English by default with Vietnamese available, and no visual matching surfaces. Their reduced composition must feel intentionally focused rather than like the main Studio with missing panels. Click Repeater point selection is an explicit mode with a visible Cancel action and Escape exit. Completed, stopped, and error states retain their final progress until the next operation instead of immediately resetting to Ready.

**Key Characteristics:**

- Graphite tonal layering with thin cool-gray boundaries.
- Restrained blue for primary commands, focus, and live execution.
- Recording red paired with a dot, label, and elapsed time.
- Native Segoe UI typography, with Consolas limited to scripts and machine-readable values.
- Compact, text-led Windows controls and explicit keyboard focus.

## Colors

The palette is a cool graphite neutral system with one operational blue accent and one narrowly scoped red family. The frontmatter values are normative.

### Primary

- **Operator Blue:** Primary commands such as selecting a target, running a workflow, adding an action, and saving an edited action.
- **Focus Ice:** High-contrast keyboard focus and the current-step play marker.
- **Execution Blue:** The surface and border for the action currently being executed.

### Secondary

- **Recording Red:** Recording, stop-record emphasis, and validation/error surfaces. It always appears with text or another non-color cue.

### Neutral

- **Graphite Canvas:** Application background.
- **Graphite Panel:** Main panels, menus, and dialogs.
- **Raised Graphite:** Neutral buttons and selected tabs.
- **Selected Graphite:** Editing selection, intentionally gray so it cannot be confused with execution.
- **Cool Boundary:** Thin borders for panels, fields, and rows.
- **Primary and Secondary Text:** Near-white for commands and essential data; cool gray for descriptions and metadata.

**The State Ownership Rule.** Gray means selected for editing; blue plus a play marker and “CURRENT” means executing; red plus a dot, label, and timer means recording.

**The Accent Restraint Rule.** Do not introduce amber, neon hues, gradients, or decorative blue washes. The implemented V1 palette uses blue for operation and red for recording/error emphasis.

## Typography

**Display Font:** Segoe UI Variable Text, falling back to Segoe UI  
**Body Font:** Segoe UI Variable Text, falling back to Segoe UI  
**Label/Mono Font:** Consolas for workflow script, HWND, recording time, and validation output

**Character:** Native Windows readability and rapid scanning. Weight and size establish hierarchy; ornamental type and decorative tracking do not.

### Hierarchy

- **Headline** (Semibold, 22px): Main workflow title.
- **Title** (Semibold, 19px): Left-column panel titles.
- **Dialog Title** (Semibold, 24px): Action type in the focused editor window.
- **Section Title** (Semibold, 17px): Action-specific editor section.
- **Body** (Regular, 13px): Commands, descriptions, values, and ordinary controls.
- **Label** (Regular, 11px): Metadata and named field labels.
- **Code** (Regular, 14px): Editable DSL; compact machine values use 11px Consolas.

**The Native Readability Rule.** Keep ordinary UI in Segoe UI. Consolas communicates scripts, identifiers, time, or validation output - not brand personality.

## Layout

The main window opens at 1280×820 with a 1024×680 minimum. A 42px menu/product bar and 34px status bar frame the work area. Inside an 18px margin, a fixed 350px context-and-transport column sits 18px from the flexible workflow editor. The left column scrolls vertically when required; additional width belongs to the editor.

Major panels use 18px internal padding. Spacing follows a 4/8/12/16/24/32 family, with 14px and 18px used where the dense desktop grid needs intermediate separation. The action editor is a focused 470px owner-centered dialog, sized to content and capped at 720px high. Settings opens as a resizable 620×820 owner-centered window with a 560×680 minimum; its 24px outer margin, fixed header and footer, and single scrolling content panel keep Save settings continuously available.

Workflow rows scan left-to-right as play marker, number, enabled checkbox, localized action type, summary with an optional muted italic note, and current-state text. The empty list centers an authored title and specific guidance for recording or adding an action manually.

**The Editor Priority Rule.** Preserve the context rail and give additional window width to the workflow editor.

## Elevation & Depth

The system is flat by default and uses tonal layering rather than drop shadows. Canvas, panel, raised-control, and field surfaces combine with one-pixel cool-gray borders to convey depth.

**The Tonal Depth Rule.** Do not add shadows merely to make controls clickable; use the established surface, border, hover, and focus changes.

## Shapes

Controls and nested information wells use 8px corners, workflow rows use 10px, and major panels/status pills use 12px. Borders are normally one pixel; focused buttons and workflow rows increase to a two-pixel outline. Circular geometry is reserved for the recording dot.

## Components

### Buttons

- **Shape:** 8px radius, at least 36px high, with 14px horizontal and 8px vertical padding.
- **Primary:** Operator Blue, near-white text, semibold weight.
- **Neutral:** Raised Graphite with a Cool Boundary border; hover moves to Selected Graphite.
- **Danger:** Dark red-brown surface, muted red border, and light rose text for Stop record.
- **Focus / Disabled:** Two-pixel Focus Ice border for keyboard focus; readable 42% opacity when disabled.

### Cards / Containers

- **Corner Style:** Major panels use 12px; nested wells use 8px.
- **Background:** Graphite Panel with darker nested surfaces.
- **Shadow Strategy:** No shadows.
- **Internal Padding:** 18px for panels; 8 - 12px for compact state containers.

### Inputs / Fields

- **Style:** Dark field surface, near-white text, one-pixel boundary, blue caret, and 10px by 8px padding.
- **Focus:** Fields shift to an Operator Blue border; buttons and rows use a brighter two-pixel focus border.
- **Names:** Visible labels name X, Y, text, shortcut, wait duration, and delay. Automation names add units and target-client context.
- **Numeric Recovery:** Every constrained numeric field has an explicit accessible name that describes the value, units, and relevant target context. On save, invalid fields receive a red border and requirement tooltip; the first invalid field is focused and fully selected, and the warning repeats that field's accessible name plus the exact whole-number requirement so correction is immediate rather than a generic failure.
- **Validation:** Script errors appear below the editor in a bordered dark-red surface with light rose Consolas text.

### Navigation

The 34px custom title bar uses the deepest graphite surface and replaces the mismatched native white strip while preserving drag, resize, minimize, maximize/restore, and close behavior. Product identity sits left, the version stays quiet at center, and window controls sit right; the pin joins them as a persisted operator control. The 42px command bar shares the Graphite Canvas color with the app body so the shell reads continuously. File, Edit, hotkey, and Settings controls stay transparent. Hover, keyboard focus, and an open menu reveal a two-pixel blue underline that scales from the center to full width in 140ms and exits in 90ms. Dropdown and context menus contain no separator elements or rules. Text tabs use muted inactive labels, graphite hover, and a selected raised surface with near-white text and a blue underline. The bottom status bar stays quiet and persistent.

When Windows disables client-area animation, the underline changes state immediately while retaining the same visible feedback.

### Playback Engine Settings

Settings keeps one continuous, explicitly numbered playback-engine list. Strict background remains first and selected by default. **Foreground input** explains that Run activates the selected target once, then uses the physical pointer with focus-loss auto-pause; **Background Engine v2 - experimental** names its covered-window improvements and uncertainty directly, including raw-input rejection. Modern UI Automation and Classic Win32 remain available in the same list. The main status bar and every active-action status repeat the selected engine number and name so a saved mode change is immediately verifiable. Selected modes use the existing Execution Blue surface and border; warnings remain text-led rather than introducing a new alert color.

Run/Emergency Stop and Pause/Resume form one compact two-column group with a 12px gutter. The equal-width, separately named fields use 15px Consolas, a 42px minimum height, and inline per-field error text. Their labels describe behavior rather than implementation, and the settings copy states that emergency stop releases held synthetic input.

### Scrollbars

Main and Settings share one application-level **SlimScrollBar** treatment: an 8px transparent track with a muted graphite thumb, 4px corner radius, and 1px inset. Reuse this shared resource for dense vertical content instead of creating window-specific scrollbar variants.

### Screen Coordinate Overlay

Coordinate authoring uses a transparent, click-through overlay aligned to the foreground target's live client area rather than a miniature map inside the editor. Minor lines mark 25 client pixels, major labeled lines mark 100, and numbered markers reuse the selected pin, diamond, or crosshair treatment. A compact Graphite backplate is centered above the physical pointer and reports its live client coordinate as `X | Y`; it moves below only when the upper edge has insufficient space and remains clamped inside the client bounds. The native overlay is owned by and ordered immediately above the selected target, allowing it to remain visible over topmost or borderless-style targets without becoming globally topmost. It never activates or accepts input, hides when the target loses foreground, and is disabled during recording and playback. Standard dialogs explicitly paint their complete client background with Graphite Canvas so outer margins never fall back to the white Windows default.

Because the overlay sits over arbitrary third-party content, coordinate labels use white text on near-opaque Graphite backplates, markers keep a white outline, and grid and client-boundary strokes remain translucent so underlying content stays visible. The blue, red, green, amber, and purple marker choices are a spatial-annotation exception to the application palette; never reuse them for app chrome or workflow-state semantics.

### Workflow Rows

Rows use a 10px corner and compact six-part scan order. Disabled rows use 46% opacity. Selection uses Selected Graphite and a neutral border. Keyboard focus uses the two-pixel Focus Ice outline. Current execution overrides the selected fill with Execution Blue and adds both a left play marker and right-aligned “CURRENT” label.

### Recording Status

The recording banner uses a dark red surface and muted red border. An eight-pixel red dot, “RECORDING,” and a Consolas elapsed-time value make the state readable without color alone.

### Playback Compatibility

Settings presents five full-width, text-led choices in one continuous list: Strict background, Foreground input, Background Engine v2 - experimental, Modern controls that may take focus, and Classic Win32 messages. Each choice states its purpose, focus or pointer consequence, and main limit; uses the established graphite/Execution Blue selection treatment; and remains keyboard focusable. Compatibility copy distinguishes a covered window from minimized and fully hidden states so the interface does not promise universal hidden-window automation.

Foreground input timing is one compact numeric setting beneath the engine list. It uses milliseconds, defaults to 45, accepts 10 through 1000, and explains that a longer hold can help frame-polled targets detect quick mouse and keyboard presses. Validation names the range, focuses the field, and selects its value for immediate correction.

During a run, the activation shield prevents ordinary target activation and the status bar distinguishes focus-safe semantic commands, Win32 fallback, and explicitly requested focus-unsafe UI Automation. Playback never restores the target's recorded desktop position. A visible target can be dragged while running because every action resolves its client-relative coordinate against the current window position.

### Editor History

Undo and Redo are compact neutral controls beside Add action and are repeated in the Edit menu. They use standard Windows gestures: `Ctrl+Z` for Undo, `Ctrl+Y` and `Ctrl+Shift+Z` for Redo. History covers visual edits, script edits, reordering, deletion, clearing, and completed recording batches while preserving stable action IDs. Running and recording temporarily disable history navigation.

Clipboard commands live in the Edit and row context menus and reuse the same history rather than introducing a separate document model. When the workflow list owns keyboard focus, `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, and `Ctrl+D` copy, cut, paste, and duplicate; `Space` toggles skip, `Enter` edits, `Delete` removes, and `Alt+Up/Down` reorders. These gestures never intercept typing or clipboard commands inside the DSL editor.

### Scroll Actions and Notes

Mouse scroll is a first-class client-relative action beside click and drag. Its editor uses the same X/Y picker and validation system, plus a signed wheel delta where positive means up, negative means down, and 120 represents one notch. The row summary stays compact. Each action may carry one optional 180-character note, shown beneath its summary in muted italic text only when present and serialized as a readable `# NOTE` line before the DSL command.

### Visual Matching Actions

Wait for image and Click matched image use the same focused action editor as the rest of the workflow. The PNG preview appears before tuning controls, followed by similarity, timeout, scan interval, and a compact X/Y/W/H search region. Zero width and height clearly mean the full target client area. Helper copy recommends smaller regions for speed and fewer false matches. The editor scrolls within its graphite container so the action note and Save action remain reachable without enlarging the dialog beyond the desktop.

Color actions share the visual matching capture path. A compact spectrum strip, live swatch, HEX field, synchronized RGB channels, tolerance, minimum matching area, timing, and client-relative region controls keep selection precise without a separate picker window.

The visual editor includes a restrained Scratch-inspired palette rather than copying Scratch puzzle geometry. Pointer, vision, input, flow, and reuse blocks use stable category colors and can be clicked or dragged to a workflow position. Reusable functions open in an inline right-side workspace with the function list, name, DSL steps, validation, save, and delete controls, so editing never creates another window.

### Run Schedule

The capture-and-playback panel uses one dark native-feeling selector for four mutually exclusive schedules: a fixed repeat count, infinite until stopped, a duration in minutes, or a clock stop time. Only the field required by the selected schedule remains visible. The dropdown, selected state, keyboard focus, and popup items use the same graphite surfaces and blue focus treatment as the rest of the studio.

Clear all belongs at the lower-left edge of the visual workflow toolbar, spatially separated from single-row editing commands. It uses the danger treatment and always asks for confirmation before clearing the workflow.

### Empty State

The empty editor centers “No actions yet” with guidance to select a target, then record or use Add action. Keep it actionable and specific; do not replace it with a blank list or generic placeholder.

## Do's and Don'ts

### Do:

- **Do** distinguish selection, keyboard focus, execution, disabled state, and recording with markers, text, borders, or opacity in addition to color.
- **Do** keep primary actions dark blue and reserve red for recording, stop-record, and errors.
- **Do** preserve visible field labels and descriptive automation names, including units and target-client context.
- **Do** place paired global hotkeys in the compact two-column group and keep every playback mode reachable through the themed settings scroller.
- **Do** reuse the shared slim scrollbar in Main and Settings scrolling regions.
- **Do** make numeric recovery field-specific: name the field, state its valid range, focus it, and select its contents.
- **Do** keep the workflow editor dominant while target and transport context remain available.
- **Do** use concise empty-state and helper copy that tells the operator what to do next.

### Don't:

- **Don't** use blue for ordinary row selection; gray is selection while blue is current execution.
- **Don't** communicate recording with a red dot alone; retain the label and timer.
- **Don't** add amber, neon color, gradients, distracting animation, or gaming-style effects.
- **Don't** use Consolas for general UI or headings.
- **Don't** introduce drop shadows or extra card layers where tonal contrast and borders suffice.
- **Don't** replace the shared scrollbar with competing per-window treatments or hide experimental status below the initial Settings fold.
- **Don't** show a generic numeric-error message that leaves the operator to locate the invalid field manually.
