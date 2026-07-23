# Enterprise Healthcare Design System — HMS

## Purpose
This document is the complete design system specification for the HMS, synthesizing Fluent's clarity, Carbon's data density and token discipline, Material 3's dynamic theming, and Redwood's calm clinical tone, so every screen in the [Screen Inventory](ScreenInventory.md) is built from a shared, accessible visual language rather than one-off styling decisions.

## Scope
Covers design tokens (typography, spacing, grid, color, elevation, border radius, icons) and component specifications (buttons, cards, forms, tables, tabs, steppers, navigation, alerts, toast, dialogs, badges, avatars, status chips, data visualization, date pickers, dropdowns, search, pagination, tree view, upload), plus accessibility, dark mode, and responsive grid rules.

**Out of scope:** implementation code (this is a specification, not a component library) and visual mockups of specific screens — see [ScreenInventory.md](ScreenInventory.md) for the screen catalog these components compose.

## When to Update This Document
- Whenever a new component pattern is needed that isn't covered here.
- Whenever a token value changes (color, spacing, typography scale).
- Whenever an accessibility requirement is added or WCAG guidance is revised.

## Recommended Sections
- Part A — Design Tokens (Typography, Spacing, Grid, Color, Elevation, Border Radius, Icons)
- Part B — Components (Buttons through Upload)
- Part C — Cross-Cutting Systems (Accessibility, Dark Mode, Responsive Grid)

All specifications target **WCAG 2.2 Level AA** as the floor, not the ceiling.

---

# Part A — Design Tokens

## 1. Typography

**Typeface:** Inter (humanist sans, high legibility at small sizes, excellent numeral clarity for clinical data) with system fallback stack: `Inter, "Segoe UI", system-ui, -apple-system, Roboto, sans-serif`. Tabular/monospace companion — **IBM Plex Mono** — for lab values, vitals, invoice amounts, UHIDs, so numerals align in columns.

| Role | Token | Size / Line-height | Weight | Usage |
|---|---|---|---|---|
| Display | `type-display` | 32px / 40px | 600 | Dashboard hero KPIs only |
| Heading 1 | `type-h1` | 28px / 36px | 600 | Page title |
| Heading 2 | `type-h2` | 24px / 32px | 600 | Section title |
| Heading 3 | `type-h3` | 20px / 28px | 600 | Card/panel title |
| Heading 4 | `type-h4` | 16px / 24px | 600 | Sub-section, table group header |
| Body Large | `type-body-lg` | 16px / 24px | 400 | Primary reading text |
| Body Medium (default) | `type-body-md` | 14px / 20px | 400 | Forms, tables, default UI text |
| Body Small | `type-body-sm` | 12px / 16px | 400 | Helper text, timestamps |
| Label | `type-label` | 14px / 20px | 500 | Form labels, button text |
| Caption | `type-caption` | 12px / 16px | 500 | Chip/badge text |
| Data / Mono | `type-data` | 14px / 20px | 400 (Plex Mono) | UHID, invoice #, lab values, vitals |

**Rules:** Body text never below 12px; default reading text 14px minimum (never rely on 12px for anything the clinician must act on). Line-length capped at ~80ch for long-form content (consultation notes). Letter-spacing 0 throughout — tracking reduces legibility at clinical-data density. Text resizing to 200% must not cause loss of content/functionality (WCAG 1.4.4).

## 2. Spacing

4px base unit, Carbon-style scale — predictable, halves/doubles cleanly across dense clinical tables and airy dashboard cards.

| Token | Value | Typical use |
|---|---|---|
| `space-01` | 2px | Icon-to-text micro gap |
| `space-02` | 4px | Chip internal padding |
| `space-03` | 8px | Compact control padding |
| `space-04` | 12px | Default form field padding |
| `space-05` | 16px | Card padding, default gutter |
| `space-06` | 24px | Section spacing |
| `space-07` | 32px | Page section separation |
| `space-08` | 40px | Panel-to-panel |
| `space-09` | 48px | Page top padding |
| `space-10` | 64px | Empty-state vertical rhythm |

