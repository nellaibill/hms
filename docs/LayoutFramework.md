# HMS Layout Framework Specification

## Purpose
This document defines the structural layout framework for the HMS — regions, dimensions, stacking order, and breakpoint behavior — so every screen in the [Screen Inventory](ScreenInventory.md) is assembled from a consistent spatial frame rather than one-off page layouts. It is a pure layout specification; visual styling (color, typography, elevation treatment) is covered separately in [DesignSystem.md](DesignSystem.md).

## Scope
Covers Top Navigation, Sidebar, Content Area, Breadcrumb, Page Header, Quick Actions, Floating Panels, Notification Drawer, Task Drawer, Footer, Sticky Header behavior, z-index stacking, and responsive layout rules across Tablet, Laptop, Desktop, Large Display, and multi-monitor setups.

**Out of scope:** color, typography styling, and component visual treatment (see [DesignSystem.md](DesignSystem.md)); navigation content/hierarchy (see [InformationArchitecture.md](InformationArchitecture.md)).

## When to Update This Document
- Whenever a region's dimensions or breakpoint behavior changes.
- Whenever a new floating/overlay element (drawer, panel) is introduced.
- Whenever a new device class or display size needs to be supported.

## Recommended Sections
- Desktop Layout Structural Diagram
- Top Navigation, Sidebar, Secondary Navigation, Content Area, Breadcrumb, Page Header, Quick Actions
- Floating Panels, Notification Drawer, Task Drawer, Dialogs (stacking reference)
- Sticky Header Behavior & z-index Stacking Ladder
- Footer
- Responsive Layout by Device Class
- Multi-Monitor Optimization

---

## Desktop Layout — Structural Diagram

```
┌───────────────────────────────────────────────────────────────────────────┐
│ TOP NAVIGATION BAR                                              h: 56px   │
│ [Logo 160px][Global Search 240–480px]        [Utility icons][Avatar 40px]│
├────────────┬────────────────────────────────────────────────────────────┤
│            │ BREADCRUMB                                       h: 32px   │
│            ├────────────────────────────────────────────────────────────┤
│  SIDEBAR   │ PATIENT CONTEXT BAR (conditional)                h: 48px   │
│  72/240px  ├────────────────────────────────────────────────────────────┤
│            │ PAGE HEADER                                      h: 64–88px│
│ height:    ├────────────────────────────────────────────────────────────┤
│ 100vh−56px │ QUICK ACTIONS (optional)                         h: 48px   │
│            ├────────────────────────────────────────────────────────────┤
│            │                                                            │
│            │ MAIN CONTENT AREA (scrolls independently)                 │
│            │ 12-col grid · max-width 1440px · padding 24px             │
│            │                                                            │
│            ├────────────────────────────────────────────────────────────┤
│            │ FOOTER                                           h: 40px   │
└────────────┴────────────────────────────────────────────────────────────┘
```

Floating elements (drawers, panels, dialogs) render as overlays above this frame — detailed in the Floating Panels / Drawer sections below.

---

## 1. Top Navigation

- **Height:** 56px, full viewport width, fixed to `top: 0`.
- **Layout, left to right:**
  - Logo/brand lockup — 160px width, 16px left padding, vertically centered.
  - Vertical divider.
  - Global Search field — min-width 240px, max-width 480px, height 40px, 24px left margin from divider, flexible width between breakpoints.
  - Flex spacer (absorbs remaining width).
  - Utility icon cluster (right-aligned): Calendar, Calculator, Notifications, Pending Tasks, Language selector — each a 40×40px hit target, 8px gap between icons.
  - User profile control — 40px avatar + 8px gap + 16px chevron, 16px right padding from viewport edge.
- **z-index:** 1000 (topmost persistent layer).

## 2. Sidebar (Primary Navigation)

- **Position:** fixed left, `top: 56px`, `height: calc(100vh - 56px)`.
- **Collapsed width:** 72px — icon-only, 24px icon centered, 56px row height per domain item, label shown as hover tooltip.
- **Expanded width:** 240px — 24px icon + 12px gap + 14px label text, 44px row height per item.
- **Toggle control:** 40×40px, pinned at bottom of sidebar.
- **Active-item indicator:** 3px accent bar on the left edge of the row (positional, not color-defined here).
- **Nested items** (when a domain is expanded inline rather than via a separate secondary column): 16px indent per level, 36px row height.
- **z-index:** 900.

