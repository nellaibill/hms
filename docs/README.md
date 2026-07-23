# Documentation Index

## Purpose
This document is the entry point into the `docs/` folder. It exists so anyone — new team member or stakeholder — can find the right document without knowing the full folder contents in advance.

## Scope
Covers an index of every document in `docs/` with a one-line description of what it's for.

**Out of scope:** the actual content of each topic — that lives in the linked document.

## When to Update This Document
- Whenever a document is added, removed, or renamed under `docs/`.

## Recommended Sections
- Index grouped by category (Architecture & Structure, Standards & Conventions, Engineering Practices, Security & Access, Operations, Project History & Tracking)

---

## Index

### Start Here
- [DeveloperHandbook.md](DeveloperHandbook.md) — The official developer handbook: every architectural decision, layer, convention, and lesson learned while building the Users reference module, plus a step-by-step blueprint for building the next one.

### Architecture & Structure
- [Architecture.md](Architecture.md) — High-level system architecture, style, and module boundaries.
- [FolderStructure.md](FolderStructure.md) — Approved repository folder layout.
- [InformationArchitecture.md](InformationArchitecture.md) — Navigation, module hierarchy, search, notifications, and profile architecture for the HMS.
- [ScreenInventory.md](ScreenInventory.md) — Complete screen catalog per module: main, create, edit, view, search, dashboard, reports, settings, dialogs, popups, print/export views.
- [DesignSystem.md](DesignSystem.md) — Enterprise design system spec: tokens (typography, spacing, grid, color, elevation, radius, icons), components, accessibility (WCAG 2.2 AA), dark mode.
- [LayoutFramework.md](LayoutFramework.md) — Structural layout spec: nav, sidebar, drawers, sticky behavior, z-index stacking, and responsive/multi-monitor rules with measurements.

### Standards & Conventions
- [CodingStandards.md](CodingStandards.md) — Code style and quality expectations.
- [NamingConventions.md](NamingConventions.md) — Naming rules across frontend, backend, and database.
- [ApiStandards.md](ApiStandards.md) — HTTP API design conventions.
- [DatabaseGuidelines.md](DatabaseGuidelines.md) — Shared PostgreSQL schema and migration conventions.

### Engineering Practices
- [DevelopmentGuidelines.md](DevelopmentGuidelines.md) — Local setup and day-to-day development workflow.
- [GitWorkflow.md](GitWorkflow.md) — Branching, commits, and pull request process.
- [TestingStrategy.md](TestingStrategy.md) — Testing approach and expectations.
- [Logging.md](Logging.md) — Logging conventions.
- [ErrorHandling.md](ErrorHandling.md) — Error handling patterns and response shape.
- [Configuration.md](Configuration.md) — Configuration and environment variable management.

### Security & Access
- [Authentication.md](Authentication.md) — How users authenticate.
- [Authorization.md](Authorization.md) — Roles, permissions, and access enforcement.
- [Security.md](Security.md) — Security practices and policies.

### Operations
- [Deployment.md](Deployment.md) — Build and deployment process.

### Project History & Tracking
- [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md) — BA/architect analysis of the client HMS requirement brief: modules, actors, entities, gaps, risks.
- [UserJourneyMaps.md](UserJourneyMaps.md) — End-to-end UX journey maps for the nine primary HMS personas (goals, pain points, actions, errors, success states).
- [ReleaseNotes.md](ReleaseNotes.md) — What shipped in each release.
- [DecisionLog.md](DecisionLog.md) — Architecture Decision Records.
- [BugFixes.md](BugFixes.md) — Log of notable bug fixes.
- [KnownIssues.md](KnownIssues.md) — Currently known, unresolved issues.
- [Troubleshooting.md](Troubleshooting.md) — Common problems and resolutions.
- [FeatureRequests.md](FeatureRequests.md) — Backlog of proposed future features.