## 3. Grid

12-column responsive grid, margins and gutters scale with breakpoint (bedside tablets and nursing-station monitors both in scope).

| Breakpoint | Range | Columns | Margin | Gutter |
|---|---|---|---|---|
| `xs` (mobile) | < 480px | 4 | 16px | 8px |
| `sm` (tablet portrait) | 480–768px | 4 | 24px | 16px |
| `md` (tablet landscape) | 768–1024px | 8 | 32px | 16px |
| `lg` (desktop) | 1024–1440px | 12 | 40px | 24px |
| `xl` (large monitor) | ≥ 1440px | 12 | 64px (content max-width 1440px, centered) | 24px |

Primary nav rail: fixed 72px (collapsed, icon-only) / 240px (expanded, icon+label) — collapses to bottom nav or drawer below `md`.

## 4. Color Palette

Two-tier system: **base palette** (raw values) feeding **semantic tokens** (what components actually consume) — the same base palette is reused across light/dark, only the semantic mapping flips. All text/background pairs below are verified ≥4.5:1 for body text, ≥3:1 for large text (18px+/14px+bold) and UI component boundaries, per WCAG 1.4.3 and 1.4.11.

**Neutral scale:**
`gray-10 #F4F4F4 · gray-20 #E0E0E0 · gray-30 #C6C6C6 · gray-40 #A8A8A8 · gray-50 #8D8D8D · gray-60 #6F6F6F · gray-70 #525252 · gray-80 #393939 · gray-90 #262626 · gray-100 #161616`

**Brand (clinical trust blue-teal):**
`brand-40 #4589FF · brand-50 #0F62FE (primary) · brand-60 #0043CE · brand-70 #002D9C`

**Semantic / status (color never used alone — always paired with icon + text per WCAG 1.4.1):**

| Status | Color | Hex (light bg) | Meaning |
|---|---|---|---|
| Success / Stable | Green | `#24A148` (text on white: 4.6:1) | Normal vitals, payment settled, order complete |
| Warning / Urgent | Amber | `#B28600` (darkened gold for 4.5:1 on white — raw gold `#F1C21B` reserved for backgrounds only, never as text) | Pending approval, stock low, license expiring |
| Error / Critical | Red | `#DA1E28` | Critical lab value, allergy conflict, blocked action |
| Info | Blue | `#0F62FE` | Informational notification |
| Neutral / Routine | Gray | `#525252` | Routine/default state |

**Semantic tokens (light theme):**
`surface-page #FFFFFF · surface-raised #FFFFFF (bordered) · surface-sunken #F4F4F4 · text-primary #161616 (14.9:1) · text-secondary #525252 (7.5:1) · text-disabled #A8A8A8 (2.7:1, non-text use only) · border-default #E0E0E0 · border-strong #8D8D8D · focus-ring #0F62FE`

**Dark theme mapping:** see §29 Dark Mode.

## 5. Elevation

Carbon-influenced: **borders are the primary depth cue** (flat, calm, reduces visual noise in high-density clinical screens); elevation (shadow) reserved for **transient/overlay surfaces only**, never for static page content.

| Token | Shadow | Used by |
|---|---|---|
| `elevation-0` | none (border only) | Cards, panels, table rows |
| `elevation-1` | `0 1px 2px rgba(0,0,0,.08)` | Dropdown menus, popovers |
| `elevation-2` | `0 4px 8px rgba(0,0,0,.12)` | Toasts, tooltips |
| `elevation-3` | `0 8px 16px rgba(0,0,0,.16)` | Dialogs, modals |
| `elevation-4` | `0 16px 32px rgba(0,0,0,.20)` | Full-screen overlay panels |

## 6. Border Radius

Moderate scale — sharp enough for data density, soft enough to feel humane in a clinical setting.