## 3. Secondary Navigation

Two layout patterns, chosen per module depth (per IA §Secondary Navigation):

| Pattern | Dimensions | Used when |
|---|---|---|
| Horizontal tab bar | 48px height, sits directly under Page Header | Shallow module sets (e.g., OPD / IPD / OT under Clinical Care) |
| Secondary column | 200px width, full content height, own vertical scroll | Deep hierarchies (Settings master data, E-MRD folder tree) |

## 4. Content Area

- **Width:** `100% − sidebar width` (72px or 240px).
- **Max content width:** 1440px, centered with auto margins beyond that (prevents excessive line-length/scan-distance on large displays — see Responsive Layout section).
- **Padding:** 24px (desktop/laptop) · 16px (tablet) · 16px (mobile).
- **Internal vertical stack, top to bottom:** Breadcrumb → Patient Context Bar (conditional) → Page Header → Quick Actions (conditional) → Main scrollable content → Footer.
- **Vertical rhythm inside main content:** 24px between major sections, 16px between related cards, per Design System `space-06`/`space-05`.

## 5. Breadcrumb

- **Height:** 32px, spans full content-area width.
- **Padding:** 8px vertical, 24px horizontal (matches content padding).
- **Segment spacing:** 8px between label and separator; 16px separator icon.
- **Truncation:** below 768px width, collapse middle segments to "…", always keeping Home + last 2 segments visible; minimum 32px tap target per segment.

## 6. Page Header

- **Height:** 64px default; grows to 88px when a subtitle/description line is present.
- **Layout:** Page title left-aligned; optional subtitle below it; action button cluster right-aligned (40px height buttons, 12px gap between).
- **Padding:** 16px top/bottom, 24px horizontal (matches content padding).
- **Divider:** full-width rule below the header.

## 7. Quick Actions

- **Height:** 48px, optional row directly beneath Page Header.
- **Layout:** horizontal button/chip row, 8px gap, left-aligned by default (utility actions like Export/Print may right-align).
- **Overflow:** horizontal scroll with fade-edge if items exceed container width (same rule as Tabs overflow in the Design System).

## 8. Floating Panels

Lightweight, anchored utility popups (Calculator, Calendar quick view, info tooltips) — distinct from full Drawers (§9–10).

| Property | Value |
|---|---|
| Anchor | 8px below the triggering top-nav icon |
| Calculator panel | 280 × 360px, fixed |
| Calendar quick view | 320 × 360px, fixed |
| Collision handling | flips to the left if it would overflow the right viewport edge; 8px minimum margin maintained |
| z-index | 1100 |
| Dismissal | click-outside or Escape |

## 9. Notification Drawer

- **Slide-in edge:** right, full height (`top: 56px` to `bottom: 0`, or full `100vh` with its own header replicating a close control).
- **Width:** 400px (desktop/laptop) · 360px (tablet) · 100vw (mobile, full-screen).
- **Internal structure:**
  - Drawer header — 56px (title + 40×40px close button).
  - Category filter tabs — 48px.
  - Scrollable list — each row min-height 72px (24px icon + text block + timestamp).
  - Drawer footer — 48px ("Mark all as read").
- **z-index:** 1200.
- **Behavior:** overlays the content area only (sidebar and top nav remain visible/interactive); does not reflow or resize the content area underneath.

## 10. Task Drawer (Pending Tasks)

