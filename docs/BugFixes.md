# Bug Fixes

## Purpose
This document is a lightweight log of notable bug fixes and their root causes, giving the team institutional memory without requiring a heavyweight issue tracker for an MVP.

## Scope
Covers non-trivial bug fixes worth remembering (root cause, affected module, fix summary).

**Out of scope:** trivial fixes (typos, minor styling), which don't need a log entry. Currently open, unresolved issues belong in [KnownIssues.md](KnownIssues.md) until fixed.

## When to Update This Document
- After fixing a non-trivial bug, especially one with a non-obvious root cause.
- When an entry in [KnownIssues.md](KnownIssues.md) is resolved (move it here).

## Recommended Sections
- Bug Fix Entry Template
- Chronological list of fixes (newest first)

---

## Bug Fix Entry Template

### [Short Title]
**Date:** YYYY-MM-DD
**Affected Module:** _To be documented._
**Reported By:** _To be documented._

**Symptom**
_To be documented._

**Root Cause**
_To be documented._

**Fix**
_To be documented._

---

## Fixes

### Hospital Super Admins never received permissions added after provisioning
**Date:** 2026-10-20
**Affected Module:** Identity (seen first in Discharge Summary)
**Reported By:** User (LHS, local copy of production data)

**Symptom**
On a discharged IPD admission, the Discharge tab showed "Failed to load the discharge summary. Please try again." for the hospital's Super Admin, even though no summary existed yet and the tab should have offered "Create Discharge Summary".

**Root Cause**
`GET /api/v1/admissions/{id}/discharge-summary` returned 403, not 404. A migration that adds permissions (`AddDischargeSummaryPermissions`, `AddPharmacyViewCostAndDiscountApprovePermissions`) only inserts the catalog rows. The Super Admin role was re-synced to the full catalog only in the legacy database (`IdentityDataSeeder`, at startup). Every hospital database's Super Admin kept the permissions it had when the hospital was provisioned. On 2026-10-20, every non-legacy hospital database checked locally had 45 of 51: it was missing `discharge-summary.*` (4), `pharmacy.view-cost` and `finance-billing.discount-approve`. The entry card treated the 403 like any other failure.

**Fix**
- `SuperAdminPermissionSync` (Identity) holds the re-sync logic, which `IdentityDataSeeder` already had. `TenantMigrationService` now runs it via `IdentityModule.SyncTenantSuperAdminPermissionsAsync` right after migrating each hospital's identity schema. That covers Platform → Hospitals → Migrate, feature toggles, and provisioning, where it's a no-op because no role exists yet.
- The Discharge Summary entry card shows a permission message for 403 instead of "Please try again".
- After deploying, run **Migrate** on each hospital. Signed-in Super Admins must sign out and back in, because permissions are JWT claims captured at login.