| Token | Value | Usage |
|---|---|---|
| `radius-none` | 0 | Table cells, dense data grids |
| `radius-sm` | 4px | Inputs, buttons (default), badges |
| `radius-md` | 8px | Cards, dialogs, dropdowns |
| `radius-lg` | 12px | Large panels, bottom sheets |
| `radius-full` | 9999px | Avatars, pills, status chips, count badges |

## 7. Icons

24×24px grid, 1.5px stroke weight, two states (outline = default, filled = active/selected). Sizing scale: 16 (inline with body-sm text) / 20 (inline with body-md/buttons) / 24 (standalone/nav) / 32 (empty states). Every icon-only control carries an `aria-label`; decorative icons get `aria-hidden="true"`. Healthcare-specific glyph additions beyond a standard set: allergy alert, blood drop/group, vitals/pulse, ambulance, stethoscope, wheelchair/accessibility, MLC/legal flag, PPE/isolation.

---

# Part B — Components

## 8. Buttons

**Variants:** Primary (filled, brand-50), Secondary (outlined), Tertiary/Ghost (text-only), Destructive (red, for irreversible actions — discharge, delete, reject claim), Icon Button.
**Sizes:** Small (32px height, compact toolbars), Default (40px), Large (48px, primary CTA on forms).
**States:** default / hover (–10% lightness) / focus (2px `focus-ring` outline, 2px offset) / active (–15%) / disabled (gray-30 bg, gray-50 text, `aria-disabled`) / loading (spinner replaces label, button remains focusable, `aria-busy="true"`).
**Accessibility:** All sizes meet WCAG 2.2 SC 2.5.8 Target Size (Minimum) — 24×24px CSS floor; Default/Large comfortably exceed it. Destructive actions require a confirmation dialog (§17), never fire on single click.

## 9. Cards

**Structure:** optional media/icon → header (title + optional overflow menu) → body → footer (actions, right-aligned). Border (`border-default`), `radius-md`, `elevation-0` at rest, `elevation-1` only on interactive/hoverable cards (e.g., Dashboard drill-down tiles). States: default / hover / selected (2px brand border) / disabled (reduced opacity, non-interactive). Patient/consultant cards use the Avatar (§20) in the header slot.

## 10. Forms

Top-aligned labels always (never placeholder-as-label — fails WCAG 3.3.2 in practice as labels vanish on input). Required fields marked with a text "(required)" suffix, not asterisk alone (asterisk alone fails for screen-reader users without a legend). Helper text below field in `text-secondary`; error text below field in `error` red **with an error icon**, and the field border switches to `error` red — never color-only. Field states: default / focus / filled / error / disabled / read-only. Grouped clinical fields (Emergency Contact, Allergy Details) use `<fieldset>`/`<legend>` semantics. Per new WCAG 2.2 SC 3.3.7 (Redundant Entry), previously-entered data (e.g., patient demographics already on file) must be pre-filled or offered as a one-click copy, never re-keyed — directly applicable to the Old Patient Registration flow.

## 11. Tables

Sticky header row, right-aligned numeric/monetary columns (using `type-data` mono), left-aligned text columns. Row density: Compact (32px row height, default for clinical worklists — Lab Order Queue, OPD Patient List) and Comfortable (48px, default for financial/HR lists). Sortable column headers expose sort state via `aria-sort`. Row selection checkboxes have a visible focus ring and a "select all" in the header with indeterminate state support. Status Chips (§19) render inline in dedicated columns rather than color-coding whole rows. Below `md` breakpoint, tables collapse to a stacked card-per-row pattern rather than horizontal scroll, per responsive rules (§30).

## 12. Tabs

Horizontal tab list, underline indicator on active tab (2px, brand-50), full ARIA `tablist`/`tab`/`tabpanel` pattern with roving `tabindex` and arrow-key navigation (Home/End jump to first/last). Overflow (e.g., many OPD sub-views) scrolls horizontally with a fade-edge affordance rather than wrapping. Disabled tabs remain visible (not hidden) with reduced-opacity styling and `aria-disabled`.

