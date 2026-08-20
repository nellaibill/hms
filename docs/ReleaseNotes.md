# Release Notes

## Purpose
This document provides a chronological, human-readable record of what shipped in each release, for team and stakeholder visibility.

## Scope
Covers user- and team-facing summaries of changes per release (features, fixes, breaking changes).

**Out of scope:** the technical rationale behind decisions (see [DecisionLog.md](DecisionLog.md)) and detailed bug root-cause analysis (see [BugFixes.md](BugFixes.md)).

## When to Update This Document
- Every time a release/version is shipped.
- When an unreleased change is merged to `main` (add it under "Unreleased" first).

## Recommended Sections
- Unreleased
- Versioned Release Entries (newest first)
- Entry Template (Added / Changed / Fixed / Removed)

---

## Unreleased

**Added**
- Pharmacy module — minimal direct-dispense MVP: a `pharmacy` schema with a running stock-balance ledger per product/batch (`PharmacyStockBalance` + append-only `PharmacyStockTransaction`), Stock Receipt and Dispense workflows (expiry and insufficient-stock checks enforced server-side, optimistic-concurrency-safe under concurrent dispenses), read-only stock-balance and combined stock-ledger views; a `/pharmacy` web UI (hub, stock receipt, dispense, and ledger pages) replacing the previous placeholder page, including inline batch creation and Expired/Expiring-soon stock badges added during a full end-to-end QA pass. Reuses the existing Products drug/batch catalog and the already-seeded `pharmacy.*` permissions.
- Pharmacy dispense billing — a successful dispense now generates a real invoice via a new `Pharmacy` `BillingType` and a best-effort call into `IInvoiceService`; a billing failure never blocks or reverts the dispense itself, and is surfaced to staff for manual posting via OPD Billing Entry. Reverses ADR-027's earlier deferral — see [DecisionLog.md](DecisionLog.md) ADR-028.
- Pharmacy dispense cart — a new `POST /api/v1/pharmacy/dispenses/cart` endpoint checks out several product/batch/quantity lines for one patient in a single call, dispensing all of them atomically (one line's failure aborts the whole checkout with nothing partially dispensed) and billing them as ONE invoice with N line items, reusing Billing's existing multi-item `CreateInvoiceRequest.Items` support. The Dispense Stock page now presents a cart UI (add/remove item rows, a live running total, one Checkout submit) in place of the previous single-item form; the original single-item `POST /dispenses` endpoint is unchanged and still available.
- Users module (Identity) — the first complete reference module, end to end: `identity.users` PostgreSQL table with audit columns and soft delete; full backend (entity, repository, service, FluentValidation, controller, DI registration, global exception handling); Create/Update/Delete(soft)/Get-by-ID/Get-paged(sort/search/filter)/Activate/Deactivate APIs; shared TypeScript package additions (DTOs, HTTP client, error models, validation); a complete React web feature (list, create, edit, view, delete-confirm, activate/deactivate, routing, pagination, search, sorting); a feature-parity React Native mobile implementation; and unit/integration/architecture tests.
- Central Package Management (`Directory.Packages.props`) for the backend solution.

**Changed**
- N/A

**Fixed**
- Shared HTTP client (`frontend/shared/api-client/httpClient.ts`) no longer swallows `AbortError` into a generic network error, and both web and mobile query clients use `networkMode: 'always'` — a failed request now reliably surfaces as an error instead of occasionally hanging in a loading state (see [DecisionLog.md](DecisionLog.md) ADR-007).

**Removed**
- N/A

**Known limitations** (see the Users module's Phase 10 checklist for the full list): the backend has not been built/restored/migrated against a real .NET SDK or PostgreSQL instance in this environment; the EF Core migration's tool-generated Designer/Snapshot files still need to be produced via `dotnet ef`; React Native has been type-checked but not bundled through Metro/Expo.

## Release Entry Template

### [Version] - YYYY-MM-DD
**Added**
- _To be documented._

**Changed**
- _To be documented._

**Fixed**
- _To be documented._

**Removed**
- _To be documented._
