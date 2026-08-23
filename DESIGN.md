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

Background Automation Studio is a dense but calm Windows operator workspace. Graphite surfaces keep target context, transport controls, project state, and the workflow editor visible together; darker accessible blue is reserved for primary action, keyboard focus, and live execution; red is reserved for recording, stop-record emphasis, and errors.

The hierarchy follows the work rather than decoration. Selection and current execution are visibly different states, important fields have visible names, and the empty editor provides authored next-step guidance instead of a blank canvas.

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

**The Native Readability Rule.** Keep ordinary UI in Segoe UI. Consolas communicates scripts, identifiers, time, or validation output—not brand personality.

## Layout

The main window opens at 1280×820 with a 1024×680 minimum. A 42px menu/product bar and 34px status bar frame the work area. Inside an 18px margin, a fixed 350px context-and-transport column sits 18px from the flexible workflow editor. The left column scrolls vertically when required; additional width belongs to the editor.

Major panels use 18px internal padding. Spacing follows a 4/8/12/16/24/32 family, with 14px and 18px used where the dense desktop grid needs intermediate separation. The action editor is a focused 470px owner-centered dialog, sized to content and capped at 720px high.

Workflow rows scan left-to-right as play marker, number, enabled checkbox, action type, summary, and current-state text. The empty list centers an authored title and specific guidance for recording or adding an action manually.

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
- **Internal Padding:** 18px for panels; 8–12px for compact state containers.

### Inputs / Fields

- **Style:** Dark field surface, near-white text, one-pixel boundary, blue caret, and 10px by 8px padding.
- **Focus:** Fields shift to an Operator Blue border; buttons and rows use a brighter two-pixel focus border.
- **Names:** Visible labels name X, Y, text, shortcut, wait duration, and delay. Automation names add units and target-client context.
- **Validation:** Script errors appear below the editor in a bordered dark-red surface with light rose Consolas text.

### Navigation

The native menu bar exposes File and Edit commands and visible New/Open/Save gestures. Text tabs use muted inactive labels, graphite hover, and a selected raised surface with near-white text and a blue underline. The bottom status bar stays quiet and persistent.

### Workflow Rows

Rows use a 10px corner and compact six-part scan order. Disabled rows use 46% opacity. Selection uses Selected Graphite and a neutral border. Keyboard focus uses the two-pixel Focus Ice outline. Current execution overrides the selected fill with Execution Blue and adds both a left play marker and right-aligned “CURRENT” label.

### Recording Status

The recording banner uses a dark red surface and muted red border. An eight-pixel red dot, “RECORDING,” and a Consolas elapsed-time value make the state readable without color alone.

### Playback Compatibility

Settings presents three full-width, text-led choices: Strict background, Modern controls that may take focus, and Classic Win32 messages. Each choice includes its operational consequence, uses the established graphite selection treatment, and remains keyboard focusable. Strict background is the default and never calls focus-taking UI Automation patterns. The Modern option is explicitly labeled as potentially interrupting typing and IME composition. Compatibility copy distinguishes a covered window from minimized and fully hidden states so the interface does not promise universal hidden-window automation.

During a run, the activation shield prevents ordinary target activation and the status bar distinguishes focus-safe semantic commands, Win32 fallback, and explicitly requested focus-unsafe UI Automation. Playback never restores the target's recorded desktop position. A visible target can be dragged while running because every action resolves its client-relative coordinate against the current window position.

### Editor History

Undo and Redo are compact neutral controls beside Add action and are repeated in the Edit menu. They use standard Windows gestures: `Ctrl+Z` for Undo, `Ctrl+Y` and `Ctrl+Shift+Z` for Redo. History covers visual edits, script edits, reordering, deletion, clearing, and completed recording batches while preserving stable action IDs. Running and recording temporarily disable history navigation.

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
- **Do** keep the workflow editor dominant while target and transport context remain available.
- **Do** use concise empty-state and helper copy that tells the operator what to do next.

### Don't:

- **Don't** use blue for ordinary row selection; gray is selection while blue is current execution.
- **Don't** communicate recording with a red dot alone; retain the label and timer.
- **Don't** add amber, neon color, gradients, distracting animation, or gaming-style effects.
- **Don't** use Consolas for general UI or headings.
- **Don't** introduce drop shadows or extra card layers where tonal contrast and borders suffice.