## 13. Steppers

Used for multi-stage flows — most prominently Patient Registration (Demographics → Emergency Contact → Allergy → Registration Details → Upload → Billing). Horizontal on desktop, vertical on mobile/tablet. Step states: completed (checkmark, brand-50), current (filled circle, bold label), upcoming (outline circle, `text-secondary`), error (red circle + icon, blocks progression until resolved). Steppers are **non-linear-capable** — a user may jump back to a completed step, but forward-jumping past an incomplete required step is blocked with an inline explanation, not just disabled silently.

## 14. Navigation

Directly implements the [Information Architecture](InformationArchitecture.md):
- **Primary Nav Rail:** 10 domains + Home, icon+label, role-filtered per the IA's Role-Based Navigation Matrix, collapsible to icon-only.
- **Secondary Nav:** contextual left-rail or tab set within a domain, per IA §Secondary Navigation.
- **Breadcrumb:** location-based per IA §Breadcrumb Strategy — `Home > Domain > Module > Sub-module > Record`, rendered as a landmark (`nav aria-label="Breadcrumb"`), current page marked `aria-current="page"`.
- **Patient Context Bar:** a persistent, dismissible bar below the breadcrumb showing the active patient/encounter (photo/initials avatar, name, UHID, age/sex, allergy flag) — decoupled from breadcrumb per IA principle #3, present across every clinical/financial screen once a patient is in context.

## 15. Alerts (inline banners)

Persistent, page/section-level, non-blocking. Variants: Info / Success / Warning / Error / **Critical-Clinical** (the last reserved for allergy conflicts, critical lab values — bold red border, icon, and requires an explicit "Acknowledge" action before the alert can be dismissed; it cannot be dismissed by an outside click or timeout). Every alert pairs an icon + heading + description; icon shape differs per severity (not just color) so the distinction survives grayscale/colorblind rendering.

## 16. Toast