- Shares the **same right-edge drawer slot and dimensions** as the Notification Drawer (400/360/100vw) rather than a second competing drawer — a segmented control in the shared drawer header switches between "Notifications" and "Tasks," so only one drawer instance ever occupies the edge at a time.
- **Task row:** min-height 80px (icon + title + due-context text + inline 32px-height action button, e.g. "Approve"/"Review").
- **z-index:** 1200 (same layer as Notification Drawer, since they're mutually exclusive views of one drawer).

## 11. Dialogs (stacking reference)

Per Design System §17 sizing (Small/Medium/Large/Fullscreen) — included here only for the z-index ladder below.

## 12. Sticky Header Behavior

- **Top Navigation:** always sticky, `top: 0`, z-index 1000.
- **Breadcrumb + Patient Context Bar:** stick together as one unit directly below the top nav (`top: 56px`); combined stuck height is 32px alone, or 80px when the Patient Context Bar is present.
- **Page Header:** sticky only on long-scroll screens (long forms, large tables) — sticks below the breadcrumb/context region, at `top: 88px` (no patient context) or `top: 136px` (with patient context).
- **Table headers:** sticky within their own scroll container only (not page-level) — see Design System §11.
- **Footer:** not sticky by default; becomes sticky only on data-entry-heavy screens (e.g., multi-step Registration) where it hosts persistent Save/Cancel actions.

**z-index stacking ladder (highest to lowest):**

| Layer | z-index |
|---|---|
| Dialog / Modal | 1400 |
| Notification / Task Drawer | 1200 |
| Floating Panel | 1100 |
| Top Navigation | 1000 |
| Sidebar | 900 |
| Sticky Page Header / Breadcrumb region | 800 |
| Page content | auto (0) |

## 13. Footer

- **Height:** 40px, sits at the bottom of the content area (beside the sidebar, not a full-viewport footer).
- **Layout:** left = version/build text; right = help/support link + environment tag.
- **Padding:** 8px vertical, 24px horizontal.

---

## 14. Responsive Layout by Device Class

Builds on the Design System's breakpoint scale (`xs` <480 / `sm` 480–768 / `md` 768–1024 / `lg` 1024–1440 / `xl` ≥1440), mapped to the actual device contexts clinical staff use:

### Tablet (768–1024px — bedside / ward rounds)
- Sidebar auto-collapses to 72px icon rail; no auto-expand.
- Secondary column nav (§3) converts to the horizontal tab-bar pattern to conserve width.
- Patient Context Bar grows to 56px height with a 40px avatar (larger touch target).
- Notification/Task Drawer narrows to 360px.
- All touch targets increase to 44px minimum (vs. 40px on desktop/laptop).

### Laptop (1024–1440px)
- Standard frame as diagrammed above.
- Sidebar **defaults to collapsed (72px)** to preserve content width on 13"–14" screens; user can manually pin it expanded (240px), and that preference persists per-user.

### Desktop Monitor (1440–1920px)
- Sidebar **defaults to expanded (240px)**.
- Content max-width remains 1440px, centered — leaves margin on 16:9 monitors rather than stretching content edge-to-edge.
- Secondary column nav pattern (200px) is used in place of tab bars wherever width allows, since it's available without crowding.

### Large Display (≥1920px — 27"+ monitors, control-room/admin dashboards)
- Content still caps at 1440px max-width by default.
- Dashboard/Reports screens may opt into a **wide layout** (up to 1800px max-width, 16-column grid instead of 12) to use the extra space for side-by-side widgets.
- Forms remain capped at 720px max-width regardless of monitor size — wide forms hurt scan-line accuracy and are never widened just because screen space allows it.

## 15. Multi-Monitor Optimization

- All floating elements (drawers, panels, dialogs) render within the triggering browser window only — never assumed to span or target a second monitor automatically.
- **Pop-out windows:** designated screens (Patient 360 View, OPD Consultation Workspace, OT Schedule) expose an "Open in new window" quick action, launching a secondary browser window at a default 1280×800px — supporting the common two-monitor nursing-station/OPD desk setup (chart on one screen, order entry on the other).
- **State sync:** a popped-out window and its parent share live state (e.g., an order placed in the pop-out reflects immediately in the parent Dashboard); each window traps its own dialog focus independently.
- **Window persistence:** a popped-out window's position/size is remembered per-user and restored on next login, rather than resetting to the 1280×800px default every time — reduces setup friction on fixed dual-monitor desks.

---

## Summary
This layout frame is device-adaptive but structurally constant — the same six regions (Top Nav, Sidebar, Breadcrumb, Page Header, Content, Footer) exist at every breakpoint; only their width, density, and pattern (tab bar vs. column) change.