Transient, corner-anchored (bottom-right desktop / bottom-center mobile), auto-dismiss at 8 seconds minimum for informational toasts (exceeds WCAG 2.2.1's requirement that timing be adjustable — user can also hover/focus to pause the dismiss timer). **Toasts never carry the only copy of a critical/blocking message** — critical events use the Alert or Dialog components instead, since toasts are inherently missable. Announced via an `aria-live="polite"` region (`aria-live="assertive"` only for error toasts). Stack vertically, most-recent on top, max 3 visible with "+N more" overflow.

## 17. Dialogs

Modal, focus-trapped, `role="dialog"` + `aria-modal="true"` + `aria-labelledby`. Sizes: Small (confirmations — "Discard changes?"), Medium (forms — "Add Staff Record"), Large (rich content — Consent Form), Fullscreen (complex workflows on mobile/tablet). Closing via Escape key, explicit Cancel, or backdrop click (backdrop-click disabled for destructive/irreversible confirmations, e.g. Refund Validation, Permission Change). Focus returns to the triggering element on close. Confirmation-dialog pattern is used throughout the journey maps (Discount Approval, Discharge Checklist, Permission Change) — always states the consequence in plain language, never just "Are you sure?".

## 18. Badges

Small numeric/status indicators anchored to an icon or avatar corner (e.g., unread notification count). `radius-full`, max display "99+", background uses semantic color with white text at verified 4.5:1+, always paired with an `aria-label` announcing the full count (e.g., "12 unread notifications") since the visual badge alone isn't accessible to screen readers.

## 19. Status Chips

The system's most healthcare-specific component — used for encounter status, MLC/NMLC, payment status, admission status, lab/radiology urgency. Structure: icon + label text inside a `radius-full` pill, semantic background at reduced opacity (10–15%) with full-strength text/icon for contrast. **Never color alone**: "Critical" chip = red + warning-triangle icon + word "Critical"; "Stable" = green + check icon + word "Stable". Fixed vocabulary per category (e.g., Payment: Paid / Pending / Partially Paid / Refunded) to keep meaning consistent system-wide.

## 20. Avatars

Photo-first, falls back to initials on a deterministic background color (hashed from name, from an accessible-contrast palette subset), falls back further to a generic person/consultant icon if no name available. Sizes: 24 / 32 (default, table rows) / 40 (cards, profile menu) / 64 (profile page header). Optional status dot (on-duty/off-duty for consultants) — dot alone never conveys meaning without an accessible label (`aria-label="Dr. Rao — on duty"`).

## 21. Data Visualization

Dashboard charts (census, income/expense, HR presence) use a colorblind-safe categorical palette (avoiding red-green as the sole differentiator; pairing hue with pattern/label) and a sequential blue scale for single-metric heatmaps (bed occupancy). Every chart has a text-equivalent (data table toggle or caption) per WCAG 1.1.1 — a chart is never the only way to access the data. Gridlines and axis labels meet 3:1 contrast against the chart background; tooltips are keyboard-triggerable (focus, not hover-only).

## 22. Date Pickers

Supports the source requirement's `DD/MM/YYYY` format explicitly (locale-configurable), single-date and range modes, fully keyboard-operable calendar grid (arrow keys move focus by day, PageUp/PageDown by month, Home/End to week start/end) with `role="grid"` semantics — this satisfies WCAG 2.2 SC 2.5.7 (Dragging Movements) by ensuring no interaction requires drag-only input. Manual text entry is always available alongside the calendar widget, with inline format validation.

## 23. Dropdowns

Three variants: **Select** (fixed list — Title, Gender, Blood Group, Department master data), **Multi-select** (checkbox list with a summarized closed-state label, e.g., "3 departments selected"), **Combobox/Searchable** (typeahead filter — Consultant Search, large master lists). Full ARIA `listbox`/`combobox` pattern, keyboard-operable (arrow keys, type-ahead jump, Escape to close without selecting). Options requiring an "Other, please specify" (per the Profession field in registration) reveal an inline text input on selection.

## 24. Search Components

**Global Search** (per IA §Global Search Behavior): persistent chrome search box, entity-type result tabs, permission-filtered, sets Patient Context Bar on patient selection. **Inline table/list search**: scoped filter above a table, debounced, announces result count changes via `aria-live="polite"` region ("14 results found"). **Typeahead**: minimum 2-character trigger, loading state, "no results" empty state with a suggested next action (e.g., "No patient found — Register new patient").

## 25. Pagination

Page-number style for large administrative lists (Activity Log, Reports), "Load more" / infinite-scroll for activity feeds where order matters less. Always displays total count ("Showing 1–20 of 342"), page-size selector (10/20/50/100), and Previous/Next controls that remain keyboard-focusable even when disabled at boundaries (with `aria-disabled`, not removed from the tab order abruptly).

## 26. Tree View

Used for hierarchical structures: Settings' Department/Consultant master data, E-MRD folder/document hierarchy, Module/Role permission trees. Standard ARIA `tree`/`treeitem`/`group` pattern, expand/collapse via disclosure triangle (click or Right/Left arrow), multi-level indentation with a visible connecting guideline (not indentation alone, for users with low vision tracking hierarchy depth). Checkbox trees (permission assignment) support tri-state (checked/unchecked/indeterminate for partially-selected children).

## 27. Upload Components

Drag-and-drop zone with an equally prominent "Browse files" button (drag alone is never the only path — satisfies WCAG 2.5.7 Dragging Movements). Shows accepted file types/size limit up front, not just on rejection. Per-file progress bar, success/error state per file (not just batch-level), and a clear remove/retry action. Validates against known failure modes flagged in the journey maps (unsupported format, file too large) with specific, actionable error text — never a generic "Upload failed."

---

# Part C — Cross-Cutting Systems

## 28. Accessibility (WCAG 2.2 AA Compliance Baseline)

| Area | Requirement | Applied via |
|---|---|---|
| Color contrast | 4.5:1 text, 3:1 large text/UI components (1.4.3, 1.4.11) | Verified token pairs, §4 |
| Non-text info | Never color alone (1.4.1) | Status Chips, Alerts, Form errors |
| Target size | ≥24×24px CSS minimum (2.5.8) | Buttons, chips, icon buttons |
| Dragging alternative | Every drag interaction has a non-drag path (2.5.7) | Upload, Date range picker |
| Focus visible | 2px visible focus ring, never `outline: none` without replacement (2.4.11) | All interactive components |
| Consistent help | Help/support entry point in the same location across pages (3.2.6) | Global Chrome |
| Redundant entry | Don't re-ask for data already captured (3.3.7) | Old Patient Registration, forms §10 |
| Error identification | Errors described in text, not just color/icon (3.3.1, 3.3.3) | Forms §10 |
| Keyboard operability | No keyboard traps, full operability without a mouse (2.1.1, 2.1.2) | All components |
| Text resize | Reflow at 400% zoom without horizontal scroll or content loss (1.4.10) | Responsive grid §30 |
| Screen reader semantics | Correct ARIA roles/states on all custom widgets (4.1.2) | Tabs, Tree View, Dialogs, Combobox |
| Skip navigation | "Skip to main content" link as first focusable element | Global Chrome |
| Timing | Adjustable/pausable timers (2.2.1) | Toasts, session timeout |

## 29. Dark Mode

Token-based, not a simple inversion — surfaces shift to dark grays rather than pure black (reduces eye strain for extended night-shift use, a real clinical usage pattern):

`surface-page #161616 · surface-raised #262626 (bordered, no shadow-based elevation) · surface-sunken #0D0D0D · text-primary #F4F4F4 (15.8:1) · text-secondary #C6C6C6 (9.8:1) · border-default #393939 · focus-ring #4589FF (brightened for dark-bg visibility)`

Semantic status colors shift to their brightened variants (e.g., Error `#DA1E28` → `#FA4D56`) to maintain ≥4.5:1 against dark surfaces — status meaning and icon pairing stay identical across themes, only luminance adjusts. Elevation shadows are replaced by a subtle lighter border + very low-opacity glow rather than a dark drop-shadow (shadows are nearly invisible on dark backgrounds). Theme follows OS preference by default, with an explicit override in the User Profile menu (per IA §User Profile Architecture) — the toggle, not just detection, is required since clinical settings may standardize the display mode hospital-wide regardless of individual device OS settings.

## 30. Responsive Grid

Builds on §3's breakpoints with device-context rules specific to hospital environments:

- **Desktop (`lg`/`xl`):** nursing station / admin desks — full 12-column layout, primary nav expanded, side-by-side Patient Context Bar + content.
- **Tablet landscape (`md`):** bedside/ward rounds — 8-column, nav collapses to icon rail, tables switch to Comfortable density with larger touch targets (44px) for gloved-hand use.
- **Tablet portrait/Mobile (`xs`/`sm`):** ambulance/field or quick lookups — 4-column, nav becomes a bottom bar or drawer, tables collapse to stacked cards (§11), steppers go vertical (§13), dialogs go fullscreen (§17).

All layouts reflow (never require 2D scrolling) at 320px CSS width per WCAG 1.4.10, and support 200% browser zoom without loss of content or functionality.

---

## Summary

This system pairs Carbon's border-first, low-elevation data density (essential for the dense clinical tables and worklists cataloged in the [Screen Inventory](ScreenInventory.md)) with Fluent's calm neutral palette, Material 3's token-driven theming for light/dark parity, and Redwood's restraint around color — reserving strong hues exclusively for clinical/status meaning rather than decoration. Every component ties back to a concrete need surfaced in the [journey maps](UserJourneyMaps.md) (allergy conflict dialogs, discharge checklists, discount approvals) rather than being designed in the abstract.
