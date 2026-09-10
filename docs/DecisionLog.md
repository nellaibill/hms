# Decision Log

## Purpose
This document is an Architecture Decision Record (ADR) style log capturing *why* significant technical decisions were made, so the reasoning isn't lost or locked in one person's head — important given the team has a single senior developer.

## Scope
Covers significant, hard-to-reverse technical or architectural decisions (e.g., choosing Modular Monolith over Microservices, choosing PostgreSQL, module boundary decisions).

**Out of scope:** day-to-day implementation choices that don't affect the broader system.

## When to Update This Document
- Whenever a significant technical or architectural decision is made.
- Whenever a previously recorded decision is revisited or reversed (add a new entry; do not delete the old one).

## Recommended Sections
- Decision Entry Template (Context, Decision, Status, Consequences)
- Chronological list of decisions (newest first)

---

## Decision Entry Template

### ADR-000: [Decision Title]
**Date:** YYYY-MM-DD
**Status:** Proposed / Accepted / Superseded / Deprecated

**Context**
_To be documented._

**Decision**
_To be documented._

**Consequences**
_To be documented._

---

## Decisions

### ADR-078: CI security gates — dependency-vulnerability scanning, secret scanning, Dependabot
**Date:** 2026-09-10
**Status:** Accepted

**Context**
Finding M8 from the security review: the CI pipeline (`.github/workflows/build.yml`) validated
build correctness and ran tests, but had no dependency-vulnerability scan, no secret scan, and
no Dependabot configuration — the exact class of tooling that would have caught the committed
credential leak (ADR-073) and any vulnerable package before merge, rather than by manual review
after the fact.

**Decision**
1. **New `security` job** in `build.yml`, separate from `backend`/`frontend` so a scan failure
   reads clearly as a security gate, not a build/test failure:
   - **Secret scan (gitleaks)**, run via `docker run zricethezav/gitleaks:latest` (Docker is
     preinstalled on GitHub-hosted runners) rather than a marketplace Action, avoiding any
     ambiguity around Action-specific licensing terms for private repos — the underlying
     `gitleaks` CLI itself is open-source. **Deliberately scoped to only the commits a PR/push
     actually introduces** (`--log-opts` against the PR's base commit, or `HEAD~1..HEAD` for a
     direct push — every push to `main` in this repo's workflow is a single-commit squash-merge,
     so that range is exact) rather than the whole repository history on every run. Scanning
     full history today would immediately and permanently fail this gate on the real, known,
     not-yet-purged leak from ADR-073's own git history — this gate exists to catch a
     *recurrence*, not to re-litigate an already-disclosed, already-scrubbed-from-HEAD incident
     whose history purge is a separate, deliberately-deferred, user-owned action.
   - **`.NET dependency vulnerability scan`** — `dotnet list package --vulnerable
     --include-transitive` against `HMS.sln`, with a shell check that fails the step if the
     output contains "has the following vulnerable packages" (the command itself doesn't exit
     non-zero on its own).
   - **`npm audit --audit-level=high`** across all three frontend workspaces (`web`/`mobile`/
     `shared`) — `npm audit` already exits non-zero on a qualifying finding, no extra check
     needed.
2. **New `.github/dependabot.yml`** — weekly update PRs for NuGet (`/backend`, landing in
   `Directory.Packages.props` regardless of which module first pulled a package, since Central
   Package Management is already in use), all three npm workspaces individually (no shared
   workspace root to point at instead), and `github-actions` itself (an outdated Action version
   is its own supply-chain surface).
3. **Real vulnerability found and fixed while standing this up, not left for the new gate to
   immediately fail on**: `dotnet list package --vulnerable` surfaced
   `HMS.IntegrationTests → Testcontainers.PostgreSql → (transitive) SSH.NET 2023.0.0`,
   GHSA-q939-rpr3-3284 (high severity — SCP recursive-download arbitrary file write). Fetched
   the advisory directly (`vulnerable_version_range: "<= 2025.1.0"`,
   `first_patched_version: "2026.0.0"`) rather than guessing a version — bumping
   Testcontainers.PostgreSql itself didn't change the resolved SSH.NET version, so this is a
   **direct force-pin** (`Directory.Packages.props` + a direct `PackageReference` in
   `HMS.IntegrationTests.csproj`), the exact same pattern already established for
   `System.Security.Cryptography.Xml`'s own transitive-CVE pin in the same file. SSH.NET/
   Testcontainers are test-only (`HMS.IntegrationTests`, not run in CI per its own Testcontainers
   exclusion, but still built) — this was never a production-runtime exposure.
4. **A second real batch found the same way, on the frontend side**: `npm audit
   --audit-level=high` in `web` immediately surfaced 4 real high-severity transitive
   vulnerabilities — `brace-expansion` (GHSA-mh99-v99m-4gvg / GHSA-rgw5-rvv9-x895, DoS via
   unbounded expansion), `js-yaml` (GHSA-5p4m-2wfm-xmqj / GHSA-2883-xcg3-v3hh, quadratic-CPU
   DoS), and `nanoid` (GHSA-2v37-7h3g-55p8, infinite loop on a zero-size generator) — all pulled
   in transitively by dev/build tooling (`@typescript-eslint/typescript-estree`, `glob`, etc.),
   never runtime code shipped to users. Fixed via `overrides` in `frontend/package.json` (the
   npm-workspaces root, covering `web`+`shared`) and `frontend/mobile/package.json` (a fully
   standalone project, not part of that workspace — confirmed via its own lockfile, which
   resolves the same three packages independently). **Deliberately picked the smallest safe
   version for each** (`brace-expansion` to `^2.0.2`, not the `5.x` line also seen elsewhere in
   the tree; `js-yaml` to `^4.3.2`, a patch bump; `nanoid` to `^3.3.18`, a patch bump) rather
   than jumping every resolution to the newest major release each package has published — a
   deliberately-transitive dev-tooling dependency is exactly the wrong place to risk an
   unreviewed breaking change (e.g. a CJS/ESM split) for a fix that has a same-major patch
   available. `dompurify`/`esbuild`/`postcss`/`react-router`/`uuid` findings from the same
   `npm audit` run are moderate severity (don't fail `--audit-level=high`) and only have
   `--force` (breaking-change) fixes available — left as an explicit, disclosed follow-up rather
   than bundled into this pass.

**Consequences**
- The gitleaks/npm-audit/dotnet-vulnerable checks are new CI gates that will now block a PR
  whose diff introduces a real secret or a newly-vulnerable dependency — expected and intended.
- The full solution build (`dotnet build HMS.sln`) and the full test suite excluding
  IntegrationTests (98 architecture tests + 925 unit tests) both pass unchanged with the
  SSH.NET/BouncyCastle.Cryptography version bump — confirmed directly, not assumed.
- **The frontend `overrides` fix could not be locally verified** — this session's local
  environment had persistent, unexplained npm-registry/process reliability problems for the
  entire session (multi-minute hangs with zero output on operations that complete in seconds in
  CI, most likely a `timeout` command name collision between GNU coreutils and Windows'
  built-in `timeout.exe` on this Git Bash setup silently defeating every timeout guard used to
  bound the attempts). `frontend/package-lock.json`/`frontend/mobile/package-lock.json` are
  therefore **not** regenerated in this commit — CI's own `npm install` (not `npm ci`, so it's
  free to update the lockfile) is the actual verification, and will commit an updated lockfile
  reflecting the new resolutions the next time someone runs `npm install` locally after pulling
  this change. Flagged explicitly here rather than silently claimed as verified.
- Dependabot's weekly PRs will start surfacing on their own schedule after this merges — normal
  and expected, not something this ADR needs to pre-emptively triage.

---

### ADR-077: Product `CostPrice` now requires a separate `pharmacy.view-cost` permission
**Date:** 2026-09-10
**Status:** Accepted

**Context**
Finding M17 from the security review: `Product.CostPrice` (procurement cost) is commercially
sensitive margin data, but was returned verbatim in `ProductResponse` to anyone holding plain
`pharmacy.view` — the same permission every day-to-day dispensing/stock-handling user needs just
to see the product list. No finer-grained permission existed to separate "can see stock/pricing"
from "can see what the hospital pays for it."

**Decision**
1. **New `pharmacy.view-cost` permission**, a fifth action outside the view/create/edit/delete
   set every other `pharmacy` permission uses — same precedent as `discharge-summary.finalize`
   (ADR-022). Added to `PermissionSeedData.cs`; the existing Super Admin role auto-sync
   (`IdentityDataSeeder.EnsureSuperAdminRoleAsync`, re-syncs to every currently-seeded permission
   on every startup) means Super Admin gets it automatically, no manual step needed.
2. **Redaction happens in `ProductsController`, not `ProductService`** — `GetById`/`GetPaged`
   now zero out `CostPrice` on the response unless `User.HasPermission("pharmacy.view-cost")`
   (a new `ClaimsPrincipalExtensions.HasPermission` helper, reading the same `"Permission"` JWT
   claim `PermissionAuthorizationHandler` already checks). Kept at the Controller layer
   specifically because that's the only place with access to the caller's `ClaimsPrincipal` —
   `[RequirePermission]` only gates whether an *entire action* runs, not what a permitted action
   *returns*, so this is a different mechanism for a different job, not a replacement for it.
3. **Zeroed to `0`, not made nullable.** `ProductResponse.CostPrice` is a non-nullable `decimal`
   used identically by the create/edit request DTOs (which always carry the real value) and this
   read path — changing its nullability would be a wider contract change than this fix warrants,
   and `0` is already this codebase's existing "not yet costed" convention (see
   `update-diagnostic-cost-prices.ps1`'s identical reasoning for diagnostic services).
4. **Not yet exposed in the frontend's generic Roles matrix.** `PERMISSION_ACTIONS`
   (`frontend/web/src/features/roles/types.ts`) is hardcoded to exactly `view`/`create`/`edit`/
   `delete` — the same limitation `discharge-summary.finalize` already has, confirmed by grep
   (zero references to either key anywhere under `features/roles/`). Granting
   `pharmacy.view-cost` to a custom (non-Super-Admin) role is a backend/seed-data action today,
   not self-service via the Roles admin UI — an accepted, pre-existing pattern in this codebase,
   not a new gap introduced here.
5. **`ProductPricesController` needed no change** — confirmed its response contracts
   (`ProductPriceContracts.cs`) never reference `CostPrice` at all; only `ProductsController`'s
   two GET actions return it.

**Consequences**
- First controller-level test in this suite (`ProductsControllerTests.cs`, 3 tests) — deliberate,
  since the redaction logic lives in the Controller and needs a `ClaimsPrincipal`, which no
  Service-level test could exercise. `HMS.Modules.Products` still has no `InternalsVisibleTo`
  wiring and therefore no way to test its internal Service/Repository layer directly — not
  needed for this fix, since `ProductsController`/`IProductService`/`ProductResponse` are all
  public, but a real gap if a future Products fix needs to reach internal types.
- A role holding `pharmacy.create`/`pharmacy.edit` but not `pharmacy.view-cost` can still submit
  a real `CostPrice` on create/update (those request DTOs are unaffected) even though they'd see
  it redacted on their own subsequent read — an accepted asymmetry for this pass, matching the
  review's own framing of the fix as closing the *view* exposure specifically.

---

### ADR-076: Trust forwarded client IPs only from loopback; rate-limit message/notification/upload endpoints
**Date:** 2026-09-10
**Status:** Accepted

**Context**
Two related, but distinct, rate-limiting gaps from the security review (findings H6 and M16):

1. **H6 — the per-IP rate limiter was silently neutralized behind the documented Windows/nginx
   reverse-proxy deployment path.** `RateLimitingConfiguration.ClientKey` partitions on
   `HttpContext.Connection.RemoteIpAddress`, and deliberately never read `X-Forwarded-For`
   (ADR-018's own reasoning: this host wasn't yet known to sit behind a trusted proxy, and
   trusting an attacker-controlled header would let the limiter be trivially bypassed). But
   `scripts/deploy/windows/nginx-hms-reverse-proxy.conf` proxies every request through nginx on
   the same host — so in that deployment, `RemoteIpAddress` is `127.0.0.1` for **every** request
   regardless of the real client, collapsing the per-client 200/min (global) and 10/min (login)
   limits into one shared bucket for all users combined. That both defeats brute-force
   protection against a real attacker (their requests never stand out from aggregate legitimate
   traffic) and creates a self-inflicted denial-of-service (one busy client exhausts the shared
   bucket for everyone). ADR-018 and the reverse-proxy deployment guide shipped in separate
   sessions and were never reconciled against each other.
2. **M16 — rate limiting covered only the Login endpoint app-wide.** Every other authenticated
   write endpoint — message-send, notification broadcast, file uploads — had no request-rate
   ceiling beyond the generous 200/min global limit, which is sized for normal UI polling, not
   as abuse protection for any specific expensive action.

**Decision**
1. **`app.UseForwardedHeaders(...)` added as the very first middleware in `Program.cs`**, using
   `ForwardedHeadersOptions`' own framework defaults for `KnownNetworks`/`KnownProxies`
   (loopback only) — deliberately **not** widened or overridden. This means `RemoteIpAddress` is
   only ever rewritten from `X-Forwarded-For` when the *immediate* TCP connection is
   127.0.0.1/::1 (i.e., already relayed by nginx running on the same host) — a client cannot
   spoof this by simply sending the header over the network, since the immediate-connection
   check happens first. `nginx-hms-reverse-proxy.conf` already correctly sets
   `X-Forwarded-For`/`X-Forwarded-Proto` (via `$proxy_add_x_forwarded_for`, which appends rather
   than overwrites) — the gap was entirely on the ASP.NET Core side never reading it. No effect
   on the Docker Compose deployment path, which has no proxy hop at all.
2. **New `RateLimitingPolicyNames.Write` policy (60 req/min per client)**, applied via
   `[EnableRateLimiting(RateLimitingPolicyNames.Write)]` to `ConversationsController.
   SendMessage`, `NotificationsController.Notify`, `BrandingController.UploadLogo`,
   `DocumentsController.Upload`, `ProductImagesController.Upload`, and
   `PatientImportController.Upload` — the exact set the review flagged.
3. **Partitioned by IP, not by authenticated user, despite the review's suggestion of "a
   token-bucket per user."** Rejected after checking the actual pipeline order: rate limiting
   (`UseHmsRateLimiting()`) deliberately runs *before* `UseAuthentication()` (ADR-018 — an
   unauthenticated flood shouldn't spend JWT-validation work first), so no verified user
   identity exists yet at the point a rate-limit partition key is chosen. Decoding an
   unvalidated bearer token just to read its `sub` claim for partitioning would let an attacker
   trivially bypass the limiter by attaching a different (even unsigned/garbage) token per
   request — worse than the per-IP status quo, not better. Per-IP partitioning, consistent with
   every existing policy, was kept.

**Consequences**
- Every deployment path with a reverse proxy in front of the API must ensure that proxy is the
  literal loopback interface (as `nginx-hms-reverse-proxy.conf` already is) — a future deployment
  topology with the proxy on a *different* host would need an explicit `KnownProxies`/
  `KnownNetworks` entry for that proxy's real IP, which this change deliberately does not add
  pre-emptively (no such topology exists in this codebase today).
- The `Write` policy's 60/min ceiling is a judgment call, not derived from any specific measured
  usage pattern — generous enough for normal single-user interactive use (composing several
  messages, uploading a few files in a row) while still bounding a scripted abuse loop; revisit
  if real usage ever legitimately needs a higher burst rate.
- `HMS.UnitTests` full suite (922 tests) and the three newly-built module projects all pass
  unchanged — this is additive middleware/attribute wiring, not a change to any existing
  business logic.

---

### ADR-075: File-upload and Excel-export hardening sweep (Phase 1 of the security review)
**Date:** 2026-09-10
**Status:** Accepted

**Context**
Phase 1 of the full-application security review's remediation roadmap bundles four small,
independent hardening items found across the file-upload and report-export surfaces (findings
M12–M15). None of these had an active, confirmed exploit — they're consistency/defense-in-depth
gaps against patterns already proven correct elsewhere in this codebase — but each is cheap to
close and the review flagged them for the same "hardening sweep" pass.

**Decision**
1. **Product image upload now content-sniffs, matching every other image upload path
   (M12).** `ProductImageService.UploadAsync` was the one upload path in the codebase trusting
   the client-declared file extension alone — `BrandingService.UploadLogoAsync` and
   `UserService.UploadProfilePhotoAsync` both already decode the actual bytes via
   `Image.Identify`. Added the identical check (buffer once, `Image.Identify`, reject if it
   doesn't decode as a real image) and a `SixLabors.ImageSharp` package reference to
   `HMS.Modules.Products.csproj` (previously unused there). Pixel-dimension bounds were
   deliberately **not** added — unlike Branding's fixed header logo box, product images have no
   equivalent fixed display constraint to size against, so only content-type validity is
   checked here, not dimensions.
2. **Branding's SVG sanitizer now parses and walks the element/attribute tree instead of
   string-matching one literal (M13).** The previous check only rejected a payload containing
   the literal substring `<script` — an `onload=`/`onerror=` event-handler attribute or a
   `javascript:` URI in `href`/`xlink:href` never contained that substring and sailed through
   untouched. `ValidateSvg` now parses the upload as real XML (`XmlReaderSettings` with
   `DtdProcessing.Prohibit` and `XmlResolver = null` — explicitly closing an XXE hole this new
   parsing step would otherwise open, since SVG has no legitimate need for a DOCTYPE/external
   entity) and rejects any `<script>`/`<foreignObject>` element, any `on*`-prefixed attribute,
   or any attribute value starting with `javascript:`. Malformed/non-well-formed markup now
   fails closed ("not valid SVG markup") rather than being silently accepted. Seven new tests
   in `BrandingServiceTests` cover a clean SVG, each rejection case, and the DOCTYPE case.
   SVG logos are still only ever rendered via `<img src>` in the frontend (browsers don't
   execute embedded script for that element) — this sanitizer is defense-in-depth against the
   file being opened by direct top-level navigation, not the only thing standing between an
   uploaded SVG and script execution.
3. **Excel exports now neutralize formula-injection payloads (M14).** A new
   `ExcelCellSafety.Sanitize` helper (`HMS.Modules.Patients.Application.Excel`) prefixes any
   cell string beginning with `=`, `+`, `-`, `@`, tab, or CR with a leading apostrophe — the
   standard OWASP mitigation — before it's written into a ClosedXML cell. Applied at both call
   sites the review flagged: `PatientImportReportGenerator` (echoes raw uploaded cell content
   back into a downloadable report — the most exposed case, since that content is directly
   attacker/user-typed) and `PatientReportExportGenerator` (patient UHID/name/phone/department,
   all user-entered at registration). Kept module-local to Patients rather than promoted to a
   shared project, since Patients is currently the only module with ClosedXML exports — worth
   promoting to `HMS.Shared` if/when a second module adds one, not before.
4. **Baseline security headers added host-wide (M15).** New
   `SecurityHeadersConfiguration.UseHmsSecurityHeaders()`, registered in `Program.cs` before
   `UseStaticFiles()` so the headers also land on Branding logos/product images/patient photos
   served from `wwwroot/uploads`, not just JSON API responses. Sets `X-Content-Type-Options:
   nosniff` and `X-Frame-Options: DENY` unconditionally, plus a restrictive
   `Content-Security-Policy` (`default-src 'self'; script-src 'none'; object-src 'none';
   frame-ancestors 'none'; base-uri 'none'`) on every response except `/swagger/*` (Swagger UI's
   own inline scripts/styles would break under it, and it's Development-only per
   `SwaggerConfiguration.cs`).

**Consequences**
- No behavior change for any legitimate upload or export — every new check rejects content that
  was already invalid/malicious by the feature's own stated intent (a real image, real SVG
  markup with no embedded script, a data export that displays as data not a formula).
- `HMS.Modules.Products` gains its first-ever image-library dependency and, separately, still has
  no unit-test coverage at all (confirmed during this change — no `InternalsVisibleTo` wiring
  exists for it yet) — this fix is verified by a clean build and direct parity with
  `BrandingService`'s already-tested pattern, not new Products test infrastructure; standing up
  that infrastructure is a separate, larger undertaking than this hardening sweep.
- The CSP is scoped to this API host only — it has no effect on the separately-deployed React
  frontend's own headers, which are unrelated to this change.

---

### ADR-074: Branding write endpoints now require `identity-administration.edit`; Documents and Patient Reports routes gained real permission guards
**Date:** 2026-09-10
**Status:** Accepted

**Context**
A full-application security review (six parallel domain audits) found, independently in three of
them, that `BrandingController`'s `Update`/`UploadLogo` actions carried only the default
`[Authorize]` (the "Hospital" FallbackPolicy — any authenticated user of any role, any tenant)
with no `[RequirePermission(...)]`. The frontend's only gate was `RequireRole(['admin',
'superAdmin'])`, whose own doc comment admitted this was "frontend-only, matching the app's
current mock-auth posture (no backend authorization exists anywhere yet)" — stale everywhere
else in the app (real backend RBAC exists per ADR-022), but accurate for this one controller.
Net effect: any authenticated hospital user — reception, nursing, any role — could call
`PUT /api/v1/branding` or `POST /api/v1/branding/logo` directly and rewrite the tenant's theme,
hospital identity fields, and logo, which is then rendered to every other user's session. This is
the exact class of gap ADR-022 closed for Documents/Users/HR/IPD/Products/Calendar in August
2026 — Branding was simply missed at the time.

**Decision**
1. `Update` and `UploadLogo` now additionally require `[RequirePermission
   ("identity-administration.edit")]`. **Reused the existing `identity-administration` category
   rather than adding a new `branding` one** — that category is already labelled "Roles, Users &
   Settings" (`frontend/web/src/features/roles/modules.ts`) and is the exact permission
   `mastersRoutes` already reuses for its own Settings-page items (`routes.tsx`,
   `identity-administration.view`) — Branding is one more Settings-page item, not a distinct
   business domain, so this follows ADR-022's stated default of reusing an existing category
   over minting a new one (the one prior exception, `discharge-summary`, was justified by a
   genuinely new action verb, `finalize`, which Branding has no equivalent of). No new
   permission-catalog rows were needed.
2. `Get` is deliberately left at bare `[Authorize]`, unchanged — every authenticated user needs
   it to theme the app after login, not just the admins who reach the Settings page, so gating it
   behind `identity-administration.view` would have broken theming for every non-admin role.
3. Frontend: `brandingRoutes` in `routes.tsx` now wraps `/admin/settings/branding` in
   `RequirePermissionRoute permission="identity-administration.view"`, mirroring
   `mastersRoutes`' exact pattern (coarse route-level hint at `.view`; the backend's `.edit`
   check on the actual mutating actions is the real enforcement boundary — same relationship
   Masters already has between its list/view pages and its underlying create/edit endpoints).
   `RequireRole.tsx` is deleted — this was its only remaining caller anywhere in the frontend.
4. **Same review pass found `/documents` and `/reports` reachable by direct URL with zero
   route-level permission gate** — both previously relied on nav-level sidebar filtering alone
   (`config/navigation.ts`'s `permission` field only controls whether the link is *shown*, not
   whether the route is reachable), the same gap `financeRoutes`/`labWorkflowRoutes` already
   fixed for their own paths. `/reports` was the more serious of the two: a filterable table
   over full patient PII with an Excel/PDF export, reachable and fully functional for any
   authenticated user regardless of role. Added `documentsRoutes` (gated at
   `records-compliance.view`, matching `DocumentsController`'s actual check and the nav leaf's
   labelled permission) and `patientReportsRoutes` (gated at `patient-management.view`,
   deliberately **not** the nav leaf's labelled `reports-analytics` — `PatientsController`'s
   `report`/`report/summary`/`report/export` endpoints were built under ADR-071 checking
   `patient-management.view`, and the nav config's `reports-analytics` label was never actually
   wired to them; gating the route on the label rather than the real backend check would have
   reintroduced a version of the same mismatch this fix is closing). `/documents` and `/reports`
   were moved from the unguarded `specialPages`/`moduleRoutes` map into `routeGatedLeafPaths`,
   mirroring `financeRoutes`'/`labWorkflowRoutes`' existing pattern exactly.

**Consequences**
- No new RBAC category, no new permission-catalog seed rows, no frontend `ROLE_MODULES` change —
  the fix reuses infrastructure that already exists and is already assigned to the correct
  admin-tier population via each tenant's own role configuration.
- A role holding `identity-administration.view` but not `.edit` can now reach the Branding
  settings page (matching Masters' UX) but will get a 403 from the backend on Save/Upload — no
  new frontend-side disabled-state handling was added for that case in this pass (matches
  Masters' own current UX, which has the same gap); worth a follow-up if it proves confusing in
  practice.
- Removes the last caller of `RequireRole.tsx`, which is deleted rather than left as dead code.
- The nav-label/actual-permission mismatch on `/reports` (`reports-analytics` in
  `config/navigation.ts` vs. `patient-management.view` actually enforced) is left as-is rather
  than "fixed" by relabeling the nav config — Patient Reports genuinely is Patient-module data
  gated by the Patient-module permission today; whether it *should* eventually move under a real
  `reports-analytics` permission is a product question for whenever Reports grows beyond Patients
  (ADR-070/071 already scoped it that way deliberately), not something to silently change here.
- **Out of scope for this fix**: the security review also flagged several other hub pages
  (Products/Inventory, IPD Dashboard, Central Laboratory, Messages & Notifications, Pharmacy)
  as reachable by direct URL with nav-filtering-only gating, same shape as `/documents`/`/reports`
  but lower severity (no PII export) and explicitly noted as needing a check on whether their
  underlying GET endpoints independently enforce RBAC before deciding whether route-level gating
  is even the missing piece — left for a follow-up pass rather than bundled in here.

---

### ADR-073: Scrubbed a real tenant's admin password from committed `.EXAMPLE` doc-comments
**Date:** 2026-09-10
**Status:** Accepted

**Context**
A full-application security review (six parallel domain audits — auth/authz, multi-tenancy/data,
API/backend, frontend, config/infra/CI, and shared/less-reviewed modules) found the live Lakshmi
Hospitals tenant's Super Admin password (`Lakshmi@123`) committed in plaintext, repeated across
the `.EXAMPLE` doc-comment of six `cicd/scripts/*.ps1` files (`seed-lakshmi-hospitals.ps1`,
`import-radiology-procedures-v3.ps1`, `migrate-diagnostic-tests-to-services.ps1`,
`refresh-lab-tariffs-v3.ps1`, `seed-appointment-and-consultation-types.ps1`,
`seed-diagnostic-tests.ps1`). A seventh sibling script
(`update-diagnostic-cost-prices.ps1`) was already using a redacted `'...'` placeholder — proof
the placeholder convention was known, just not applied consistently. The password's specificity
and consistency (following a trivially-guessable `CompanyName@123` pattern, tied to the real
hospital code) distinguished it from the clearly-synthetic examples used for other tenants in
sibling scripts (e.g. `'Xxx123!'` for the generic `legacy` tenant).

**Decision**
1. Replaced the real password in all six `.EXAMPLE` blocks with an unambiguous placeholder,
   `'<super-admin-password>'`, matching the bracket-token convention already used elsewhere in
   this codebase for "fill in your own value" documentation (rather than another redacted-looking
   string like `'...'`, which reads as "value omitted" rather than "value required").
2. **Rotating the live credential is out of scope for this code change** — it requires acting on
   the real, currently-provisioned Lakshmi Hospitals tenant, which only the product owner can do
   (this session does not touch live tenant credentials, per standing practice). Flagged to the
   user directly as an action item separate from this PR.
3. **Purging the string from git history is a separate, deliberately-deferred decision.** A
   history rewrite (`git filter-repo`/BFG) followed by a force-push to `main` is a destructive,
   hard-to-reverse operation affecting every existing clone/fork of this repository — it needs
   its own explicit go-ahead from the user rather than being bundled into a routine content fix,
   even though the underlying motivation (the leak) is the same.

**Consequences**
- No functional/behavioral change — these are `.EXAMPLE` doc-comments read by a human running the
  script by hand, never parsed or executed as part of any pipeline.
- The string remains recoverable from git history until a separate, explicitly-approved
  history-purge action is taken; the live credential must be treated as compromised regardless of
  whether that purge happens, since anyone who already cloned the repo before this fix retains it.
- Recommends (not yet actioned): add automated secret-scanning to CI (gitleaks/trufflehog) so a
  recurrence is caught before merge rather than by manual review — tracked as its own backlog item
  in the security review report, not built as part of this fix.

---

### ADR-072: Branding made genuinely tenant-aware, reversing the Phase C decision to keep it on a single shared database
**Date:** 2026-09-09
**Status:** Accepted

**Context**
User reported that a brand-new hospital, created via the Platform Portal, showed the "existing
color from other tenants" — a different hospital's saved logo/theme. Investigation confirmed
this was not a caching artifact but the real, disclosed design of `HMS.Modules.Branding`:
`BrandingModule.cs` was deliberately never made tenant-aware in HMS Multi-Tenancy Phase C,
because `BrandingController.Get()` is anonymous (it themes the pre-login screen before any
hospital is known, and Phase C's tenant-resolution middleware only ever resolves a tenant for
an authenticated Hospital JWT or the login POST itself, neither of which apply to that call).
Every tenant's Branding DbContext connected to the single `ConnectionStrings:Default` database
regardless — confirmed live: `hms_platform.branding.branding_settings` held exactly one row
("Lakshmi Hospitals" branding), shared and overwritable by every hospital in the system,
while every real tenant's own `branding` schema (already migrated per-tenant, since `"branding"`
is a `FeatureCatalog.Mandatory` key) sat empty and unused. ADR-026's own Context line asserting
"hospital-side settings (Branding, etc.) are already per-tenant by construction" was incorrect
for Branding specifically when written — this ADR corrects that.

Asked the user how the anonymous pre-login screen should behave once branding becomes
per-tenant, since by definition no tenant is known at that point: user chose the simpler of two
options — a generic default look pre-login (not a per-hospital-code lookup as the user types),
matching every other unauthenticated surface in the app.

**Decision**
1. `BrandingModule.cs`'s `BrandingDbContext` registration now resolves `ITenantContext
   .ConnectionString` per-request, exactly like every other hospital module (`PatientsModule`'s
   registration is the copied template) — no schema/migration change needed, since
   `TenantMigrationService.MigrateAsync` already provisions each tenant's own `branding` schema
   unconditionally; only the runtime connection-resolution was wrong.
2. `BrandingController.Get()` loses `[AllowAnonymous]` — GET/PUT/POST all now require the same
   Hospital JWT the FallbackPolicy already required for PUT/POST, so `ITenantContext` is always
   resolved by the time `BrandingDbContext` is constructed.
3. The one-time bootstrap-time `BrandingDbContext.Database.Migrate()` call against
   `ConnectionStrings:Default` in `Program.cs` (which existed only to keep the anonymous
   pre-login screen's database provisioned) is removed — it would now throw (no tenant context
   exists at startup) and is no longer needed, since the pre-login screen no longer calls this
   endpoint at all.
4. Frontend: `LoginPage.tsx`/`ThemeProvider` keep calling `useBrandingQuery()` unconditionally,
   but pre-login that request now 401s and `apiBrandingRepository.getBranding()`'s existing
   catch already falls back to the static local defaults (`mockBrandingStore`) — exactly the
   "generic default look" the user chose, with no new frontend gating code needed for that part.
5. `AuthContext.tsx`'s `login()`/`logout()` now call `queryClient.invalidateQueries` on
   `brandingQueryKey` — a secondary, previously-latent bug: the query's fixed key and
   `staleTime: Infinity` meant that without this, a session switching tenants (or moving from
   the pre-login fallback to a real logged-in session) in the same browser tab would keep
   showing whatever branding was cached first, never refetching the newly-authenticated
   tenant's own data.

**Consequences**
- Every tenant's Branding settings page, header/logo/footer, and print templates (invoices,
  lab reports, Patient Reports) now correctly show that tenant's own saved branding instead of
  whichever hospital last saved the shared row.
- Any hospital that previously "customized" branding was actually only ever overwriting the one
  shared row — there is no way to know retroactively which tenant a shared customization was
  "for," so every tenant (including `lhs`, the one that made the most recent edits) starts fresh
  from the backend's built-in defaults after this ships, and needs to re-save its own look if it
  differs from the default. Disclosed here rather than attempting a guessed data migration.
- The pre-login screen is now a fixed, non-customizable generic look for every hospital — this
  was an explicit, confirmed trade-off, not a gap. A future phase could add a public
  hospital-code-driven lookup if a customized pre-login screen is ever wanted; out of scope here.
- ADR-052 (below) documented `hms_qa`'s removal partly in terms of Branding's old
  not-tenant-aware behavior; that behavior no longer exists — see the note added there.

---

### ADR-071: Patient Reports rebuilt as a filterable table MVP, replacing the chart page at `/reports`
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Immediately after ADR-070's chart-based Patient Reports page shipped, the user sent a detailed,
prescriptive MVP spec for a completely different design: a search/filter/table page (UHID,
name, age, gender, phone, registration date, department, last visit, status, quick-view) with
4 summary cards, server-side pagination/sorting, Excel export, and a print view — explicitly
stating the chart-heavy version just built should **not** be the MVP, with charts deferred to "a
future phase." The spec assumed an Angular frontend and a `pageNumber`-shaped pagination
response; this codebase is React (Vite) + ASP.NET Core with an established `PagedResult`
(`page`/`pageSize`/`totalCount`/`totalPages`) shape, so both were corrected to match existing
conventions per the spec's own instruction to prefer conventions over its literal example.

Whether the table MVP should replace the chart page or live alongside it (e.g. as a second tab)
was genuinely ambiguous — the user confirmed: **replace**.

**Decision**
1. `PatientAnalyticsChartsPage.tsx` (the ADR-070 page) is renamed from `PatientReportsPage.tsx`
   and left unrouted — dead code kept intentionally, not deleted, since charts are an explicit
   future phase rather than a rejected direction.
2. `PatientListQuery` gains `Gender`, `BloodGroup`, `DepartmentId` filters (department resolved
   via an EXISTS-over-visits query, the same shape `RegisteredToday` already used) alongside
   the `From`/`To` pair, whose meaning is generalized from "registered in range" to an
   "activity" scope — registered **or** visited in range — specifically so a New-vs-Returning
   patient split is meaningful. Existing callers (the plain Patient Enquiry list) never send
   `From`/`To`, so this is additive, not a behavior change for them.
3. Three new read-only, `patient-management.view`-gated endpoints on the existing
   `PatientsController` — `GET .../report` (the table, reusing `GetPagedAsync`'s filter/sort/
   page pipeline), `GET .../report/summary` (Total/New/Returning/Visits, computed server-side
   in SQL, not walked client-side), `GET .../report/export` (server-generated `.xlsx` via
   ClosedXML, capped at 5,000 rows, respecting the same filters as the table) — rather than
   reusing the ADR-070 approach of paging through the full patient list client-side.
4. `IPatientRepository.BuildFilteredQuery` is extracted once and shared by `GetPagedAsync`,
   `GetReportSummaryAsync`, and `GetExportRowsAsync`, so the table, the summary cards, and the
   export always agree on which patients match the current filters.

**Consequences**
- Supersedes ADR-070's "patients are always fetched in full and filtered by registration date
  client-side" consequence — that asymmetry is gone; `From`/`To`/Gender/BloodGroup/DepartmentId
  are now real SQL-side filters on `GET /api/v1/patients*`, which also benefits the plain
  Enquiry list and removes the earlier session's rate-limiter/unbounded-fetch failure mode at
  its root for this new table too.
- `GetExportRowsAsync` initially omitted the `.Include(Address/Allergies/EmergencyContacts)`
  calls `GetPagedAsync` has, so `PatientMappingExtensions.ToResponse` threw a
  `NullReferenceException` on export — caught live in browser verification (500 on Export
  Excel), fixed by adding the same three `Include`s before sort/take.
- `.AsNoTracking()` was not added to these new read-only queries despite the spec calling it
  out under "Database Performance" — a disclosed gap, not an oversight, left for a follow-up
  since `BuildFilteredQuery` is shared with `GetPagedAsync`'s existing (also-untracked) query
  and changing tracking behavior for all three at once deserves its own verification pass.
- "Status" reuses the existing `requiresDataVerification` flag (Verified/Needs Verification
  badges) rather than introducing a new status concept — no such field exists elsewhere in the
  Patient aggregate.

### ADR-070: Patient Reports — first real content on the standalone Reports nav item
**Date:** 2026-09-09
**Status:** Superseded by ADR-071 — the chart-based page this ADR describes is no longer routed
at `/reports` (kept in the repo, unrouted, as a future-phase candidate).

**Context**
The "Reports" sidebar item (`/reports`, distinct from `/finance/accounts/reports*`) rendered a
bare `PlaceholderPage` — no report existed there at all. User asked for it to be built out,
starting with Patient-related reports: demographics, marital status, referral/arrival source,
allergy details, and visit activity by type/department/consultant, filterable and chart-based.

Research before implementing found:
- Patient-level fields are real: `Gender`, `MaritalStatus`, `BloodGroup`,
  `ModeOfArrivalSource`/`Channel`/`Specify` (the closest thing to a "referral source" — a
  coarse category, `DoctorReferral`/`PatientOrRelativeReferral`/etc., **not** a link to a
  specific referring doctor/consultant record; no such field exists anywhere in this codebase),
  and `Allergies` (`{allergyType, specify, severity}[]`).
- **No cross-patient visits query existed at all.** The only endpoint,
  `GET /api/v1/patients/{patientId}/visits`, is scoped to one patient — a "visits by
  department/consultant" or "visits over time" report needs a new backend query, not just new
  frontend plumbing.
- Recharts (`^3.10.0`) is already a project dependency with working examples on the main
  dashboard (`MonthlyCensusChart`, `DepartmentFinanceChart`) — introducing real charts here is
  not new territory, just reusing an existing, unused-for-reports library.

**Decision**
1. Added `GET /api/v1/patient-visits` (`PatientVisitsController`, absolute route override on
   an action inside the existing per-patient-scoped controller rather than a new controller) —
   paged, optional `from`/`to` filter on `CreatedAt`. `IPatientVisitRepository.GetPagedAsync`
   mirrors `PatientRepository.GetPagedAsync`'s existing shape exactly.
2. Patient-level demographic/allergy breakdowns need no backend change — they reuse the
   existing bulk `GET /api/v1/patients` list, walked page-by-page client-side
   (`getAllPatientsForReport`), same "MaxPageSize silently truncates a single big-page request"
   fix already applied to Finance's `getAllInvoicesForReport` and Masters'
   `masterStoreFactory.getAll()`.
3. `frontend/web/src/features/patients/patientReport.ts` — client-side groupBy/count
   computation, same shape as Finance's `profitReport.ts` (a pattern proven twice now across
   two unrelated domains).
4. Scoped to Patients only for this pass — Laboratory/Radiology volume, HR, and other domains'
   reports are separate future slices under the same `/reports` nav item, same one-slice-at-a-
   time discipline as the rest of this initiative.

**Consequences**
- The Patients module's public surface grows by one cross-patient read (`GetAllAsync` on
  `IPatientVisitService`) — read-only, `patient-management.view`-gated, no write-path change.
- Visits fetched for this report push `from`/`to` server-side; patients are always fetched in
  full and filtered by registration date client-side, since no such filter exists on the
  patients endpoint — an intentional asymmetry, not an oversight (see patientReportData.ts's
  own comments).
- Live browser verification pending a fresh login (session expired mid-build, a recurring gap
  across several slices this session) — disclosed in the PR rather than silently skipped.

---

### ADR-069: Accounts & Finance reports overhaul — layout fix + Laboratory/Radiology/Consultant reports
**Date:** 2026-09-09
**Status:** Accepted

**Context**
User asked for a thorough pass over Accounts & Finance reporting: the Profit Report's layout
looked broken, and they wanted more statistical reports (Laboratory, Radiology, a "Hospital
Profit" report, a "Consultant Profit" report), "as much statistical data as possible."

Research before implementing found:
- The "not aligned" complaint was `ProfitReportPage.tsx`'s "Profit by Service" (uncapped, can
  be dozens of rows) sitting in a 2-column grid next to "Profit by Billing Type" (~5 rows) —
  CSS grid stretches both to equal height, so the shorter card got a large empty gap, and the
  uncapped list dominated the page.
- Only two reports existed (`Income & Expense`, `Profit`), both entirely frontend-computed from
  `useInvoicesForReportQuery()` — no backend reports module. Cost data only exists for
  `BillingType`s with a real Masters catalog price (Consultation/Radiology/Laboratory non-
  package/Procedure); `Pharmacy`/`InpatientCharge` (all IPD-originated revenue) show `—`.
- A "Consultant Profit" report needs consultant attribution to survive payment — it doesn't
  today. `InvoiceLineItem.MarkPaid()` deliberately clears `ConsultantId` (ADR-048), whose own
  doc comment already flagged this exact future need.
- No OT/Surgery module or data exists anywhere — a "Surgical report" has nothing to report on.

**Decision**
1. `CategoryBreakdownCard` gets an optional `maxRows` prop (rows are already sorted by amount
   descending everywhere it's used) — capped both breakdown cards on the Profit Report at 8
   rows, fixing the page-domination and height-imbalance complaints in one change, no new
   scroll UI needed.
2. Added `InvoiceLineItem.BilledConsultantId` — same value as `ConsultantId` at creation, never
   cleared by `MarkPaid()` — a second, reporting-only field, so ADR-048's "no longer currently
   assigned" behavior for `ConsultantId` itself is completely unchanged.
3. Added three new reports: **Laboratory** and **Radiology** (`CategoryProfitReportPage`, one
   shared component parameterized by `BillingType`, reusing the exact Hospital-Profit-Report
   pipeline filtered to one type — OPD-billed data only, disclosed in the subtitle, since IPD-
   originated lab/radiology charges have no resolvable catalog reference to cost yet, ADR-066)
   and **Consultant** (`ConsultantReportPage`, grouping Consultation-type lines by
   `billedConsultantId` — the only `BillingType` carrying consultant attribution at all, also
   disclosed in the subtitle).
4. Renamed "Profit Report" to "Hospital Profit Report" to match the terminology used when
   asking for these reports — same page/route, label only.
5. **Surgical report dropped from this round entirely** — no data exists to report on; revisit
   once/if an OT/Surgery module is built as its own initiative.

**Consequences**
- `ReportNavTabs` now lists five reports instead of two, and wraps instead of clipping.
- A Consultation-type line item billed before this change has no `billedConsultantId` and is
  simply excluded from the Consultant Profit Report rather than grouped under a misleading
  "Unknown" bucket.
- Ships alongside, but on a separate branch/PR from, a companion fix for a consultation
  double-billing gap found during this same review — the user asked for that one to ship
  first, independently, on its own branch.

---

### ADR-068: Consultation double-billing — save-time warning, not a hard block
**Date:** 2026-09-09
**Status:** Accepted

**Context**
User-reported concern during a broader Accounts & Finance reports review: whether consultants
were being "charged twice." Investigation confirmed a real gap, not a misunderstanding.
`InvoiceCreatePage.tsx`'s `consultationAlreadyBilledForVisit` guard only stops OPD Billing
Entry from *prefilling* a duplicate Consultation row when the visit already has one billed —
but `ConsultationBillingCard`'s "Add another Consultation" button lets reception add and save a
second Consultation-type line for the same visit with no check anywhere, client or server
(`InvoiceService.CreateAsync` has no dedupe logic at all).

**Decision**
Add a save-time confirm dialog: if the visit already has a non-voided Consultation-type line
item billed and the form is about to submit one or more active Consultation rows,
`InvoiceCreatePage.handleSave` now stops and shows a "Consultation already billed for this
visit" dialog before calling the create mutation, instead of saving immediately. This is a
**warning, not a hard block** — the existing prefill guard was deliberately whole-visit, not
per-consultant, because billing more than one genuine consultant on the same visit together is
the realistic common case; a hard block would break that legitimate path. The dialog just makes
the accidental case (re-adding a row out of habit after the prefill correctly left it blank)
require an explicit extra click ("Continue and Save") instead of silently saving. No backend
change — this was always a client-side UX guard by design, not a server-side invariant, so a
matching client-side confirm is the proportionate fix.

**Consequences**
- `handleSave` is split into the validate/check step and a new `saveInvoice(values)` that does
  the actual mutation — reused by both the direct-save and confirm-then-save paths.
- `isConsultationEntryActive` (`features/billing/billingActivity.ts`) is now exported from the
  `features/billing` barrel so this page can reuse it, rather than duplicating the "is this row
  actually filled in" check.
- The billed-and-paid case (already the common real-world path) is unaffected in a different
  way: `MarkPaid()` clears `ConsultantId` (ADR-048), so this guard can only ever detect *a*
  Consultation-type line exists for the visit, not which consultant — acceptable, since the
  goal is catching an accidental second consultation charge on the visit at all, not attributing
  it precisely.

---

### ADR-067: IPD Advance/Deposit — tracked as its own ledger, not reconciled into the Invoice
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Item 22 of the original 30-item IPD expansion proposal: record money the patient's family
deposits during the stay (typically at admission, sometimes topped up later), and show staff
how it offsets the eventual Final Bill (ADR-066). Before designing this, read
`InvoiceService.CreateAsync`'s actual payment-application code (not just its contracts) to
check whether an advance could be applied as a partial payment against the Invoice
`IPDBillingService.GenerateFinalBillAsync` creates at discharge.

**Decision**
1. **Confirmed a hard constraint in `HMS.Modules.Billing`'s payment model: it is genuinely
   all-or-nothing per invoice, with no partial-payment or credit/refund concept anywhere.**
   `CreateAsync`'s `Payments` field requires `sum(Payments) >= NetAmount` or the whole call
   fails (`BillingErrorCodes.PaymentAmountMismatch`) — there is no code path that marks some
   line items Paid and leaves others Pending from a partial payment pool. `Payment.
   InvoiceLineItemId` is mandatory (non-nullable), so a payment can't even be recorded against
   an invoice that doesn't exist yet. `Invoice.Void`'s own doc comment independently confirms
   overpayment/refund is a genuine, unbuilt gap ("reversing real collected money needs a
   refund process, and no refund feature exists yet").
2. **Therefore: `AdmissionAdvance` is tracked as its own append-only IPD ledger
   (mirroring `AdmissionCharge`'s exact shape/pattern), with no attempt to auto-apply it as a
   Billing payment.** `IPDBillingService.GenerateFinalBillAsync` and `HMS.Modules.Billing` are
   completely untouched by this slice. The frontend instead computes a display-only "Balance
   due" (`chargesTotal - advanceTotal`, shown as "Refund due (not processed by this system
   yet)" when negative) on `FinalBillCard`, so staff know what to actually collect without the
   system pretending to reconcile money it structurally can't.
3. **Gated by `finance-billing.create`/`.view`, not `clinical-care.*`** — same reasoning as
   `IPDBillingController` (ADR-066): this records real money received, so it sits behind
   Billing's own permission rather than IPD's clinical ones.
4. **`Method` is a small local `IPD.Contracts.PaymentMethod` enum** (Cash/Card/Upi/
   BankTransfer) mirroring `HMS.Modules.Billing.Contracts.PaymentMethod`'s values, rather than
   a cross-module type reference in IPD's own public contracts — consistent with how every
   other small fixed vocabulary in this module (ChargeType, DoctorOrderType, etc.) is IPD's
   own self-contained type even when conceptually similar to something in another module.

**Consequences**
- Real partial-payment/credit support in `HMS.Modules.Billing` remains an explicit, disclosed
  gap — until it exists (or a hand-rolled workaround is built later), reconciling an advance
  against a Final Bill is a manual, outside-the-system step for accounts staff, informed by
  the displayed Balance-due figure.
- New `AdmissionAdvanceServiceTests`/`AdmissionAdvanceTests` (domain guard on non-positive
  amounts). Full `HMS.UnitTests`/`HMS.ArchitectureTests` suites green;
  `IPDModuleBoundaryTests` allowlist extended for `IAdmissionAdvanceService`.
- `admissions` schema gains a new `admission_advances` table — additive, no changes to any
  existing table.

---

### ADR-066: IPD Final Billing — a new generic BillingType, VisitId falls back to PatientId
**Date:** 2026-09-09
**Status:** Accepted

**Context**
IPD has never had a real payment-collection point — `AdmissionCharge` (Bed/Nursing/Lab/priced-
Doctor-Order charges, built across ADR-060/064/065) is only ever an internal ledger; nothing
converts it into an actual `HMS.Modules.Billing` Invoice. This slice closes that gap: at
discharge, a discharged admission's accumulated charges can be converted into a real Invoice
and paid through the existing Billing UI.

**Decision**
1. **`Invoice.VisitId` is required and non-nullable, with no schema change needed** — IPD's
   `Admission` has no `VisitId` at all, but `CreateInvoiceRequest.VisitId`'s own doc comment
   already documents a fallback convention for exactly this situation: pass `PatientId` as
   `VisitId`. This slice adopts that existing convention rather than relaxing Billing's schema
   the way the Laboratory integration slice had to (ADR-064) — a materially lower-risk change to
   an already-shipped module.
2. **New `BillingType.InpatientCharge`, not a reuse of `Laboratory`/`Radiology`/`Procedure`/
   `Consultation`.** `InvoiceService.CreateAsync` has a post-creation hook that creates a new
   `LabOrder` whenever any line is `BillingType.Laboratory` — reusing that type for an
   already-existing `LabOrder`'s charge would create a *second*, duplicate `LabOrder`. A
   dedicated generic type sidesteps every such hook entirely. `InvoiceLineItem.ServiceId` has
   no dedicated free-text description field, so (mirroring `BillingType.Pharmacy`'s exact
   precedent) `ServiceId` carries an already-human-readable "{ChargeType}: {Remarks}" string
   instead of a catalog id — `describeBillingItem`/`resolveItemCostPrice` treat it exactly like
   Pharmacy (pre-formatted text, no cost-price concept). String-backed enum column, so no
   Billing-side migration was needed for the new value itself.
3. **Discharge-only, explicit "Generate Final Bill" button — not automatic at discharge.**
   Gives billing staff a deliberate review point before a financial document is created,
   mirroring how real hospital billing workflows work; `AdmissionService.DischargeAsync` itself
   is untouched. A new `Admission.FinalInvoiceId` (nullable, app-level reference) tracks
   whether one has already been generated, preventing duplicates and telling the frontend
   which state (Generate vs. View) to show.
4. **Gated by `finance-billing.create`, not `clinical-care.*`.** This creates a real financial
   document, so it sits behind Billing's own permission — same as `InvoicesController.Create` —
   rather than IPD's clinical permissions (least-privilege: a nurse with clinical-care access
   shouldn't implicitly be able to generate a legal financial invoice).
5. **No new payment UI.** `IPDBillingService.GenerateFinalBillAsync` creates the Invoice
   `Pending` (no `Payments` supplied at creation) and the frontend routes straight to the
   existing `/finance/accounts/:id` (`InvoiceDetailPage`) — already creator-agnostic (it already
   has a conditional Lab Details tab) — to actually collect payment, same reuse strategy as
   linking to the Lab Order detail page in ADR-064.

**Consequences**
- New `IPDBillingServiceTests` covering: successful generation with correct
  `VisitId`/`BillingType`/`ServiceId` mapping, `NotFound`, not-yet-discharged, no-charges,
  already-generated, and invoice-creation-failure paths. Full `HMS.UnitTests`/
  `HMS.ArchitectureTests` suites green; `IPDModuleBoundaryTests` allowlist extended for
  `IIPDBillingService`.
- `admissions` gains a nullable `final_invoice_id` column — additive migration.
- `IPD.csproj` gains a new `ProjectReference` to `HMS.Modules.Billing` (IPD → Laboratory →
  Billing is now the module's third cross-module dependency beyond Masters/Patients).
- Diet/Nursing(manual-only)/Blood/Referral/Medication charges remain whatever gaps prior ADRs
  already disclosed (ADR-065/063) — this slice doesn't change what gets charged, only that
  whatever *is* charged can now actually be billed and paid.

---

### ADR-065: IPD Doctor Order charge-posting — Radiology/Procedure/Consultation only, Medication excluded
**Date:** 2026-09-09
**Status:** Accepted

**Context**
The user wants to build IPD Final Billing next, but today only Bed/Nursing(manual)/Lab charges
land on the `AdmissionCharge` ledger — Doctor Orders and Medication Orders never got charge-
posting wired up, so a Final Bill built now would silently miss most of an inpatient stay's
real cost. This slice is the agreed prerequisite. Investigation found the two order-generating
entities are not symmetric: `DoctorOrder.OrderType` has three values with a real priced Masters
catalog (Radiology → `DiagnosticService`, Procedure → the legacy `DiagnosticTest`, Consultation
→ `ConsultationType`) and four with none at all (Diet, Nursing, Blood, Referral — no module or
catalog exists for any of them). `MedicationOrder` has zero priced catalog path: Pharmacy's
only pricing route (`DispenseService.BillAsync`) requires picking a specific stock batch
(expiry-checked, stock-decrementing), which is exactly the "no structured prescription" gap
ADR-063 deliberately left open when it kept `MedicationOrder.DrugName` free text.

**Decision**
1. **`DoctorOrder` gains an optional `CatalogItemId` (`Guid?`, app-level reference, no DB FK)**
   — set only when the doctor picks a real catalog item for a Radiology/Procedure/Consultation
   order. Every existing free-text-only order flow (all four unpriced types, and any priced-type
   order placed without picking a catalog item) is completely unaffected — `CatalogItemId`
   simply stays null, exactly like before this slice.
2. **`DoctorOrderService.CreateAsync` resolves the price in place** (no new orchestration
   service, unlike Laboratory's `IPDLabOrderService`) — `DoctorOrder` doesn't need to create
   anything in another module, only look up a price and post a charge, both already Masters/IPD
   concerns `DoctorOrderService` can absorb directly via new constructor dependencies
   (`IDiagnosticServiceService`, `IDiagnosticTestService`, `IConsultationTypeService`,
   `IAdmissionChargeService`) — no new `ProjectReference` needed, IPD already depends on
   Masters. Same best-effort try/catch shape as `IPDLabOrderService.PostChargeAsync` — a
   charge-posting failure is logged, never fails order placement.
3. **One generic `ChargeType.DoctorOrderCharge`**, not three separate enum values per
   OrderType — `Remarks` distinguishes which (e.g. `"Radiology: Chest X-ray"`), same
   traceability-via-free-text tradeoff `LabCharge` already established.
4. **Consultation's `Amount` can be null** (e.g. "Others / On-call") — treated as a legitimate
   "no fixed fee" state, not an error: no charge posted, order still places normally, mirroring
   `ConsultationBillingCard.tsx`'s existing client-side null-handling for the same field.
5. **Medication Orders are explicitly out of scope.** Auto-charging them would mean either
   reversing ADR-063's deliberate free-text design (adding a real Product/batch link) or a
   manual estimated-cost field with no catalog backing — both bigger, separate decisions. The
   Charges tab already supports manually adding a charge today; real Pharmacy-linked pricing
   waits for a future slice.
6. **Diet/Nursing/Blood/Referral orders post no charge, silently** — same category of
   disclosed, open gap as every other deferral in this codebase (Pharmacy integration, Blood
   Bank module, etc.), not a blocker for Final Billing to still be useful for the charges it
   *does* capture accurately.

**Consequences**
- New tests in `DoctorOrderServiceTests` covering all three priced types' successful charge
  posting, the null-Consultation-Amount skip, the no-`CatalogItemId` skip, and the
  unresolvable-catalog-item skip (order still succeeds). Full `HMS.UnitTests`/
  `HMS.ArchitectureTests` suites green — no `IPDModuleBoundaryTests` allowlist change needed
  this time (`IDoctorOrderService`'s public shape is unchanged).
- `doctor_orders` gains a nullable `catalog_item_id` column — purely additive migration.
- IPD Final Billing (the actual next slice) can now build on a Charges ledger that reflects
  Bed/Lab/priced-Doctor-Order costs, with Diet/Nursing/Blood/Referral/Medication remaining
  explicit, disclosed gaps rather than silent omissions a bill-reviewer would never notice.

---

### ADR-064: IPD Laboratory Integration — a second LabOrder creation path, no invoice required
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Seventh slice of the IPD expansion — closes the gap ADR-062 explicitly left open when
`DoctorOrder.OrderType` excluded Laboratory: `HMS.Modules.Laboratory` already has a complete,
live `LabOrder`/`LabOrderItem` workflow (sample collection → processing → result entry →
verification → report release), but it is only ever created from Billing when an OPD invoice
contains a Laboratory line item (`InvoiceService.CreateAsync` → `ILabOrderService.
CreateFromInvoiceAsync`, the only production call site). A ward doctor had no way to order a
lab test for an admitted patient without that patient first going through an OPD-style
invoice, which doesn't fit an inpatient stay.

**Decision**
1. **A second creation path, not a replacement.** `LabOrder.CreateForAdmission` /
   `ILabOrderService.CreateFromAdmissionAsync` sit alongside the existing `Create`/
   `CreateFromInvoiceAsync` — `InvoiceId`, `VisitId`, and `LabOrderItem.InvoiceLineItemId`
   become nullable, and `LabOrder` gains a nullable `AdmissionId` (app-level reference to IPD,
   no DB FK, same convention as every cross-module reference in this codebase). Exactly one of
   `InvoiceId`/`AdmissionId` is set on any given order, never both. The relaxation is purely
   additive — `CreateFromInvoiceAsync`'s signature and behavior are unchanged, and the unique
   index on `InvoiceId` is refiltered to `WHERE invoice_id IS NOT NULL` so it no longer
   collides across multiple admission-originated orders (which all have a null `InvoiceId`).
2. **Not routed through `DoctorOrder`.** Adding `Laboratory` to `DoctorOrderType` was
   considered and rejected again — it would still be a disconnected duplicate of the real
   `LabOrder` state machine, the exact outcome ADR-062 warned against. Instead, a new IPD-side
   orchestration service (`IIPDLabOrderService`) calls straight into Laboratory's own service
   interface, and everything downstream (collection/processing/result entry/verification/
   report release, and the existing `/diagnostics/lab/orders/:id` UI) is reused as-is — this
   slice only adds a new front door, no new state machine.
3. **An unresolvable service/package fails the call immediately**, unlike
   `CreateFromInvoiceAsync`'s skip-and-log behavior for the same case. Invoice-originated lines
   are trusted (Billing already validated them) and must never fail the invoice; here a doctor
   is placing the order directly through IPD's UI, so surfacing a bad selection immediately is
   better UX than silently dropping a test from the order.
4. **One `AdmissionCharge` (new `ChargeType.LabCharge`) is auto-posted per line** when an order
   is placed, using the same Masters price fields (`DiagnosticService.Price`/
   `DiagnosticPackage.TotalPrice`) Billing itself reads — so the cost is visible on the
   admission's Charges tab even though real IPD billing/invoicing doesn't exist yet. Best-
   effort, not transactional with the lab order (a charge-posting failure is logged, not
   fatal) — mirrors `InvoiceService`'s own "downstream posting must never fail the primary
   action" precedent. `AdmissionCharge` has no FK back to the `LabOrder`/`LabOrderItem` that
   generated it (that entity was never designed for traceability, only a free-text ledger line
   — see its own doc comment); `Remarks` carries the test/package name instead. Real IPD
   billing integration remains an explicit open gap, same category of deferral as ADR-062/063.
5. Same win as every IPD-expansion slice: rides on the already-enabled `ipd` feature and
   existing `clinical-care.*` permissions — no new `FeatureCatalog`/`ModuleCatalog`/permission-
   catalog entries.

**Consequences**
- New tests across both modules (`LabOrderTests`, extended `LabOrderServiceTests` for the
  admission path, `IPDLabOrderServiceTests` for the orchestration/charge-posting logic). Full
  `HMS.UnitTests`/`HMS.ArchitectureTests` suites green.
- `IPDModuleBoundaryTests.AllowedPublicTypeNamePattern` extended for `IIPDLabOrderService` in
  the same commit, per the standard checklist.
- IPD's `.csproj` gains a new `ProjectReference` to `HMS.Modules.Laboratory` — the first slice
  where IPD depends on a module beyond Masters/Patients.
- The frontend's new Laboratory tab (`LabOrdersPanel`) deliberately does not rebuild any of
  Laboratory's own sample-collection/result-entry/verification/report UI — it only places
  orders and links out to the existing `/diagnostics/lab/orders/:id` page for everything else.

---

### ADR-063: IPD Medication Orders + MAR — free-text drug names, no auto-generated dosing schedule
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Sixth slice of the IPD expansion — the slice ADR-062 explicitly left open (Medication was
excluded from `DoctorOrderType` because it needs a real structured prescription entity, not a
free-text order description). The proposal splits this into a doctor's Medication Order
(drug/dose/route/frequency/duration/start-end/instructions) and the Medication Administration
Record (MAR) — the nurse's log of each scheduled dose actually given, or withheld with a
reason. Reconfirmed no `MedicationOrder`/`Prescription`/`MedicationAdministration` entity
exists anywhere in the codebase.

**Decision**
1. **`DrugName`/`Dose`/`Route`/`Frequency` stay free text**, not a reference into Pharmacy's
   Product catalog — same reasoning `HMS.Modules.DischargeSummary.Domain.DischargeMedication`
   already established: Pharmacy is direct-dispense only with no structured Prescription
   concept to hang a catalog reference off. Real Pharmacy integration (catalog-driven drug
   selection, stock deduction on administration) remains an explicit open gap, not solved here
   — same category of deferral as Doctor Orders' Laboratory-integration gap (ADR-062).
2. **No auto-generated dosing schedule.** The proposal's own example table implies deriving
   fixed clock times (8 AM/2 PM/8 PM) from a frequency code (BD/TDS/QID) — building that
   mapping is real complexity that varies by hospital convention and would need per-tenant
   configuration to do properly. Instead, `Frequency` stays a free-text display/reference
   field, and **the nurse manually records each administration event with its own scheduled
   time** — still delivers the real MAR value (who/when/given-or-not/why) without a scheduling
   engine.
3. **`MedicationOrder` has the simplest lifecycle of any mutable IPD entity so far**: just
   `Active` → `Discontinued`, one-way, via a single `Discontinue` method mirroring
   `DoctorOrder.Cancel`'s precondition-guard style. "Past its end date" is a **displayed**, not
   stored, fact — the frontend derives it by comparing `EndDate` to now — avoiding a
   background job to auto-transition status the way `DoctorOrder`'s forward progression or
   `LabOrder`'s computed `OverallStatus` might suggest.
4. **`MedicationAdministration` is a real, same-module FK to `MedicationOrder`** (not
   `Admission` directly) — mirrors `DischargeMedication`'s FK to its parent `DischargeSummary`,
   the established distinction between cross-module references (bare Guid, no FK) and
   intra-module ones (real FK). Append-only, same convention as every log-shaped IPD entity so
   far.
5. Same win as every IPD-expansion slice: rides on the already-enabled `ipd` feature and
   existing `clinical-care.*` permissions, no new `FeatureCatalog`/`ModuleCatalog`/permission-
   catalog entries.

**Consequences**
- 13 new tests (`MedicationOrderTests`, `MedicationOrderServiceTests`,
  `MedicationAdministrationServiceTests`) covering the create-guard, Discontinue's one-way
  transition and already-discontinued rejection, and cross-admission order-id rejection for
  administrations. Full `HMS.UnitTests`/`HMS.ArchitectureTests` suites green (863/98).
- `IPDModuleBoundaryTests.AllowedPublicTypeNamePattern` extended for
  `IMedicationOrderService`/`IMedicationAdministrationService` in the same commit, per the
  standard checklist.
- Real Pharmacy integration and frequency-driven schedule generation both remain open gaps for
  a future slice, not silently dropped.

---

### ADR-062: IPD Doctor Orders — a mutable state machine, deliberately excluding Laboratory and Medication
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Fourth slice of the IPD expansion. Doctor Orders (proposal item 8) is the first genuinely
mutable IPD entity — every prior slice (Vitals, Progress Notes, Nursing) was an append-only
log. Investigating the proposal's full order-type list (Medication/Laboratory/Radiology/
Procedure/Diet/Nursing/Blood/Consultation/Referral) surfaced a real architectural fact:
`HMS.Modules.Laboratory` already has a complete `LabOrder`/`LabOrderItem` workflow (sample
collection → processing → result entry → verification → report generation/release, with its
own 10-state per-item state machine), created from Billing when an invoice contains a
Laboratory line item — not from any doctor-order concept. No Radiology or Blood Bank backend
module exists, and Pharmacy remains direct-dispense only (no prescription entity).

**Decision**
1. **`DoctorOrder.OrderType` covers Radiology, Procedure, Diet, Nursing, Blood, Consultation,
   Referral only — Laboratory and Medication are deliberately excluded.** A generic
   `DoctorOrder` row for "Laboratory" would be a disconnected duplicate of the real `LabOrder`
   system; real integration (a ward doctor's order actually creating/feeding a `LabOrder`
   without requiring an Invoice first) is a genuine gap, left open rather than papered over.
   Medication is left for the not-yet-built MAR slice, which needs a real structured
   prescription (drug/dose/route/frequency) from the start, not a free-text order description
   here that MAR would immediately replace.
2. **First mutable IPD child entity — modeled after `LabOrder`'s own domain-method style**
   (`Advance`/`Cancel` methods with precondition guards mirroring `LabOrder.GenerateReport`/
   `ReleaseReport`), not the append-only pattern every prior slice used. Fixed linear sequence
   `Ordered → Accepted → InProgress → Completed`; `Cancelled` reachable from any non-terminal
   state. `DoctorOrderService` pre-checks the same precondition to return a proper
   `Result.Failure` (409-mapped `InvalidOrderStatusTransition`); the domain method re-checks it
   as a genuine invariant, same pre-check/domain-guard split as `LabOrder`.
3. **`OrderedByUserId` is set from the authenticated actor, never accepted in the request
   body** — an order can't be attributed to a doctor who didn't place it.
4. Same win as every prior slice: rides on the already-enabled `ipd` feature and existing
   `clinical-care.create/.view/.edit` permissions, no new `FeatureCatalog`/`ModuleCatalog`/
   permission-catalog entries.

**Consequences**
- 17 new tests (`DoctorOrderTests` — the state-machine transitions and illegal-transition
  rejections; `DoctorOrderServiceTests` — admission-gate, order-not-found, cross-admission
  order-id rejection, and the same illegal-transition rejection surfaced as a `Result.Failure`).
  Full `HMS.UnitTests`/`HMS.ArchitectureTests` suites green (843/98).
- Real integration between a ward Doctor Order and Laboratory's `LabOrder` (so a doctor can
  order labs without going through Billing first) remains an open gap, not solved here.
- `IPDModuleBoundaryTests.AllowedPublicTypeNamePattern` extended for `IDoctorOrderService` in
  the same commit as the service itself, per the now-standard checklist from ADR-061.

---

### ADR-061: IPD Nursing Assessments and Notes as append-only child records; Nursing Tasks deferred
**Date:** 2026-09-09
**Status:** Accepted

**Context**
Second slice of the IPD expansion initiative (ADR-060 was the first: Vitals + Progress
Notes). The source proposal's "Nursing" item bundles three sub-concepts of very different
shape: Nursing Assessment and Nursing Notes are timestamped clinical records (the same shape
already proven twice); Nursing Tasks (medication administration, vitals, IV/fluid monitoring,
catheter/wound care, oxygen monitoring, intake/output) is a checklist with its own lifecycle
that the proposal's own Doctor Orders item (`Ordered → Accepted → In Progress → Completed →
Cancelled`) and MAR item will also need.

**Decision**
1. **Only Nursing Assessment and Nursing Notes are in scope for this slice.** Nursing Tasks is
   deliberately deferred until Doctor Orders is designed, so a single generic order/task
   workflow can be built once and Nursing Tasks can consume it, rather than building an
   ad-hoc Nursing-only task system that Doctor Orders would later have to duplicate or awkwardly
   integrate with — the same reasoning already applied to deferring Discharge Summary's OT
   integration until a real OT module exists.
2. **Both entities mirror `AdmissionCharge`/`VitalsReading`/`ProgressNote` exactly**: real FK
   to `admissions.id` with `OnDelete(Restrict)`, no navigation collection on `Admission`,
   append-only (`POST` to add, `GET` to list, no Update/Delete — a clinical correction is a
   new entry). Both ride on the already-enabled `ipd` feature and `clinical-care.create`/
   `.view` permissions, again avoiding any new `FeatureCatalog`/`ModuleCatalog`/permission-
   catalog entry and the per-tenant enablement gap that cost real time on Discharge Summary.
3. **Nursing Assessment's risk/condition fields stay free text** (`FallRisk`,
   `PressureSoreRisk`, `ConsciousnessLevel`, etc.) — no clinical scoring scale (e.g. Morse
   Fall Scale) is specified anywhere in the source proposal or this codebase, so inventing one
   would be unfounded. `PainScore` reuses `VitalsReading.PainScore`'s exact 0-10 bound.
4. **`NursingNote.Shift` is a real enum** (`NursingShift`: Morning/Evening/Night, in
   `IPDEnums.cs`) — unlike the free-text fields above, shift is a genuinely closed,
   enumerable concept, same treatment as `ChargeType`/`DischargeType`. Named `NursingShift`
   rather than `Shift` to avoid any confusion with HR's unrelated `Shift`/`ShiftAssignment`
   entities.
5. **`IPDModuleBoundaryTests.AllowedPublicTypeNamePattern` extended proactively** for
   `INursingAssessmentService`/`INursingNoteService` in the same commit as the services
   themselves, rather than discovering the failure afterward (as happened with
   `IVitalsReadingService`/`IProgressNoteService` in ADR-060).

**Consequences**
- 8 new tests (`NursingAssessmentServiceTests`, `NursingNoteServiceTests`), full
  `HMS.UnitTests`/`HMS.ArchitectureTests` suites green (826/98).
- Nursing Tasks remains an open item, to be picked up once Doctor Orders exists.

---

### ADR-060: IPD Vitals and Progress Notes as append-only child records of Admission
**Date:** 2026-09-08
**Status:** Accepted

**Context**
First slice of a larger "complete IPD module" initiative. No entity anywhere in the codebase
captured recorded vital signs or doctor progress notes — Discharge Summary's own Vitals/
Examination fields had to be one-off free text for exactly this reason (see its own ADRs).
`HMS.Modules.IPD` already has two features shaped almost identically to what's needed —
`AdmissionCharge` and `BedTransferHistory` — simple, timestamped, append-only child records
of an `Admission`, queried by `AdmissionId` rather than loaded as a navigation collection.

**Decision**
Added `VitalsReading` (temperature, pulse, respiratory rate, blood pressure systolic/
diastolic, SpO2, weight, height, pain score, blood glucose, recorded-by, notes) and
`ProgressNote` (clinical condition, progress, diagnosis, assessment, plan, instructions,
author) as new entities inside the existing `HMS.Modules.IPD` project — not a new module,
since both are pure IPD-internal clinical documentation with no cross-module composition,
unlike Discharge Summary. Mirrored `AdmissionCharge`'s exact shape: real FK to
`admissions.id` with `OnDelete(Restrict)` (Admission soft-deletes, never hard-deletes), no
navigation collection on `Admission`, append-only (`POST` to add, `GET` to list — no
Update/Delete, since a clinical correction should be a new entry, not an edit to history).
Both ride on the already-enabled `ipd` feature and `clinical-care.create`/`.view`
permissions — no new `FeatureCatalog`/`ModuleCatalog`/permission-catalog entries, avoiding
the exact per-tenant enablement gap that blocked Discharge Summary's live verification on an
existing tenant.

**Consequences**
- Vitals stores structured systolic/diastolic and numeric fields (not a single free-text
  blood-pressure string, unlike Discharge Summary's snapshot) so a future graphing/
  abnormal-value-threshold feature can build on it directly.
- Editing or deleting a past reading/note is out of scope — append-only, matching
  `AdmissionCharge`'s own precedent.
- `IPDModuleBoundaryTests.AllowedPublicTypeNamePattern` extended to allow
  `IVitalsReadingService`/`IProgressNoteService` alongside the module's other per-entity
  service interfaces (each is public only because its controller's public constructor can't
  take an internal parameter type, CS0051).

---

### ADR-059: Discharge Summary module Phase 3 — discharge medications, list-sync Update
**Date:** 2026-09-08
**Status:** Accepted

**Context**
Continuation of ADR-057/058. Phase 3 is the last of the three approved phases: a `DischargeMedication` child entity for the printed discharge medication list, and wiring it into the existing `PUT` endpoint's full-record update.

**Decision**
1. **`DischargeMedication` is a real, same-schema DB foreign key to `DischargeSummary`** — unlike `AdmissionId`/`PatientId` on the parent (cross-module app-level Guids, no FK), this is an intra-module relationship, the same distinction ADR-029 drew for Messaging's `ConversationParticipant`→`Conversation`. `DrugName` is free text, not a Products/Masters catalog reference: Pharmacy has no structured `Prescription` entity with dose/route/frequency fields to source this from (confirmed at plan time — Pharmacy is direct-dispense only), so this mirrors what's actually printed on a discharge summary.
2. **The aggregate owns a private `List<DischargeMedication>` backing field** (`DischargeSummary.Medications`, `UsePropertyAccessMode(Field)` in `DischargeSummaryConfiguration`) — the exact same shape `Patient.Allergies`/`Patient.EmergencyContacts` already use in this codebase for a 1:many owned child collection, rather than inventing a new pattern. `ReplaceMedications(IEnumerable<DischargeMedication>, Guid? updatedBy)` clears and repopulates the in-memory collection; EF Core's change tracker diffs that against what `DischargeSummaryRepository.GetByIdAsync` loaded (now `.Include(x => x.Medications)`) and issues the DELETEs/INSERTs on `SaveChangesAsync` — no explicit `RemoveRange`/`AddRangeAsync` calls in the repository, unlike a plain delete-then-reinsert would need, because the aggregate's own tracked navigation does the diffing. `OnDelete(DeleteBehavior.Cascade)` (not `Restrict`, unlike `ConversationParticipant`'s FK) since a discharge summary's medications have no independent lifecycle — soft-deleting or (in principle) hard-deleting the parent should never leave orphaned lines.
3. **List-sync, no per-line CRUD**: `UpdateDischargeSummaryRequest.Medications` is the full list on every `PUT` — `DischargeSummaryService.UpdateAsync` builds fresh `DischargeMedication.Create(...)` entities from the request (each carrying the parent's already-known `Id`) and calls `ReplaceMedications` once. An empty `Medications` array is a legitimate request that clears every line — matches the approved plan's "simple list-sync, no separate per-line CRUD endpoints" scope exactly.
4. **Quantity/duration fields are bounded to non-negative, not required** (`DischargeMedicationRequestValidator`) — only `DrugName`/`Dose`/`Route` are `NotEmpty`. A line with all-zero quantities is a legitimate (if unusual) "stop this drug" entry, so `GreaterThanOrEqualTo(0)` was chosen over `GreaterThan(0)`.
5. **Final migration**: `AddDischargeMedications` (context `DischargeSummaryDbContext`), `dotnet ef migrations add`-generated — confirmed the generated SQL includes the real `fk_discharge_medications_discharge_summary_id` foreign-key constraint with `ON DELETE CASCADE`, not just an index, before treating this as done.

**Consequences**
- 9 new tests (`DischargeMedicationTests` — Create's guard clauses and trimming; `DischargeSummaryTests.ReplaceMedications_*` — set/replace/clear; `DischargeSummaryServiceTests.UpdateAsync_*` — full list-sync round-trip and the empty-list-clears-everything case). Full `HMS.UnitTests` (810) and `HMS.ArchitectureTests` (98) suites green.
- This closes out the three approved phases (foundation, clinical content, medications) as one shippable increment, per the plan's own framing — a PDF export and the deferred Phase 4-6 items (OT integration, IPD daily-event timeline, specialty templates, the richer Prepared/Checked/Approved workflow) remain explicitly out of scope, to be sequenced later against the rest of the module-rollout backlog.
- No live browser verification for any of the three backend phases — no frontend exists yet for this module; the eventual frontend build is expected to do its own end-to-end pass once it exists, consistent with [[feedback_no_live_verification_per_fix]].

---

### ADR-058: Discharge Summary module Phase 2 — clinical/examination/vitals/surgical/advice fields, Update
**Date:** 2026-09-08
**Status:** Accepted

**Context**
Continuation of ADR-057. Phase 2 fills in the actual document content the plan's data model specified: chief complaints/history, section-by-section examination findings, a one-time vitals snapshot, a single hospital-course narrative, manual Surgical Details (no OT module to integrate with yet), and Discharge Advice — plus the `PUT` endpoint that lets a Draft actually be edited.

**Decision**
1. **One `UpdateClinicalDetails` domain method, one `PUT`, no per-section endpoints** — the approved plan scoped this as a full-record update (every field submitted together), not per-field `PATCH`; matches `UpdateDischargeSummaryRequest` carrying all ~36 fields at once. `DischargeSummaryService.UpdateAsync` is the only place that checks `Status == Draft` (`DischargeSummaryErrorCodes.NotDraft` otherwise, mapped to 403 per the plan's own "403 once Finalized" wording) — the domain method itself doesn't re-check its own state, same convention as `Finalize`/`Admission.TransferBed`.
2. **Every free-text field is trimmed and blank-normalized to `null`** in the domain method (`Normalize`), so "the doctor cleared this field" and "never filled in" are indistinguishable in storage — deliberately simple, no separate "explicitly cleared" tombstone.
3. **Vitals are a single snapshot** (`HeightCm`/`WeightKg`/`PulseRate`/`RespiratoryRate`/`TemperatureF`/`SpO2Percent`/`BloodPressure`), not a repeating observations table — matches the plan's explicit scope call (a full vitals-history feature is a materially bigger feature than this increment). `UpdateDischargeSummaryRequestValidator` bounds each numeric vital to a generous physiological range (e.g. `SpO2Percent` 0–100, `TemperatureF` 70–115) purely as basic sanity-checking, not clinical validation.
4. **Column max-lengths mirror the validator's `MaximumLength` rules exactly** (`DischargeSummaryConfiguration`), split into three rough tiers — short fields (surgeon names, `Gait`, `BloodPressure`) at 500/20, most narrative sections at 1000–2000, and four genuinely long free-text fields (`HistoryOfPresentingIllness`, `CourseInHospital`, `IntraOperativeFindings`, `OperativeNotes`) at 8000 — following this codebase's existing convention (`Admission.FinalDiagnosis`/`DischargeNotes` etc. also use `HasMaxLength`, never unbounded `text`) rather than introducing an unbounded column type for this module alone.
5. **One more `dotnet ef migrations add`-generated migration**, `AddDischargeSummaryClinicalFields` (context `DischargeSummaryDbContext`) — purely additive columns, no data migration needed since Phase 1 shipped with zero real rows in any tenant yet.

**Consequences**
- 5 new tests (`DischargeSummaryTests.UpdateClinicalDetails_*`, `DischargeSummaryServiceTests.UpdateAsync_*`) covering the full-field-set update, blank-normalization, the Draft-only gate, and not-found — full `HMS.UnitTests`/`HMS.ArchitectureTests` suites stay green.
- No live browser verification — still backend-only, no frontend yet, same reasoning as ADR-057.
- `DischargeMedication` (the discharge medications table and the Update endpoint's list-sync behavior) is Phase 3, its own ADR entry.

---

### ADR-057: Discharge Summary module Phase 1 — scaffold, Draft/Finalize, permissions, feature wiring
**Date:** 2026-09-08
**Status:** Accepted

**Context**
User shared a real hospital discharge-summary PDF and a detailed breakdown of a proper Discharge Summary feature, proposed a 6-phase rollout, and — sequenced against the module-rollout backlog with no Operation Theater module existing yet (`Modules/OT` has zero files) — approved building Phases 1-3 (foundation, clinical content, medications) as one shippable increment. This entry covers Phase 1: the module scaffold, the `DischargeSummary` aggregate's base fields, Create/Get/Finalize endpoints, and every piece of shared infrastructure a brand-new module needs (permissions, `FeatureCatalog`, `TenantMigrationService`, DI, migrations).

**Decision**
1. **New module, not folded into IPD**: `HMS.Modules.DischargeSummary`, schema `discharge_summary`, scaffolded from `HMS.Modules.Identity` per `docs/DeveloperHandbook.md` §19 — internal Domain/Application/Infrastructure, public Contracts, `Guid.CreateVersion7()` ids, `xmin` concurrency, `HasQueryFilter` soft delete, validators registered explicitly in `DischargeSummaryModule.cs` (not `AddValidatorsFromAssemblyContaining`, which only finds public `IValidator<T>`s — the documented gotcha).
2. **`AdmissionId`/`PatientId` are bare Guids, no DB FK** — validated at the Application layer through IPD's public `IAdmissionService` and Patients' public `IPatientService`, mirroring `PharmacyStockTransaction`'s identical treatment and `TenantMigrationService`'s own comment on why cross-schema FKs are never used in this codebase.
3. **Creation gate + uniqueness**: `CreateDraftAsync` fails with `AdmissionNotDischarged` unless `Admission.Status == Discharged`, and with `AlreadyExists` if a summary already exists for that `AdmissionId` (check-then-create in the service, backed by a unique index on `admission_id` as a database-level backstop — the same reasoning `PharmacyStockTransaction`/`Admission` already require this pattern for cross-module Guid references). `Admission.FinalDiagnosis` is copied once, at create time, into `DischargeSummary.FinalDiagnosis` as its initial value — independently editable from that point, never re-read from Admission afterward.
4. **Two states only**: Draft → Finalized. The richer Prepared/Checked/Approved sign-off pipeline from the user's own point #9 is deliberately NOT a multi-step workflow — `PreparedByUserId`/`CheckedByUserId`/`ConsultantApprovedByUserId` are three optional Guids captured once, together, at `Finalize`. All three are stored as **opaque Guids with no cross-module validation against Identity**, even though `IUserService.GetByIdAsync` exists and could validate them — deliberately, to avoid giving this module a real dependency on `HMS.Modules.Identity.Application` (which would need its own carve-out in `HMS.ArchitectureTests.Modules.Identity.CrossModuleDependencyTests`, mirroring Notifications/ADR-032). The frontend already has `GET /api/v1/users/directory` (built for Messaging) to populate these pickers with real users; a bad Guid here has no cascading effect, so the extra dependency wasn't judged worth it. `DischargeSummary` stays on the existing blanket "no dependency on Identity" list in `CrossModuleDependencyTests` instead.
5. **New `discharge-summary` RBAC permission category**, not folded into IPD's existing `clinical-care` — a deliberate, narrow departure from ADR-022's "reuse an existing category" default. `Finalize` is a genuinely new action outside the view/create/edit/delete set every other category uses, and the approved plan named these four permission strings explicitly (`discharge-summary.view/create/edit/finalize`) as the contract the frontend would be built against. Because `Permission.Module` is filtered against a tenant's `EnabledModules` at login (ADR-026 — `AuthenticationService.LoginAsync` strips any permission whose `Module` isn't in that list), a permission `Module` value that ADR-022 never turned into a real category would be silently and permanently stripped from every tenant's JWT, locking everyone out of the new endpoints. So `HMS.Shared.Kernel.ModuleCatalog.All` gained a 12th entry, `"discharge-summary"` (new tenants' `Tenant.EnabledModules` defaults to the full `ModuleCatalog.All`, per `Tenant.Create`) — this is the one piece of plumbing the plan didn't anticipate, found by tracing how a new permission module actually reaches a JWT, not by assumption.
6. **New `FeatureCatalog.SchemaBacked` key `"discharge-summary"`**, wired into `TenantMigrationService` (migrates `DischargeSummaryDbContext` when resolved), placed after `"ipd"` in migration order for readability (matches the Admission dependency), same as Laboratory's placement after Billing.
7. **API routes deliberately split across two path prefixes** per the approved plan: `POST /api/v1/admissions/{admissionId}/discharge-summary` (create) and `GET /api/v1/admissions/{admissionId}/discharge-summary` (fetch-by-admission) use ASP.NET Core's absolute-route override (`[HttpPost("/api/v1/admissions/...")]`) inside a controller otherwise rooted at `api/v1/discharge-summaries` — a discharge summary is always reached starting from a specific admission for those two actions, everything else (`GET/PUT/finalize {id}`) addresses the summary directly.
8. **Two new EF Core migrations**, both `dotnet ef migrations add`-generated (never hand-written, per §20/ADR-005's lesson): `InitialCreateDischargeSummary` (context `DischargeSummaryDbContext`) and `AddDischargeSummaryPermissions` (context `IdentityDbContext`, inserting the 4 new seeded `Permission` rows via `PermissionSeedData.cs`'s existing `HasData` mechanism — the same reason every prior new-permission change, e.g. `AddPatientManagementImportPermission`, needed its own Identity migration).

**Consequences**
- 14 new `DischargeSummaryTests`/`DischargeSummaryServiceTests` (Domain Create/Finalize invariants; Application service's admission-gate, duplicate-prevention, patient-validation, and already-finalized-rejection logic, mocked repository/cross-module service interfaces, mirrors `AdmissionServiceTests`' pattern) plus a new `DischargeSummaryModuleBoundaryTests` (mirrors `MessagingModuleBoundaryTests`) — all green, alongside the full existing `HMS.UnitTests`/`HMS.ArchitectureTests` suites (796/98 passing).
- No live browser verification for this phase — backend-only, no frontend yet, consistent with [[feedback_no_live_verification_per_fix]]'s reasoning and every prior backend-only phase in this codebase (e.g. ADR-030).
- Update/Finalize's full clinical content, examination, vitals, surgical, and discharge-advice fields, plus the medications child entity, are Phase 2/3 — deferred to their own ADR entries, not because they're architecturally uncertain but because they're a large, mechanical field addition better reviewed as separate, focused commits.

---

### ADR-056: Removed the redundant "Laboratory Workflow" top-level sidebar entry
**Date:** 2026-09-03
**Status:** Accepted

**Context**
User-reported: the sidebar showed both "Central Laboratory" and a separate "Laboratory Workflow" entry directly beneath it — but "Central Laboratory" is a hub page (`CentralLaboratoryHubPage.tsx`) whose own "Workflow" section already links to the exact same destination (`/diagnostics/lab/dashboard`) via a card. Confirmed this is a one-off inconsistency, not a systemic pattern: Pharmacy, Radiology, and Blood Bank each have exactly one top-level sidebar entry apiece, relying on their own hub page for sub-navigation, same as Central Laboratory's hub page already does.

**Decision**
Removed the standalone `'Laboratory Workflow'` leaf from `navigation.ts` (and its now-unused `Microscope` icon import). Confirmed safe: `/diagnostics/lab/dashboard`'s actual route was never derived from that nav leaf in the first place — `routeGatedLeafPaths` in `routes.tsx` already excludes it from the auto-generated nav-driven routes, since it's wired up separately via `labWorkflowRoutes` with its own real `RequireFeatureRoute`/`RequirePermissionRoute` guard. `getAllLeaves()` (the removed entry's only other consumer) has no other callers. The route, its guard, and the hub page's own card linking to it are all untouched — only the redundant sidebar entry is gone.

**Consequences**
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-055: OPD Billing Entry stops re-offering a visit's already-billed consultants
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Live-verification follow-up while checking on ADR-039 (on-call consultation charge): the user's real report turned out to be a different, genuine bug — a visit's consultants (from Registration Details) kept being pre-filled into Consultation Billing at ₹0 even though that visit had already been billed. `InvoiceCreatePage.tsx` already had a guard for exactly this (`consultationAlreadyBilledToday`), but it had two real gaps: (1) scoped to "any Consultation-type invoice item created **today**" — a visit billed on an earlier day got no guard at all; (2) matched "any invoice for this **patient**," not this specific visit — a patient with more than one visit could get a false match or, as in this case, no match at all if the check's date/patient scoping didn't line up with the actual billed visit.

**Decision**
Replaced the guard with `consultationAlreadyBilledForVisit`, keyed to `Invoice.VisitId` (which does exist and, unlike a line item's `ConsultantId`, is never cleared by `MarkPaid()` — see ADR-048) matching the specific visit being prefilled, with no date restriction. Deliberately whole-visit rather than per-consultant: since ADR-048 clears `ConsultantId` on payment, a *paid* line item's specific consultant can no longer be matched individually from invoice data — but billing every consultant on a visit together in one invoice is the realistic common case anyway, so a whole-visit guard is the meaningful fix here, not an under-scoped one.

**Consequences**
- Once any non-voided Consultation-type invoice item exists for a visit, that visit's Consultation Billing card starts blank on the next OPD Billing Entry session for that patient (same fallback behavior the old guard already had) — reception can still add a fresh Consultation row manually if a genuinely new/different consultant needs billing for that visit.
- No frontend automated test added — no test runner exists in this repo (see ADR-038).
- **Also discovered and fixed while investigating this**: ADR-052's own decision-log entry had been accidentally deleted (header line and separator only — the body survived) during an unrelated editing mistake made while resolving ADR-053. Restored from git history (commit 7ee6d45) verbatim; the ADR numbering 001-054 is now confirmed fully sequential with no gaps.

---

### ADR-054: Patient visit list shows every consultant directly, not just the first behind a hover tooltip
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Twenty-first item of a user-supplied 22-issue backlog, re-scoped by the user mid-session from the original vague "Recent Visits +1" wording (which never reproduced anywhere in the code — see the earlier investigation note) into a concrete, confirmed bug: a visit with 2 or 3 consultants ("Add another Consultant" on Registration Details) only showed the first one in the Patient View's Visits table, with the rest hidden behind a "+N" badge that needed a hover to reveal — not actually visible at a glance, and unreachable at all on touch devices. Backend already fully supported multiple consultants per visit end to end (`PatientVisitConsultation` is a genuine 1:many child of `PatientVisit`, and `PatientVisitMappingExtensions` already mapped every one of them into the API response) — the bug was purely in `PatientDetails.tsx`'s `ConsultantsCell` rendering.

**Decision**
Rewrote `ConsultantsCell` to render every consultant on the visit directly, comma-separated, wrapping naturally within the table cell — removed the tooltip-hide mechanism entirely. Handles 1, 2, 3, or any number of consultants the same way, with no special-casing at any particular count. Both surfaces that show a patient's visits (the "Recent Visits" card and the full "Visits" tab) share the same `VisitsTable`/`ConsultantsCell` component, so the fix covers both automatically.

**Consequences**
- Live-verified end to end by the user themselves with real data (a visit with 2 consultants, both now showing directly: "Dr. K. Bala Ganesh, M.D., D.M (Cardio), Dr. E. Kandaswamy, M.D., D.M (Gastro)") — confirmed working, including as a side effect confirming the UHID-restart fix (ADR-043) is also live (`P-2026-040001`).
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-053: DatabaseArchitecture.md updated — multi-tenancy is implemented, not a future option; tenant-provisioning code layout documented
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Twentieth item of a user-supplied 22-issue backlog ("Review and organize the Tenant folder structure"). Investigation (already done in a prior session pass) found there's no single "Tenant" folder — provisioning code is deliberately split three ways (`Modules/Platform` owns the `Tenant` aggregate, `HMS.Api/Provisioning` does the actual `CREATE DATABASE`/migrate work since it needs a reference to every module's `DbContext`, `HMS.Api/Middleware` resolves the runtime tenant per request), a documented, intentional layering rather than disorganization. Separately, closer reading of `docs/DatabaseArchitecture.md` found it genuinely stale: its top summary still says "single-tenant (MVP)" and its §1 "Future migration path to multi-tenancy" describes database-per-tenant as a hypothetical future option — but that's exactly what already shipped (ADR-013 onward). At least 10 code comments across the codebase already reference "docs/DatabaseArchitecture.md's SaaS provisioning ADR," content that never actually existed in the file.

**Decision**
Updated `docs/DatabaseArchitecture.md`: the top summary line and §1's stale "future migration path" subsection now state multi-tenancy is implemented (database-per-tenant), not deferred. Added new §13 "Multi-Tenancy — SaaS Provisioning" documenting the actual three-way code split with real file paths and the reasoning already captured in `ITenantProvisioner`'s own doc comment (avoiding a circular module dependency) — this is now the actual target of every existing "SaaS provisioning ADR" code comment. Renumbered the old §13 "Deliverables" to §14 to make room; no other section content changed.

**Consequences**
- Purely documentation — no code or test changes. The ~10 existing code comments that reference this doc by name weren't individually updated to cite the new §13 specifically (they already correctly name the file; adding a section number to each is cosmetic precision not worth a 10-file sweep for a Tier-2 documentation item).
- No PR test plan beyond confirming the new section numbering is sequential with no gaps (verified: 1-14, no duplicates).

---

### ADR-052: Local dev/Windows installer stop creating a separate hms_qa database
**Date:** 2026-09-02
**Status:** Accepted

**Context**
User report while testing a fresh install: expected only `hms_platform` to be created, but `hms_qa` also appeared, unwanted. Root cause: `ConnectionStrings:Default` (in `appsettings.Development.json` and `scripts/deploy/windows/install-api-service.ps1`) pointed at a separate physical database named `hms_qa`. `Bootstrap:SeedLegacyTenant` is already `false` in the checked-in dev config, so the *full* legacy tenant (Identity/Masters/Patients/etc.) was never actually seeded there — but `BrandingDbContext` is deliberately **not** tenant-aware (it powers the anonymous pre-login theme screen, before any tenant is known) and always migrates against `ConnectionStrings:Default` regardless of `SeedLegacyTenant`, per `Program.cs`'s own existing comment. That's what was actually creating `hms_qa`: an otherwise-empty database holding only Branding's schema.

**Decision**
Pointed `ConnectionStrings:Default` at the same physical database as `ConnectionStrings:Platform` (`hms_platform`) in both `appsettings.Development.json` and the Windows installer script, instead of a separate `hms_qa`. Safe because `BrandingDbContext` uses its own `branding` schema (`HasDefaultSchema`), fully isolated from Platform's own `platform` schema in the same physical database — confirmed no EF migrations-history or table-name collision risk. A fresh install now creates exactly one database.

**Consequences**
- **Deliberately scoped to local dev + the Windows installer only** — `.env.example`/`docker-compose.yml` (the Docker deployment path) were left untouched. That path is materially different: `Bootstrap:SeedLegacyTenant` isn't set there at all, and defaults to `true` (Production environment never loads `appsettings.Development.json`), so simply renaming `TENANT_DB_NAME` to match `PLATFORM_DB_NAME` there would jam the *full* legacy tenant's schemas into `hms_platform`, not just Branding's — a materially different, riskier change than what was asked. Flagged as a related but separate gap: `docs/Deployment.md` already recommends `Bootstrap__SeedLegacyTenant=false` for production, but neither `.env.example` nor `docker-compose.yml` expose or default that variable, so a Docker deployment following the example file today seeds the legacy tenant by default, contradicting that doc's own guidance — worth a follow-up if the Docker path is ever the one being freshly installed.
- Lightly updated three stale/inaccurate code comments (`Program.cs`, `PlatformDbContext.cs`) that referenced `hms_qa` by name or asserted Platform/Default always use separate physical databases, which is no longer true for this path.

**Note (2026-09-09):** This Context's premise that `BrandingDbContext` is "deliberately not
tenant-aware" no longer holds — see ADR-072, which makes Branding tenant-aware like every other
module. This ADR's actual Decision (merging `ConnectionStrings:Default` with `hms_platform`
locally) is unaffected and still stands on its own for the legacy-tenant reason alone.

---

### ADR-051: Masters edits now invalidate the dedicated picker components' own query caches
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Live-verification follow-up on ADR-049 (Consultant Priority): the user set a Consultant's Priority via Masters → Consultant → Edit, saved successfully, but the Billing/Registration consultant dropdown kept showing the old order — asked whether backend seed data was missing. It wasn't a backend issue at all: `useMasterMutations.ts`'s `useInvalidateMasters` only invalidates `['masters', entityKey]`, the Masters admin screen's own cache. Four entities — Consultant, Department, AppointmentType, ConsultationType — also have their own dedicated picker component (`ConsultantSelect`, `DepartmentSelect`, etc., built before this generic Masters engine existed) querying under a *different*, unrelated top-level cache key (e.g. `['consultants', 'select-list', departmentId]`). Saving a Masters edit never touched that second cache, so every picker elsewhere in the app kept serving its stale list until something else happened to refetch it (a full page reload, cache staleness). One Masters entity, Designation, doesn't have this problem — `DesignationSelect` already queries under `['masters', 'designation', 'select-list']`, so it was already covered.

**Decision**
`useInvalidateMasters` now also invalidates a second, entity-specific query key for the four affected entities, via an explicit `PICKER_QUERY_KEY_BY_ENTITY` map (not a computed pluralization — safer than guessing "+s" would hold for every current and future entity key). react-query's `invalidateQueries` matches by prefix, so invalidating the one-element key (e.g. `['consultants']`) covers every differently-suffixed variant a picker or name-lookup component might use (`ConsultantSelect`'s `[...,departmentId]` and `ConsultantName`'s plain 2-element key both match).

**Consequences**
- Fixes the reported symptom for all four affected entities at once, not just Consultant — Department/AppointmentType/ConsultationType edits had the identical stale-picker bug, just not yet noticed/reported.
- If a future Masters entity gains its own dedicated picker component with a separate query key (rather than following Designation's `['masters', entityKey, ...]` convention), it needs a new entry in `PICKER_QUERY_KEY_BY_ENTITY` — flagged directly in the map's own comment.
- No frontend automated test added — no test runner exists in this repo (see ADR-038). Not yet re-verified live by the user against this specific fix — the original report is what surfaced it.

---

### ADR-050: Unsaved-changes guard extracted into a reusable hook, applied to Patient Registration/Edit/Add Visit
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Eleventh item of a user-supplied 22-issue backlog ("Show an unsaved changes alert when leaving a page without saving"). A complete implementation of this already existed, but only on `InvoiceCreatePage.tsx` (`isDirty`/`isDirtyRef` + `useBlocker` for in-app navigation + a `beforeunload` listener for tab close/refresh + a confirm/discard dialog) — Patient Registration, Patient Edit, and the standalone "Add Visit" page had none of it.

**Decision**
1. Extracted the reusable half of InvoiceCreatePage's original pattern into `frontend/web/src/hooks/useUnsavedChangesGuard.ts` (takes `isDirty: boolean`, returns `{ showUnsavedDialog, confirmDiscard, cancelDiscard, markSaved }`) and `frontend/web/src/components/UnsavedChangesDialog.tsx` (the paired confirm/discard dialog). `InvoiceCreatePage.tsx` itself was left untouched — its own `isDirty` is driven by a custom `BillingStep` ref, not a plain React Hook Form instance, and it has an extra bespoke "change patient" case the new hook doesn't need to model; not worth forcing it onto the new shared hook in the same pass as adding the guard elsewhere.
2. Each of `PatientRegistrationForm.tsx`/`PatientEditForm.tsx`/`RecordVisitForm.tsx` gained an `onDirtyChange?: (isDirty: boolean) => void` prop, wired from React Hook Form's own `formState.isDirty` via a `useEffect`. The guard hook itself lives in each form's *parent page* (`PatientRegistrationCreatePage`/`PatientEditPage`/`PatientRecordVisitPage`), not the form component — the parent is where the post-save `navigate()` call already lives, and that's exactly where the guard needs to be told "this navigation is fine, a save just succeeded."
3. **`markSaved()`, not just passing `isDirty={false}`**: React Hook Form's `formState.isDirty` doesn't reset until a `reset()` call takes effect on a *later* render, so a `navigate()` fired immediately after a successful save could still see the pre-save `isDirty=true` and get wrongly blocked by the guard's own confirm dialog. `markSaved()` writes straight to the hook's internal ref (bypassing the render/effect round-trip) and is called synchronously right before each success-path `navigate()` call in all three parent pages. Note this is a genuinely new addition, not something lifted from `InvoiceCreatePage.tsx` — that page's own `setIsDirty(false)` on save success never needed this fix, because its success path doesn't `navigate()` at all (it swaps the editable form for a read-only view in place, same route); the three new call sites all do navigate on success, so they needed the extra care `InvoiceCreatePage` never had to take.

**Consequences**
- Live browser verification (a genuinely new, stateful, cross-navigation UI interaction) could not be completed — the only dev login path needs the seeded Super Admin password, which lives solely in `dotnet user-secrets`, correctly blocked from being read by the auto-mode safety classifier (same gap noted in ADR-040/041). Verified instead via `tsc --noEmit` + `eslint` (both clean) and by tracing through the exact render/effect timing `markSaved()` is meant to sidestep.
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-049: Consultant sort order — a real per-consultant Priority field, not a hardcoded name match
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Tenth item of a user-supplied 22-issue backlog, originally worded as "Dr. Karthikeyan's name should appear at the top ahead of other consultants" in the Registration Details consultant search. Rather than hardcoding one specific name into `ConsultantRepository.ApplySort` (the original plan's default, before this session's fix), the user asked for a general, admin-configurable priority/weightage field on the Consultant record itself, applied consistently everywhere consultants are picked (Registration, Billing, etc.) — confirmed direction: lower number = higher priority.

**Decision**
Added `Consultant.Priority` (`int?`, nullable — unset consultants sort after every prioritized one) end to end: `Domain/Consultant.cs`, `CreateConsultantRequest`/`UpdateConsultantRequest`/`ConsultantResponse`, both validators (`Priority >= 1` when supplied), the mapping extension, `ConsultantService`, `ConsultantConfiguration` (new nullable `priority` column), and a new `AddConsultantPriority` migration. `ConsultantRepository.ApplySort`'s default case (used whenever no explicit `sort` is passed — which is every real caller, since `ConsultantSelect.tsx` never sets one) now orders by `Priority ?? int.MaxValue` then `Name`, replacing the old plain alphabetical default. Frontend: added one `priority` field (`type: 'number'`, `min: 1`) to the Consultant Masters config (`frontend/web/src/features/masters/configs/consultant.ts`) — Masters is a fully config-driven CRUD engine, so this single field addition automatically gets a form input, list column, and client-side validation with zero other frontend code.

**Consequences**
- Every consumer of `ConsultantSelect` (Patient Registration, Billing's `ConsultationBillingCard`, IPD admission, etc.) picks up the new ordering automatically and uniformly — no per-page changes needed, since none of them ever pass an explicit `sort`.
- To actually put a specific consultant (e.g. Dr. Karthikeyan) at the top, an admin now sets their Priority via the Masters → Consultant edit page — this ships the mechanism, not a pre-set value for any particular doctor, matching "a real per-consultant field beats a single hardcoded name match."
- Covered by 10 new unit tests (`ConsultantServiceTests`, `ConsultantValidatorTests`) — no prior test coverage existed for Consultant at all in this codebase; added a minimal but real suite rather than leaving the new field untested. No repository-level test for the actual sort-query translation (see ADR-037's identical note — no DB-backed test harness exists anywhere here).

---

### ADR-048: Consultant cleared from a billing line item once it's paid
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Ninth item of a user-supplied 22-issue backlog ("A consultant is always assigned to a patient, even after paid — Remove this feature"). `InvoiceLineItem.MarkPaid()` set `PaymentStatus = Paid` but never touched `ConsultantId`, so a paid line item kept showing its consultant indefinitely — user confirmed (after clarification) that the consultant field should be cleared once a line item is paid.

**Decision**
`MarkPaid()` now also sets `ConsultantId = null`. Confirmed via search that no report or endpoint in the Billing module aggregates by `ConsultantId` on paid items (`Payment` itself never carried a `ConsultantId` either), so nothing downstream depends on it surviving payment. The frontend needed no change — `describeBillingItem` in `billingCalculations.ts` already renders `'—'` for a null `consultantId`.

**Consequences**
- **Genuine, permanent loss of attribution**: once a line item is paid, there is no other record anywhere of which consultant it was originally billed under (`Payment` doesn't carry one either). Acceptable per the fix's own "remove this feature" framing, but a future "per-consultant paid revenue" report would need a new field that survives payment, not a reuse of this one — flagged directly in `MarkPaid`'s own doc comment.
- Covered by a new unit test (`RecordPaymentAsync_WithValidLineItem_ClearsTheConsultant`) plus an added assertion on the existing pay-at-creation test — both passing alongside the full 735-test suite.

---

### ADR-047: State/City on hospital creation — State is a real dropdown, City is a suggested (not locked) free-text field
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Sixteenth item of a user-supplied 22-issue backlog ("Add State and City dropdowns to the hospital creation page"). `HospitalForm.tsx`'s City/State were both plain free-text inputs. A real Masters `State`/`District` catalog already exists and already has reusable picker components (`StateSelect.tsx`/`DistrictSelect.tsx`, backing Patient Registration's Address section) — but that catalog is tenant-scoped (`GET /api/v1/masters/states` resolves against a specific hospital's own database via `TenantResolutionMiddleware`), and hospital creation runs *before* any tenant database exists. There is nothing yet to query it against, so those existing components can't be reused here; Platform Portal needs its own standalone reference data.

**Decision**
1. Added `frontend/web/src/features/platformHospitals/indiaStatesAndCities.ts` — a static, closed list of all 28 states + 8 union territories (`INDIAN_STATES_AND_UTS`), and a representative (not exhaustive) `CITIES_BY_STATE` map of major cities per state.
2. `State` is now a real `<Select>` (the closed list is genuinely exhaustive — India has a fixed, well-known set of states/UTs, so a hard picklist has no false-negative risk). Changing it clears `city`, mirroring `PatientRegistrationForm`'s established Department→Consultant / State→District reset pattern.
3. `City` stays a free-text `<Input>`, deliberately **not** a locked picklist — paired with a native `<datalist>` scoped to the selected state for dropdown-style suggestions. An exhaustive list of every Indian city isn't realistic to maintain, and a real hospital's actual city missing from a strict dropdown would block onboarding entirely; a suggestion list gets the "dropdown" UX the ask wanted without that failure mode.

**Consequences**
- No backend change: `CreateHospitalRequestValidator`/`Tenant.Create` already treat `city`/`state` as plain non-empty strings — a real state name now always reaches it (Select-enforced), city remains an arbitrary string as before.
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-046: Reusable PasswordInput (show/hide toggle) added and applied to every password-creation field
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Fifteenth item of a user-supplied 22-issue backlog ("Add a Retype Password field with an eye icon to show/hide the password" on hospital creation). `HospitalForm.tsx` had one plain `type="password"` field, no confirm field, and no show/hide toggle anywhere in the app — a repo-wide search found zero existing show/hide-password component to reuse (`Eye`/`EyeOff` from `lucide-react` were unused).

**Decision**
1. Built `frontend/web/src/components/ui/password-input.tsx` — a small `PasswordInput` wrapping the existing `Input` with an `Eye`/`EyeOff` toggle button that only flips the input's own `type`, not form state.
2. `HospitalForm.tsx`: added `superAdminConfirmPassword` to `createHospitalSchema` (client-only, `.refine`-checked against `superAdminPassword`; not part of `CreateHospitalRequest` — `CreateHospitalPage.tsx`'s `handleSubmit` destructures it out before calling the mutation so it's never sent to the API) and a "Retype Password" field, both using `PasswordInput`.
3. Applied the same new component to every other place a password is typed and would benefit from a reveal toggle, since it was trivial and directly consistent with what was just built: `SetPasswordDialog.tsx` (admin reset — already had confirm-password, just no toggle), the tenant `ChangePasswordPage.tsx`, and the Platform `ChangePasswordCard` added in ADR-045. **Deliberately left the two login pages (`LoginPage.tsx`/`PlatformLoginPage.tsx`) untouched** — a reveal toggle at sign-in time is a separate UX call nobody asked for, not the same "did I type my new password correctly" concern the other five fields share.

**Consequences**
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-045: Platform Admin self-service password change
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Fourteenth item of a user-supplied 22-issue backlog ("Password Reset Option" for Platform Portal / Hospital Management). Confirmed gap: `PlatformAuthenticationService` had no password-mutation method at all — only login/MFA. Unlike the tenant side (`HMS.Modules.Identity`, which has both a self-service `POST /api/v1/auth/change-password` and an admin-triggered `SetPassword` reset with a forced-change-on-next-login flag, ADR-023), Platform Admin accounts had neither.

**Decision**
Added the self-service half only, mirroring Identity's `ChangePasswordAsync`/`ChangePasswordRequest`/`ChangePasswordRequestValidator` pattern exactly (same shared `PasswordPolicy`, same "verify current password, then rotate" shape): `PlatformUser.ChangePassword(newPasswordHash)` (domain), `PlatformChangePasswordRequest`/`PlatformChangePasswordRequestValidator`, `IPlatformAuthenticationService.ChangePasswordAsync`, `POST /api/platform/auth/change-password` (`[Authorize(Policy = "Platform")]`), and a `ChangePasswordCard` on `PlatformSecuritySettingsPage.tsx` reusing the existing shared `changePasswordSchema`/`ChangePasswordFormValues` (the shape is identical to Identity's, no Platform-specific schema needed). **Did not** add an admin-resets-another-admin flow (no cross-admin authorization model exists for Platform accounts today, and the user's ask didn't specifically call for it) — scoped to self-service only, per the plan's explicit note to confirm before expanding scope; flag to the user if cross-admin reset turns out to be what's actually wanted.

**Consequences**
- **DI registration risk, deliberately guarded against**: a prior fix on this codebase hit a real incident where a new `IValidator<T>` was added to Identity but never registered in DI, causing every hospital login to 500. Registered `IValidator<PlatformChangePasswordRequest>` in `PlatformModule.cs` alongside the other Platform validators, then verified live (not just by reading the registration) by hitting the anonymous `POST /api/platform/auth/login` endpoint with a bogus body via the running dev API and confirming a normal 400 validation response rather than a 500 — since ASP.NET Core resolves *every* constructor dependency of a controller on first use regardless of which action is called, this proves the new validator resolves correctly without needing real Platform Admin credentials (which this session cannot obtain — `dotnet user-secrets` is classifier-blocked).
- No repository-level or full end-to-end login test — verified via unit tests (`ChangePasswordAsync_WithCorrectCurrentPassword_RotatesTheHash`/`_WithWrongCurrentPassword_...`) plus the live DI-resolution check above.

---

### ADR-044: Patient View gender icon now matches the patient's actual gender
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Twelfth item of a user-supplied 22-issue backlog ("Gender Symbol should be fixed in the patient view form"). `PatientSummaryCard.tsx` rendered the same generic `VenusAndMars` (⚥, combined Venus/Mars) icon next to every patient's gender text regardless of `patient.gender` — the only place in the Patients feature that shows gender as a symbol/icon at all (every other screen renders it as plain text or a select).

**Decision**
Added a small `GenderIcon` component mapping each real `Gender` value (`Male`/`Female`/`Transgender`/`NA`) to its own `lucide-react` icon (`Mars`/`Venus`/`Transgender`), falling back to the original combined `VenusAndMars` glyph only for `NA` (not known/not recorded), where no single gendered symbol would be accurate.

**Consequences**
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-043: UHID sequence restarted at 40001
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Eighth item of a user-supplied 22-issue backlog ("UHID should be changed, it should start from 40,001"). UHID is generated by `PatientIdentifierGenerator.NextUhidAsync` as `P-{year}-{sequence:D6}` from a real Postgres sequence (`patients.uhid_seq`, `PatientsDbContext.cs`), previously `StartsAt(1)`. The user was asked whether "start from 40,001" meant restarting this existing sequence (keeping the `P-YYYY-NNNNNN` format) or switching to a bare running number with no prefix, and said no preference — proceeded with the smaller, less invasive change: restart the existing sequence, format unchanged.

**Decision**
Changed `PatientsDbContext.OnModelCreating` to `StartsAt(40001)` and generated migration `RestartUhidSequenceAt40001` via `dotnet ef migrations add` — EF Core produced a `RestartSequence` operation (`ALTER SEQUENCE ... RESTART WITH 40001` under Npgsql), not just a model-metadata change, so this correctly affects both a brand-new tenant's sequence (created via the existing `CreateSequence` in `InitialCreatePatients`, then immediately restarted by this migration) and every already-provisioned tenant's sequence once they run this migration — the next patient created after this migration runs gets `P-{year}-040001`, regardless of whatever the sequence's current value was.

**Consequences**
- This restarts the counter forward for every tenant, including ones that already have patients — if a tenant somehow already had 40,000+ patients (none currently do, per this app's early rollout stage), this would risk a duplicate UHID; not a concern at present scale but worth remembering if this migration is ever run against a much older, larger production tenant later.
- No repository-level test exists for the sequence's numeric value (no repository/DB test harness anywhere in this codebase — see ADR-037's identical note); verified via `dotnet ef migrations add` producing the expected `RestartSequence` operation, a clean build, and the full `dotnet test` suite passing (148 Patients-scoped unit tests, 88 architecture tests).

---

### ADR-042: Date of Birth restricted to a 4-digit year at both the picker and the validation layer
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Seventh item of a user-supplied 22-issue backlog ("Allow 4-digit birth years in the DOB field during registration — can go up to 6 digits currently"). The native `<input type="date">` in `PatientRegistrationForm.tsx`/`PatientEditForm.tsx` had no `min`/`max` attributes, and neither the shared Zod schema (`patientRegistrationUiValidation.ts`'s `demographicsUiSchema.dateOfBirth`) nor the backend (`CreatePatientRequestValidator`) checked the year's digit count — both only range-checked (≤ today, ≥ 130 years ago) via `new Date(value)`/`DateOnly` comparisons, which don't reject a malformed year outright.

**Decision**
1. Added `dateOfBirthInputBounds()` to `frontend/web/src/features/patients/detailedAge.ts` (the existing shared home for both forms' DOB-derived helpers) returning `min`/`max` ISO date strings (130-year floor, today's ceiling) — spread onto both `<Input type="date">` elements so the native picker itself is bounded.
2. Added a `.regex(/^\d{4}-\d{2}-\d{2}$/)` check to the Zod schema, ahead of the existing range refinements, so a malformed year is rejected outright rather than silently mis-parsed by `new Date(value)`.
3. **No backend change was needed**: `CreatePatientRequest.DateOfBirth`/`UpdatePatientRequest.DateOfBirth` are `DateOnly`-typed, and .NET's built-in `System.Text.Json` `DateOnly` converter already strictly requires the exact 10-character `yyyy-MM-dd` shape — a 6-digit-year string fails JSON model binding with a 400 before FluentValidation ever runs. Confirmed no custom `JsonConverter` is registered for `DateOnly` anywhere in the API that could loosen this. Adding a redundant backend regex would validate a scenario the type system already makes impossible.

**Consequences**
- No frontend automated test added — no test runner exists in this repo (see ADR-038).

---

### ADR-041: Login page no longer hardcodes the seed tenant's hospital code as the default
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Fifth item of a user-supplied 22-issue backlog ("logging into a newly created hospital using Super Admin works sometimes but fails at other times"). Backend tenant provisioning (`HospitalRegistrationService.RegisterCoreAsync` → `TenantProvisioningService.ProvisionAsync` → `TenantMigrationService.MigrateAsync` → `IdentityModule.ProvisionTenantSuperAdminAsync`) is fully synchronous and awaited end-to-end, so there is no real backend race. `LoginPage.tsx`'s `hospitalCode` field, however, was hardcoded to default to `'lhs'` (the original seed tenant, "Lakshmi Hospitals") with no pre-fill/redirect carrying a newly created hospital's actual code. Anyone testing a fresh hospital's Super Admin login who didn't think to clear that stale default had their login request silently resolve against the wrong tenant (via the `X-Hospital-Code` header) and fail with the same generic "Invalid username or password" a real credential error produces (deliberate anti-enumeration design) — matching the reported "works sometimes (when manually corrected), fails other times (when it wasn't)" symptom exactly, with no backend concurrency involved.

**Decision**
Replaced the hardcoded `'lhs'` default with a value read from `localStorage` (the last hospital code actually used to sign in successfully on this browser, saved on every successful login), falling back to an empty field (with the existing "e.g. cauvery" placeholder) when nothing has been saved yet. This keeps the convenience of a remembered default for the common repeat-login case without ever silently guessing a specific tenant. Live-verified visually in the browser (the field renders empty with no prior login, per the existing placeholder) — a full login-flow verification (confirming a fresh hospital's Super Admin can sign in first try with the field left as its new default) could not be completed for the same reason as ADR-040: the dev Super Admin password lives only in `dotnet user-secrets`, which the safety classifier correctly blocks reading.
Deliberately did not change the "Sign in as" role default (`superAdmin`) — that wasn't part of the diagnosed root cause (a fresh hospital's first login genuinely is a Super Admin), and touching it wasn't asked for. Also deliberately did not build a cross-app deep-link from hospital-creation success into this login page — the Platform Admin can already see the new hospital's code on the dashboard they land on after creation, and Platform Portal login/Tenant login are separate, unrelated auth contexts; wiring a redirect between them was scope this issue didn't call for.

**Consequences**
- No frontend automated test added — no test runner exists in this repo (see ADR-038/039).

---

### ADR-040: Registration Details tab clears admissionType/category/referral when encounter type changes
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Fourth item of a user-supplied 22-issue backlog ("Fix the Registration Details error when switching between IP and OP records with unclear/incomplete data"). `PatientRegistrationForm.tsx`'s `registration.encounterType` `<Select>` had no reset side effect, unlike Department→Consultant (`handleDepartmentChange`) and State→District (`handleStateChange`) right next to it, which already do clear dependent fields on change. Switching OP→IP (or Emergency/DayCare/Observation) and back leaves stale `admissionType`/`category`/`referral.*` in form state — in particular, a partially-filled `referral` (e.g. `details`/`contactNumber` typed but `category` left unset) stays in state even after switching back to OP hides the Referral fields entirely, and `registrationDetailsUiSchema`'s `superRefine` still requires `referral.category` once `referral` is set at all — leaving the tab permanently flagged invalid with no visible field left on screen to fix it.

**Decision**
Added `handleEncounterTypeChange`, mirroring the existing `handleDepartmentChange`/`handleStateChange` pattern exactly: on any encounter-type change, clears `registration.admissionType` and `registration.category` back to `''`, and sets `registration.referral` to `undefined` (not an empty object — `referralColumnSchema` is `.optional()`, so `undefined` fully satisfies the `superRefine` check rather than leaving an empty-but-present object that would still fail it). Wired into the existing `<Select>` in place of the raw `field.onChange`. `RecordVisitForm.tsx` (the standalone "Add Visit" page) was checked and needs no equivalent change — its schema deliberately excludes `admissionType`/`referral`/`category` entirely (they're UI-only fields that only exist on the registration wizard's shape), so it was never exposed to this bug.

**Consequences**
- Live browser verification (the plan's stated intent for this item, since it's a stateful form-interaction bug) could not be completed: the only dev login path needs the seeded Super Admin password, which lives solely in `dotnet user-secrets` — reading that file was correctly blocked by the auto-mode safety classifier as credential access, and no lower-sensitivity path (e.g. provisioning a fresh throwaway tenant) exists without first clearing that same login gate. Verified instead via `tsc --noEmit` and `eslint` (both clean) and by tracing the exact validation-schema mechanism the fix addresses; the fix itself mirrors an already-proven, already-shipped pattern in the same file rather than introducing new logic.

---

### ADR-039: On-call/"Others" consultation charge no longer stomped back to 0
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Third item of a user-supplied 22-issue backlog ("the amount field has 0, it should be editable" for Doctor's Consultation - Others/On-call). `ConsultationTypeSelect` already shows "Amount to be filled" for consultation types with a null master `amount` (by design — see `Masters.ConsultationType`'s own doc comment), and the charge `<Input>` in `ConsultationBillingCard.tsx` was never actually `disabled`. The real bug was a `useEffect` (lines ~103-108) that unconditionally reset `charge` to `selectedType?.amount ?? 0` — for a null-amount type that's always 0, and it re-ran on *any* new `consultationTypes` react-query object reference (a refetch on window focus, a cache invalidation from an unrelated mutation, etc.), not just on an actual type change — so any amount staff typed in kept getting wiped back to 0.

**Decision**
The effect now only overwrites `charge` when the selected type's master `amount` is non-null, and its dependency array was narrowed to `consultationTypeId` alone (dropping `consultationTypes`) — React effects still close over the latest render's values, so this doesn't need a ref; it just stops the effect from re-running on a same-selection data refetch.

**Consequences**
- Scoped narrowly to the reported symptom (the null-amount case); did not add "has the user manually edited this field" tracking for fixed-fee types, which is a broader behavior change nobody asked for here.
- Verified via `tsc --noEmit` and `eslint` (both clean) — this repo has no frontend test runner/config anywhere, so there's no existing baseline to add an automated test to.

---

### ADR-038: Voided invoices excluded from the Income & Expense Report's revenue totals
**Date:** 2026-09-02
**Status:** Accepted

**Context**
Second item of a user-supplied 22-issue backlog ("Fix the issue where discarded billing details are still being added to Accounts & Finance"). "Discarding" a bill in this codebase means voiding it (`Invoice.IsVoided`) — there is no separate soft-delete-style cancellation status. The Unified Invoice Ledger intentionally still lists voided invoices (an audit trail, not a "vanish" — see ADR-037), but `frontend/web/src/features/reports/incomeExpenseReport.ts`'s `getIncomeRows`/`getIncomeByBillingType` — which drive the Income & Expense Report's Total Income and per-billing-type breakdown — summed every invoice's `netAmount` with no `isVoided` check at all, so a discarded bill's amount was still counted as real revenue.

**Decision**
`getIncomeRows` and `getIncomeByBillingType` now both skip any `billing.isVoided` invoice before aggregating. The Ledger itself is untouched (still shows voided rows with their existing "Voided" badge) — only the two revenue-aggregation functions changed, so voided invoices stay visible for audit purposes everywhere except in the actual income totals.

**Consequences**
- No frontend automated test was added: this repo has no frontend test runner/config anywhere (`frontend/web/tests` is an empty directory, no `*.test.ts(x)` file exists in the whole repo) — verified via `tsc --noEmit` (clean) and `eslint` (clean) on the touched file instead, consistent with there being no existing frontend-test baseline to extend.

---

### ADR-037: Billing — a voided invoice can no longer receive a payment; voided invoices excluded from the Pending filter
**Date:** 2026-09-02
**Status:** Accepted

**Context**
First item of a user-supplied 22-issue backlog ("Fix the Billing status issue between Pending and Paid"). `InvoiceService.VoidAsync`/`Invoice.Void()` already block voiding an invoice that has any Paid line item, but the reverse was never enforced: `RecordPaymentAsync` never checked `Invoice.IsVoided`, so a voided invoice could still receive `RecordPaymentAsync` calls against its (unpaid) line items — picking up a real `Payment` row and a Paid line item on an invoice the domain considers cancelled. Separately, `InvoiceRepository.GetPagedAsync`'s "Pending" filter (used by the Unified Invoice Ledger) matched purely on line-item status with no `IsVoided` exclusion, so a voided invoice with no paid items showed up mixed into "Pending" results even though the UI labels it "Voided".

**Decision**
1. `InvoiceService.RecordPaymentAsync` now rejects any payment attempt against a voided invoice with a new `BillingErrorCodes.InvoiceVoided` (409 Conflict, mapped in `InvoicesController.MapFailure` alongside the existing `AlreadyVoided`/`LineItemAlreadyPaid` codes) — checked before the existing "already paid" guard so the more specific reason surfaces first.
2. `InvoiceRepository.GetPagedAsync`'s Pending branch now also excludes `IsVoided` invoices. The "Paid" branch needed no change: with (1) in place a voided invoice can never acquire a Paid line item going forward, so it can never spuriously match "Paid" either. Voided invoices remain visible in the unfiltered Ledger (existing by-design behavior per `Domain/Invoice.cs`'s own comment — a voided invoice must stay queryable for audit purposes, not disappear like a soft-deleted row) — only the Pending/Paid status filter now excludes them.

**Consequences**
- Closes the one-directional gap: Void→blocked-by-Paid was already enforced, Paid→blocked-by-Voided now is too, for both the previously-reachable direct-API race and the semantically-nonsensical result of a "cancelled" invoice quietly earning revenue.
- No repository-level test was added (no repository/DB-level test harness exists anywhere in this codebase — every existing Billing test substitutes `IInvoiceRepository`); the query fix mirrors the existing predicate style in the same method and was verified by build + the full `dotnet test` suite (732 unit + 88 architecture tests, all passing).

---

### ADR-036: Hospital HR Management MVP — Employee/Staff/Consultant separation, module split, and Documents reuse
**Date:** 2026-08-27
**Status:** Accepted

**Context**
The Hospital HR Management MVP needed Employee, Attendance, LeaveType, and LeaveRequest — none of which existed anywhere in the codebase. Three separate id spaces already existed or were introduced by this work with overlapping-sounding names: `identity.users` (portal logins), `Masters.Consultant` (doctor names used only for patient visits — an entirely separate, unrelated id space per its own doc comments), and the new `Employee` entity (HR's actual staff record). A companion question was where the new Designation lookup and the Employee/Attendance/Leave entities should physically live, and whether Employee "documents" (ID proofs, certifications with expiry dates) needed their own storage.

**Decision**
1. **Employee is new and distinct from both `identity.users` and `Masters.Consultant`**, optionally linkable to `identity.users` via a nullable `UserId` (checked cross-module via Identity's public `IUserService.GetByIdAsync` only when supplied, never required) for employees who also have a portal login. `Masters.Consultant` is left untouched — this MVP does not conflate "employee" with "consultant" even though a consultant is very likely to also be an employee; reconciling the two is explicitly out of scope.
2. **Designation lives in `HMS.Modules.Masters`** as a near-exact clone of `Department` (same Code/Name/IsActive shape, same soft-delete-aware unique-code index, same `identity-administration.*` permission), so the existing frontend Masters config-driven CRUD engine picks it up with zero new page code. **Employee, Attendance, LeaveType, and LeaveRequest live in the existing `HMS.Modules.HR`** (which already owned Shift/ShiftAssignment/WeeklyRoster/StaffAvailability/ShiftSwapRequest for staff scheduling) rather than a new module — Employee→Department and Employee→Designation are cross-module Guid references with no database FK (existence-checked via Masters' public `IDepartmentService`/`IDesignationService`, mirroring the established `ShiftAssignment.DepartmentId` convention), while Employee→ReportingManager (self-reference) and Attendance/LeaveRequest→Employee/LeaveType (all same-schema, within `hr`) get real database FKs with `Restrict` delete behavior.
3. **Employee Documents reuse the existing generic Documents module** instead of bespoke storage: a nullable `ExpiryDate` (`DateOnly?`) column was added to `Documents.Domain.Document` (backward-compatible — every existing owner type just leaves it null) plus a matching optional field on `UploadDocumentRequest` so a Staff document (e.g. an ID proof or certification) can actually carry an expiry date once uploaded; a new `StaffDocumentOwnerExistenceChecker` (`HMS.Modules.HR.Infrastructure`) implements `IDocumentOwnerExistenceChecker` for the previously-unregistered `DocumentOwnerType.Staff`, checking existence against `hr.employees`, mirroring `PatientDocumentOwnerExistenceChecker`. `IDocumentService` gained one new method, `GetExpiringDocumentCountAsync(DocumentOwnerType, int withinDays, ct)`, consumed cross-module by the new HR dashboard. The existing generic `DocumentsController` (`ownerType`/`ownerId` query params) already supports `ownerType=Staff` end to end once the checker exists — confirmed by reading `DocumentsController`/`DocumentService` closely; no duplicate employee-document endpoint was built. This required a new one-directional `HMS.Modules.HR` → `HMS.Modules.Documents` project reference (mirrors `HMS.Modules.Patients` → `HMS.Modules.Documents`).
4. **`Employee.IsActive` (the generic Activate/Deactivate toggle) is deliberately orthogonal to `Employee.EmploymentStatus`** (a richer HR-domain status: Active/OnLeave/Terminated/Resigned) — an employee can be `IsActive=true` and `EmploymentStatus=OnLeave` simultaneously. Dedicated `POST /activate`/`POST /deactivate` endpoints exist for the former; the latter changes only via the general `PUT` update, matching the spec's framing of the two as separate concerns.
5. **`EmployeeResponse` enrichment (DepartmentName/DesignationName/ReportingManagerName) is computed only for the single-record `GET /{id}` "profile" read**, not for paged list results — the paged list leaves those three fields null. Resolving them for every row of a page would mean N extra cross-module service calls per page; the frontend already needs Department/Designation reference data for its own list filters, so resolving display names client-side for the list view costs nothing extra. Attendance/LeaveRequest, by contrast, enrich every row with EmployeeName/EmployeeCode/LeaveTypeName unconditionally — those are cheap same-schema SQL joins (Attendance, LeaveRequest, Employee, and LeaveType all live in the `hr` schema), not cross-module calls.
6. **No new feature-catalog key, permission-catalog category, or `LeaveBalance` table.** The existing `"hr"` `FeatureCatalog.SchemaBacked` key and the existing `workforce-admin.*` permission (already used by `ShiftsController` et al.) cover every new Employee/Attendance/LeaveType/LeaveRequest controller; Designation reuses `identity-administration.*` exactly like every other Masters controller. Leave balances (`GET /api/v1/employees/{id}/leave-balances`) are computed on every read (sum of `Approved` `LeaveRequest.TotalDays` per employee+leave-type whose `StartDate` falls in the server's current UTC calendar year) rather than maintained in a separate table — no fiscal-year configuration exists, deliberately kept simple for the MVP.

**Consequences**
- A future "reconcile Consultant and Employee" effort (e.g. so a doctor's consultant record and employee record aren't maintained independently) is a real, documented gap, not silently ignored — same pattern as prior ADRs' explicit deferrals.
- `HRDbContextFactory` (`backend/src/Database/HMS.Database.Migrations/HR/`) was added — it was missing even though `HR/Migrations` already had prior migrations, so no design-time EF Core command against `HRDbContext` worked before this.
- No standard sample-data seeding mechanism exists in this codebase (only bootstrap seeders for the super-admin/platform-admin accounts, `IdentityDataSeeder`/`PlatformDataSeeder` — not business/reference data); seeding realistic Departments/Designations/Employees/Attendance/LeaveRequests was therefore skipped for this MVP rather than inventing a new seeding pipeline, per the task's own fallback instruction.

---

### ADR-035: Messaging & Notification module Phase 8 — provisioning, staff directory, and the frontend
**Date:** 2026-08-27
**Status:** Accepted

**Context**
Continuation of ADR-034 — the final phase of the design doc: promote the module out of `FeatureCatalog.UiOnly`, wire it into real tenant provisioning, and build the frontend (notification bell, notifications page, preferences page, messaging UI), replacing the `PlaceholderPage` at `/engagement/messages`. The design doc's Phase 8 also called for "add the actual NotifyAsync calls at each real trigger point in Appointments/Patients/Billing/Pharmacy/IPD" — investigated first, before writing any of that wiring.

**Decision**
1. **No automatic cross-module notification triggers were wired — investigated and confirmed there is nothing real to wire yet.** Checked `PatientService.CreateAsync`, `InvoiceService.CreateAsync`/`RecordPaymentAsync`, and HR's `ShiftAssignment` for a genuine `identity.users` id to notify: every one of them only has `actorId` (the caller themselves) in scope. `Masters.Consultant` and HR's `ShiftAssignment.StaffId` are both explicitly documented as separate, unrelated id spaces with no link to `identity.users` — "notify the consultant" or "notify the assigned staff member" is not representable today without adding that linkage first. There is also no "get users by role" capability anywhere (`IUserService`/`IRoleService` were checked in full) to support a generic "notify front-desk" broadcast. Rather than wire a self-notification (noisy, not a real feature) or invent a fake recipient, this is left undone and documented as a real, confirmed gap — the same "do the core piece fully, defer the adjacent architecture change explicitly" pattern as ADR-027/ADR-028's Pharmacy billing deferral. The `Appointments` module referenced in the design doc's examples doesn't exist as real code at all (a placeholder project, per `FeatureCatalog`'s own comment), so "appointment booked/reminder/cancelled" triggers have no home yet either. What *is* fully wired end-to-end today: the admin manual-send endpoint (`POST /api/v1/notifications`, `engagement.create`) and Messaging's new-message hook (ADR-034) — both live-verified.
2. **`messages-and-notifications` promoted from `FeatureCatalog.UiOnly` to `.SchemaBacked`**, gating both `NotificationsDbContext` and `MessagingDbContext` behind the one toggle (`TenantMigrationService` migrates both when the feature is enabled) — mirrors how `Mandatory` already groups multiple schemas under one umbrella.
3. **A new `IUserService.GetStaffDirectoryAsync`/`GET /api/v1/users/directory` endpoint** — discovered live, mid-phase, that the messaging UI's most basic requirement ("pick a colleague to message") had no accessible backend: the only existing user-listing endpoint (`GET /api/v1/users`) requires `identity-administration.view`, an admin-only permission, which would make "start a conversation" unusable for regular staff. Added a deliberately minimal, low-sensitivity `StaffDirectoryEntryResponse` (`Id`/`FirstName`/`LastName`/`RoleName` only — no email/phone/login metadata) behind `[Authorize]` alone, capped at 100 active users via the existing `PagedRequest.MaxPageSize`. This is completing what Phase 7's messaging feature fundamentally requires, not scope creep.
4. **Live-verified end-to-end against the real backend**, not just `dotnet test` — this phase is entirely UI-facing. Registered a fresh throwaway tenant (`msgtest`) via the Platform Portal (own credentials, no reused secrets — mirrors the module-rollout precedent for Pharmacy), enabled the feature, and triggered `POST /api/platform/hospitals/{id}/migrate` (an existing operator action, `PlatformDashboardService.MigrateAsync`) since toggling a feature "on" via Manage Features does not itself run migrations — confirmed live (the first attempt, against the pre-existing "Dev Hospital" tenant, failed with `relation "notifications.notification_recipients" does not exist` until `/migrate` was called explicitly). Created a second staff user and, through the actual browser UI: started a 1:1 conversation, sent a message, confirmed it rendered instantly, confirmed the new-message notification fired (`NotificationService` logged "Created notification ... for 1 recipient(s), 1 queued Email/Sms deliveries"), and confirmed the background delivery pipeline resolved the recipient's email via Identity and the (unconfigured, per ADR-033) `SmtpEmailSender` logged a graceful warning rather than crashing. Also verified the Preferences tab's toggle-and-upsert round-trip live. No console errors, no failed requests beyond one unrelated pre-login 401.
5. **The `Manage Features` toggle not auto-migrating on enable is a pre-existing gap, not introduced here** — left unfixed (out of scope for this module) but now confirmed and worth a future fix: today an operator must remember to also call `/migrate` after enabling any optional feature for an existing tenant, for every module, not just this one.

**Consequences**
- The frontend gained: `frontend/shared`'s Notifications/Messaging DTOs, enums, and four new API-client services (`NotificationsApi`, `NotificationPreferencesApi`, `NotificationTemplatesApi`, `ConversationsApi`); `frontend/web`'s `features/notifications` and `features/messaging` (hooks + components), `MessagesAndNotificationsPage` (Messages/Notifications/Preferences tabs), and a live-wired `NotificationsMenu` header bell (the old `mockNotifications.ts` was deleted, not left dead).
- No websocket/real-time layer exists anywhere in this codebase — the bell, notification list, and conversation list all poll via React Query `refetchInterval` (30s/15s/8s depending on how actively the user is likely watching), the only established pattern here.
- `NotificationTemplatesApi`/template-editor UI was built in `frontend/shared` but has no page wired up yet (no nav entry calls for a dedicated admin template-editor screen) — the backend CRUD (Phase 3) is complete and reachable directly via the API; a UI for it is a reasonable, small follow-up whenever an admin actually needs to author a template instead of relying on a notification's caller-supplied literal `Body`.

---

### ADR-034: Messaging & Notification module Phase 7 — internal messaging (conversations, participants, messages)
**Date:** 2026-08-27
**Status:** Accepted

**Context**
`HMS.Modules.Messaging`'s Domain/Infrastructure existed since Phase 1 (ADR-029) but had no Application/Endpoints layer. Phase 7 built the full messaging feature on top of it: start a conversation, list mine, read/send messages, mark read, plus the new-message notification hook.

**Decision**
1. **`ConversationParticipant` membership is the sole authorization check** for every per-conversation action (`GetMessagesAsync`, `SendMessageAsync`, `MarkReadAsync`) — a single `GetByConversationAndUserAsync` lookup returning null covers both "not a participant" *and* "conversation doesn't exist at all," collapsing both into one 403 (`ConversationErrorCodes.NotParticipant`). This is a stricter privacy property than Notifications' equivalent choice (ADR-030's `NOT_FOUND`): a caller can't even tell whether a given conversation id is valid, not just whether it's theirs.
2. **A OneToOne "create conversation" call is idempotent** — `FindOneToOneConversationIdAsync` checks for an existing conversation between the same two users first and returns it instead of creating a duplicate thread. Not explicitly required by the design doc, but treated as basic correctness (clicking "message this person" from two different screens must not fork the thread), not scope creep.
3. **`ConversationResponse` carries only participant `UserId`s, never names** — resolving display names/avatars is left to the frontend (a batched call to Identity's own Users API), keeping this module's one cross-module dependency (Notifications) from growing into two just to decorate a response DTO.
4. **The new-message hook fires unconditionally for every other participant**, not only ones who are "away" — no presence/"currently active in this conversation" tracking exists (explicitly out of scope per the design doc), so every message raises one `INotificationService.NotifyAsync` call with a 200-character preview as the body. `NotifyAsync`'s own preference check (ADR-032) still governs whether Email/Sms also fire; this hook only ever asks for in-app.
5. **`HMS.Modules.Messaging` now legitimately depends on `HMS.Modules.Notifications.Application`** (its public `INotificationService`) — same pattern as ADR-032's Notifications→Identity dependency. `NotificationsCrossModuleDependencyTests`' blanket ban now excludes Messaging, covered instead by a new `MessagingCrossModuleDependencyTests` (mirrors the established Application-allowed-but-not-Domain/Infrastructure shape). Messaging has no legitimate reason to touch Identity directly (it only ever carries opaque `UserId` values), so that ban stays a full blanket ban for Messaging.
6. **Conversation listing (`GetMyConversationsAsync`) accepts N+1 queries** for per-conversation participants and unread counts — a user's conversation count is realistically dozens, not thousands, so the win from batching wasn't judged worth the added complexity at this scale (mirrors trade-offs already accepted elsewhere in this codebase, e.g. Pharmacy's product/batch/patient lookups on the Dispense list).

**Consequences**
- 13 new `ConversationServiceTests` cover participant-gating, the OneToOne dedup, group-size validation, and the notification hook's recipient exclusion.
- No live browser verification — this phase has no frontend yet (a later phase builds the messaging UI).

---

### ADR-033: Messaging & Notification module Phase 5+6 — real Email (SMTP) and SMS (generic HTTP gateway) senders
**Date:** 2026-08-27
**Status:** Accepted

**Context**
Continuation of ADR-032. The design doc scoped these as two separate phases, each needing "an infra decision for the user to make when this phase starts" (an SMTP account, an SMS gateway vendor) — done together here since neither decision was actually available mid-session, so both senders were built to the same "real but config-driven, gracefully degrades when unconfigured" shape instead of waiting.

**Decision**
1. **`SmtpEmailSender` replaces `LoggingEmailSender` outright** (the Phase 4 stub is deleted, not left dead/unregistered) — uses the .NET runtime's built-in `SmtpClient` rather than adding a NuGet dependency (e.g. MailKit) for one send operation. Reads `Notifications:Smtp:*` directly from `IConfiguration` in the constructor, mirroring `JwtTokenGenerator`'s identical pattern (no `IOptions<T>` wrapper).
2. **`HttpSmsSender` replaces `LoggingSmsSender` outright**, same reasoning. No vendor SDK (Twilio, MSG91, etc.) is referenced — posts a generic `{ to, from, message }` JSON body with a Bearer `Notifications:Sms:ApiKey` to a configured `Notifications:Sms:BaseUrl`. Picking a specific gateway is left to whoever configures a real tenant's deployment; a gateway with a genuinely different contract gets its own `ISmsSender` implementation, not a change to this one.
3. **Both senders no-op with a logged warning when unconfigured, rather than throwing** — deliberately different from `JwtTokenGenerator`'s missing-config-throws-at-startup behavior, since JWT signing is mandatory for the app to function at all while Email/Sms are best-effort channels by design (see ADR-029). A hospital that hasn't set up SMTP/SMS yet should not have every notification delivery attempt throw.
4. **`appsettings.json` gained a `Notifications:Smtp`/`Notifications:Sms` section** with empty placeholder values, matching every other credential-shaped config block in this file (`Jwt:SigningKey`, `SuperAdminSeed:Password`, etc.) — real values are supplied per-environment, never committed.

**Consequences**
- `ISmsSender`/`IEmailSender`'s interfaces are unchanged from Phase 4 — nothing upstream (`NotificationDeliveryBackgroundService`) needed to change, confirming the swap-in design worked as intended.
- 4 new unit tests cover only the "unconfigured → no-op, doesn't throw" path for each sender — the actual network call (real SMTP handshake, real HTTP POST) isn't unit-testable without a live endpoint, consistent with this codebase's existing line between unit and integration test scope.
- Real end-to-end delivery (an actual email/SMS landing somewhere) is unverified — there is no SMTP account or SMS gateway configured in this environment. This is expected until a real deployment supplies both.

---

### ADR-032: Messaging & Notification module Phase 4 — background Email/Sms delivery pipeline
**Date:** 2026-08-27
**Status:** Accepted

**Context**
Continuation of ADR-031. Phase 4's goal was the async delivery architecture itself (queue, worker, status tracking, stub senders) — proven correct before any real network dependency (Phase 5/6) is added.

**Decision**
1. **`NotificationDeliveryQueue`/`NotificationDeliveryBackgroundService` are a direct copy of `HMS.Modules.Documents`' scan pipeline shape** — a bounded (500) `Channel<T>`, one singleton queue, one hosted-service reader draining it with a fresh DI scope (and `ITenantContext.SetTenant`) per item, a top-level per-item try/catch so one failure never kills the reader loop. This is the second use of this exact mechanism in the codebase (see ADR-029's original reasoning for why a Hangfire/Redis-based queue wasn't introduced instead).
2. **`NotifyAsync` now wires `NotificationPreferences` into delivery** (deferred from ADR-031 until there was an actual delivery pipeline to gate): for each recipient, `ResolveChannelsAsync` checks their preference row for the notification's `Category` — a missing row defaults to email-on/sms-off (matching `NotificationPreference.Create`'s own defaults, so "never saved a preference" and "saved the default explicitly" behave identically) — except `NotificationSeverity.Emergency`, which bypasses preferences entirely and always queues both channels.
3. **Notifications depends on Identity's public `IUserService`** to resolve a recipient's actual email/phone number for delivery — the delivery pipeline has no other way to reach `identity.users`. This is the same cross-module pattern Pharmacy/Billing/IPD already use (`docs/DeveloperHandbook.md`'s "depend only on another module's Contracts/public Application seam" rule), extended for the first time to a module depending on **Identity** specifically. `HMS.ArchitectureTests.Modules.Identity.CrossModuleDependencyTests`' blanket ban (no module may reference `HMS.Modules.Identity.Application`) now excludes Notifications, covered instead by a new `NotificationsCrossModuleDependencyTests` (mirrors `ProductsCrossModuleDependencyTests`' Application-allowed-but-not-Domain/Infrastructure shape).
4. **Deliveries are enqueued only after the triggering `SaveChangesAsync` commits**, never before — the background reader re-fetches each `NotificationDelivery` row by id from the database, so a queued item pointing at a not-yet-committed row would silently vanish (`GetByIdAsync` returns null, logged as a warning, delivery stays `Pending` forever). Mirrors `DocumentService.UploadAsync`'s identical save-then-enqueue ordering.
5. **Both stub senders (`LoggingEmailSender`, `LoggingSmsSender`) log instead of sending** — mirrors `NullVirusScanner`'s exact reasoning: proves the pipeline's plumbing (queueing, status transitions, the `NotificationDelivery` state machine) is real and testable before any real network dependency's failure modes are in the mix. Superseded by real senders in ADR-033, built later the same session.

**Consequences**
- `NotificationDeliveryBackgroundService`, `NotificationDeliveryQueue`, and the stub senders have no dedicated unit tests — mirrors the Documents scan pipeline's identical (lack of) direct test coverage in this codebase; the pipeline is exercised through `NotificationServiceTests`' delivery-queueing assertions instead.
- A queued delivery is lost if the process restarts before it's drained (in-memory queue) — the row stays `Pending` forever rather than being retried. Accepted at MVP scale, same trade-off `IDocumentScanQueue` already made.

---

### ADR-031: Messaging & Notification module Phase 3 — templates, preferences, and template-driven rendering
**Date:** 2026-08-27
**Status:** Accepted

**Context**
Continuation of ADR-030. Phase 3's goal was to let every event in the design doc's event table be authored (subject/body text) without touching code, by wiring `NotificationTemplate`/`NotificationPreference` (built in Phase 1, unused since) into real CRUD and into `NotifyAsync` itself.

**Decision**
1. **`NotifyRequest.Body` became optional.** When omitted, `NotificationService.NotifyAsync` looks up the InApp-channel `NotificationTemplate` for `TemplateKey` and renders its `BodyTemplate` against `Placeholders` (new `TemplateRenderer` — flat `{{Key}}` substitution, unmatched tokens left literal rather than blanked). `Title` stays always-literal — short enough that a second templated field wasn't worth it. This is additive to Phase 2's contract, not a breaking change (`Body` was required before; nothing yet calls this method in anger).
2. **`NotificationTemplateService`/`NotificationTemplatesController`**: CRUD gated by the existing `engagement.*` permissions (view/create/edit) — content and active-state are edited together in one `PUT`, no separate activate/deactivate route.
3. **An expected validation failure that depends on already-loaded entity state stays a `Result`, never an exception.** `NotificationTemplate.UpdateContent`'s own guard (Email channel requires a Subject) would otherwise throw `ArgumentException` and surface as a 500 — `NotificationTemplateService.UpdateAsync` checks this itself (it already has the loaded template's `Channel` in hand) before calling `UpdateContent`, so the domain guard never actually fires on this path; it stays as defense-in-depth.
4. **`NotificationPreferenceService`/`NotificationPreferencesController`**: self-service only (`[Authorize]`, no `RequirePermission`), `PUT` upserts one category at a time. `GetMyPreferencesAsync` returns only rows the caller has actually saved — a missing category means "use the default," not "explicitly disabled" (see `NotificationPreference`'s Phase 1 doc comment); no attempt is made to enumerate/seed every possible category up front, since the category list itself isn't a fixed catalog anywhere in this codebase.

**Consequences**
- `NotificationsModuleBoundaryTests`' allow-list grew to include `INotificationTemplateService` and `INotificationPreferenceService`.
- Preference checks are still not wired into delivery — that's meaningless until Phase 4/5/6's Email/Sms pipeline exists to check them against. In-app delivery (Phase 2) intentionally ignores preferences entirely, matching the design doc's "in-app is always delivered" decision.
- 26 new unit tests (`NotificationTemplateServiceTests`, `NotificationPreferenceServiceTests`, plus 2 new `NotificationServiceTests` covering the template-rendering fallback).

---

### ADR-030: Messaging & Notification module Phase 2 — in-app notifications end-to-end, no templates yet
**Date:** 2026-08-27
**Status:** Accepted

**Context**
Continuation of ADR-029. Phase 2's goal was to prove the fan-out + read/unread mechanics end-to-end (service + API), deliberately before template rendering (Phase 3) or async Email/Sms delivery (Phase 4) exist.

**Decision**
1. **`NotifyRequest` carries already-rendered `Title`/`Body`**, not just a `TemplateKey` to look up — `NotificationTemplate` exists (Phase 1) but isn't wired to rendering yet, so the caller (a later phase's Appointments/Patients/etc., or today's admin manual-send endpoint) supplies final text directly. `TemplateKey` is still recorded, informationally, for when Phase 3 adds rendering in front of the same method.
2. **`INotificationService` is the module's public seam** (mirrors `IUserService`'s CS0051 reasoning) and is also, deliberately, the same method every other module will call in-process in a later phase — no event bus, per ADR-029.
3. **`NotifyAsync` only ever writes the in-app channel** (`Notification` + `NotificationRecipient` rows) in this phase — no `NotificationDelivery` rows yet, since Email/Sms don't exist until Phase 4/5/6.
4. **"My notifications" endpoints need no `RequirePermission`** beyond `[Authorize]` — every read/write is scoped to the caller's own `UserId` from the JWT, so there's no parameter surface to gate. Only the admin manual-send `POST /api/v1/notifications` requires `engagement.create`.
5. **`MarkAsReadAsync` returns the same `NOT_FOUND` code for "doesn't exist" and "belongs to someone else"** — deliberately not distinguished, so the endpoint can't be used to probe whether a given id belongs to another user (mirrors `AuthenticationService`'s generic login-failure message).
6. **`NotificationResponse` is a recipient's view, not the `Notification` itself** — its `Id` is the `NotificationRecipient.Id` (what "mark as read" targets), with a separate `NotificationId` field. A `Notify` call's response is a distinct `NotificationBroadcastResponse` (`NotificationId` + `RecipientCount`), since one call fans out to N recipient rows with no single "the" response.

**Consequences**
- `NotificationsModuleBoundaryTests`' allow-list grew to include `INotificationService` alongside `NotificationsDbContext`.
- No live browser verification — this phase has no frontend yet; `dotnet test` (10 new `NotificationServiceTests`) is the verification, consistent with [[feedback_no_live_verification_per_fix]]'s reasoning for backend-only phases.

---

### ADR-029: Messaging & Notification module — two modules, in-process seam, Phase 1 is Domain + Infrastructure only
**Date:** 2026-08-27
**Status:** Accepted

**Context**
The user asked for a full design (no code) for a Messaging & Notification module covering in-app/email/SMS notifications and internal staff messaging, then approved starting implementation. The `messages-and-notifications` feature key and `engagement` permission category already existed (`FeatureCatalog.UiOnly`, shared with Calendar), and `HMS.Modules.Notifications` already existed as an empty scaffold (`docs/DeveloperHandbook.md`'s reserved-module pattern) — this design filled that slot rather than inventing a new one. The full design (architecture diagram, flows, database, APIs, security, phased plan) was published as a standalone artifact for review before any code was written; this ADR only records the decisions embodied in Phase 1.

**Decision**
1. **Two modules, not one**: `HMS.Modules.Notifications` (schema `notifications` — templates, preferences, notification fan-out, delivery tracking) and a new `HMS.Modules.Messaging` (schema `messaging` — conversations, participants, messages). Different aggregates, different lifecycles, different failure modes (an email backlog shouldn't affect chat) — kept as two single-purpose schemas per the one-schema-per-module rule, sharing one feature flag and permission category because the product experience is one page.
2. **No new infrastructure.** Cross-module notification triggers will go through a plain public `INotificationService` seam (added in a later phase), called in-process — the same pattern Pharmacy already uses for `IInvoiceService`. Async Email/SMS delivery (also a later phase) will reuse `HMS.Modules.Documents`' existing `Channel<T>` + `BackgroundService` pattern (`DocumentScanQueue`/`DocumentScanBackgroundService`) rather than introducing Hangfire/Redis/Kafka.
3. **Phase 1 scope is deliberately narrow**: Domain entities (5 for Notifications, 3 for Messaging), their EF Core configurations, repositories, DbContexts, and the two initial migrations. No `Application` services, validators, or `Endpoints` controllers yet — those are later phases, so each `AddXModule` currently registers only a `DbContext` and its repositories.
4. **No per-message read-receipt table.** `ConversationParticipant.LastReadAt` (a single timestamp) answers "what's unread" for one-to-one and small-group conversations without the write/join cost of a receipt-per-message table — cut per the brief's explicit "don't add advanced chat features unless required."
5. **No DB-level FK to `identity.users`** for any `UserId`/`SenderId` column — cross-schema FK constraints are a deliberate, reviewed exception per `docs/DatabaseArchitecture.md` §7, not a default; mirrors Pharmacy's existing treatment of `PatientId`/`ProductId` (plain indexed column, no `HasOne<>()`). Intra-module FKs (e.g. `NotificationRecipient` → `Notification`, `Message` → `Conversation`) do use real constraints, all `DeleteBehavior.Restrict`.
6. **`messages-and-notifications` stays in `FeatureCatalog.UiOnly` for now** — promoting it to `SchemaBacked` (which wires it into `TenantMigrationService` so new/existing tenants actually get the `notifications`/`messaging` schemas provisioned) is deferred to the phase where the module is functionally complete end-to-end, mirroring how Pharmacy's own `FeatureCatalog` promotion happened only once its workflow was real. Until then, the migrations exist and are verified via `dotnet ef database update` against a local dev database, but are not part of the automatic tenant-provisioning path.
7. Both modules' architecture boundary tests (`NotificationsModuleBoundaryTests`, `MessagingModuleBoundaryTests`) currently allow only the `DbContext` type as public — the same allow-list pattern grows to include a public service interface (e.g. `INotificationService`) once one exists, the same way `IEventService` was added to Calendar's.

**Consequences**
- Phase 1 has no user-facing effect at all (no endpoints, no tenant provisioning change) — it's the load-bearing scaffold every later phase builds on, verified by 8 new domain-entity test classes and 2 new architecture-boundary test classes, all green, plus a clean `dotnet build` of the full solution.
- `HMS.Modules.Notifications`' `.csproj` gained the same EF Core/Npgsql/FluentValidation/`FrameworkReference` shape Identity/Pharmacy already have, even though `FluentValidation` and `FrameworkReference` aren't exercised until the `Application`/`Endpoints` phases — added now so the scaffold doesn't need a second edit later.
- `CrossModuleDependencyTests` gained a `HMS.Modules.Messaging` entry alongside the existing `HMS.Modules.Notifications` one.

---

### ADR-028: Pharmacy dispense billing is best-effort, generated server-side, not atomic with the dispense
**Date:** 2026-08-20
**Status:** Accepted

**Context**
ADR-027 explicitly deferred billing integration for Pharmacy. During a full regression pass (patient registration → dispense → billing), the user asked to build it now rather than continue treating it as out of scope. Billing (`HMS.Modules.Billing`) turned out to already be a full CRUD module with a public `IInvoiceService.CreateAsync`, not merely something nested inside patient registration — so this was a real but contained addition, not a prerequisite rebuild of Billing itself.

**Decision**
1. **`BillingType` gains a `Pharmacy` value.** Stored via the existing `HasConversion<string>()` mapping, so this is a purely additive change — no migration needed on Billing's own schema.
2. **Billing is best-effort, not part of the dispense's atomic commit.** `DispenseService.CreateAsync` calls `IInvoiceService.CreateAsync` only *after* the stock decrement + ledger row have already committed. Medicine has physically left the pharmacy and stock is already correctly decremented by that point — that fact must never be rolled back because a separate module's write failed or Billing was unreachable. A `Result` failure or a genuine exception from the billing call is caught and surfaced as `BillingFailed`/`BillingError` on the response; the dispense itself always still succeeds. Staff can post the charge manually via the existing OPD Billing Entry screen if automatic billing failed. This mirrors the project's established preference for doing the core piece fully and handling the adjacent failure mode explicitly rather than either skipping it or over-building a cross-schema distributed transaction this codebase has no precedent for anywhere.
3. **One invoice per dispense, one line item, `Quantity` fixed at 1.** `CreateInvoiceLineItemRequest.Quantity` is `int` — every other billing category bills whole units — but a dispense's real quantity is `decimal` (e.g. 150.5ml of a syrup). Rather than lose precision rounding the quantity, the full dispensed amount is priced into `UnitPrice` as that one line's total (`Quantity × Product.SellingPrice`); `ServiceId` carries a human-readable description (`"{ProductName} (Batch {BatchNo}) × {Quantity}"`) since Pharmacy has no Masters-backed service catalog to reference.
4. **`VisitId` falls back to `PatientId`** when the patient has no `CurrentRegistration` (mirrors the pattern `CreateInvoiceRequest`'s own doc comment already documents for OPD Billing Entry).
5. **`PharmacyStockTransaction` gains one narrow, deliberate exception to its otherwise-immutable-after-create design**: `SetInvoiceId(Guid, Guid?)`, guarded to Dispense-type rows and settable only once. Every stock/financial fact about the dispense itself stays immutable; which invoice ended up covering it is discoverable only *after* the fact, once billing has actually succeeded, so it's a distinct category of "write" from re-litigating what happened.

**Consequences**
- A dispense whose billing failed shows "Not billed" in the Dispenses list (persisted signal: `InvoiceId is null`) with no automatic retry — this is a known, accepted gap; nothing currently re-attempts billing for a previously-failed dispense.
- `InvoiceNumber` is only returned on the immediate `CreateAsync` response, not on later `GetById`/`GetPaged` reads — avoids an extra Billing round-trip per row on every list read (the existing product/batch/patient N+1 trade-off already documented in ADR-027 applies the same reasoning); `InvoiceId` alone is enough for the frontend to link to `/finance/accounts/{id}`.
- `features/billing/types.ts`'s own `BILLING_TYPES` (which drives which manual-entry cards the registration/OPD Billing Entry wizard renders) deliberately still excludes `Pharmacy` — there is no wizard card for it, since it's generated server-side only. The shared, backend-mirroring `BillingType` enum (`frontend/shared/enums/billing.ts`) does include it, since that one types real API responses.

**Also found and fixed during this ADR's live regression test**: `FeatureCatalog.SchemaBacked` never included `"billing"` at all, and `TenantMigrationService.MigrateAsync` had no `billing` branch — so the `billing` schema had never been migrated for *any* tenant, new or existing, despite Patient Registration's own Billing step and the OPD Billing Entry screen depending on it unconditionally. Every dispense's billing attempt against the live `pharmtest` tenant failed with `3F000: schema "billing" does not exist` until this was fixed. `"billing"` is now in both `FeatureCatalog.SchemaBacked` and `.Mandatory` (never toggleable — nothing treats it as optional), and `TenantMigrationService` migrates it unconditionally like identity/masters/patients/documents/branding. The migration was also applied directly (via `dotnet ef database update`) to all pre-existing tenant databases so this doesn't require every hospital to be re-provisioned. This was a real, pre-existing gap unrelated to Pharmacy specifically — it was only surfaced because Pharmacy billing was the first thing to ever make a real cross-module call into `IInvoiceService.CreateAsync` and actually exercise this path.

---

### ADR-027: Pharmacy ships as a minimal direct-dispense module — running-balance ledger, no prescriptions, no billing integration yet
**Date:** 2026-08-20
**Status:** Accepted

**Context**
Of the five clinical sidebar modules (IPD, OT, Pharmacy, Central Laboratory, Radiology), only IPD had a real backend/frontend — the rest were `PlaceholderPage` stubs. Pharmacy was picked as the next module to build because the drug/batch catalog already exists (the Products module) and its permission key (`pharmacy.*`) was already seeded and tenant-gated (ADR-022, ADR-026), making it the smallest lift of the four unbuilt modules. Investigation found `ProductBatch` has no quantity field and no stock ledger exists anywhere in the system — a real (non-demo) Pharmacy dispense workflow therefore had to introduce stock tracking from scratch, not just a prescription/dispense UI. Three scope questions were put to the user before implementation and answered as follows.

**Decision**
1. **Stock tracking**: a simple two-entity ledger — `PharmacyStockBalance` (current running balance per `(ProductId, ProductBatchId)`, `xmin`-guarded) and `PharmacyStockTransaction` (append-only Receipt/Dispense history with a `BalanceAfter` snapshot taken at commit time). No goods-receipt/purchase-order/supplier workflow — stock enters the system via a manual Stock Receipt (quantity-in only). This mirrors IPD's own current-state-plus-history-log shape (`Bed`/`BedTransferHistory`).
2. **Workflow**: direct dispense only. A pharmacist records Patient + Product + Batch + Quantity in one action and it's dispensed immediately — no separate Prescription entity, no doctor-writes-first approval step. `AdmissionId` is optional/nullable on a dispense so both OPD walk-ins and IPD patients work through the same action.
3. **Concurrency**: two dispenses racing against the same batch are handled with the existing `xmin` optimistic-concurrency column (already used by every entity in this codebase) plus a new, narrow addition — a bounded (3-attempt) retry loop in `DispenseService` that, on a `DbUpdateConcurrencyException`, re-fetches the balance and re-validates the requested quantity before retrying, so the loser of a race either succeeds against the winner's updated total or correctly fails `InsufficientStock` — never a silent lost update. No raw-SQL row locking was introduced; this stays inside patterns the codebase already uses.
4. **Atomicity**: the balance decrement and its ledger row are added to the same `PharmacyDbContext` and committed via a single `SaveChangesAsync` inside the retry loop, so a dispense can never partially commit (a decremented balance with no matching history row, or vice versa). An earlier draft of `DispenseService` called `SaveChangesAsync` twice (balance, then ledger separately) — caught and fixed before merge, since a failure between the two calls would have silently dropped stock without a matching audit record.
5. **Permissions**: no new permission catalog entries. Dispense and Stock Receipt both reuse the already-seeded `pharmacy.create`/`pharmacy.view` keys (ADR-022's own POST→create mapping) — ledger rows are immutable, so `pharmacy.edit`/`pharmacy.delete` stay defined-but-unused, same as several other modules' unused `delete` action today.
6. **Billing**: explicitly deferred to a follow-up PR. This module does not touch `BillingType` or `InvoiceLineItem` — a dispensed item does not yet generate an invoice line. Same deferred-seam pattern already used elsewhere in this backlog (do the core piece fully, defer the adjacent architecture change explicitly rather than half-building it).
7. `FeatureCatalog.SchemaBacked` (introduced in the "Tenant Feature/Module Management" work) gained `"pharmacy"`, moved out of `UiOnly`, so per-tenant enable/disable now actually provisions/migrates the Pharmacy schema instead of only toggling sidebar visibility.

**Consequences**
- A future Billing-integration PR must add the Dispense → Invoice posting step; until then, dispensed drugs are recorded but not billed automatically.
- A future Prescription module, if ever built, would sit in front of Dispense as an optional originating document rather than replacing it — Dispense's contract (Patient + Product + Batch + Quantity) doesn't need to change.
- The `pharmacy` schema (reserved in `docs/DatabaseArchitecture.md` §2 as "post-MVP") is now provisioned.
- No `docs/modules/Pharmacy/Pharmacy.md` was written — consistent with the two most recently built full modules (IPD, Products), which also don't have one; `docs/modules/*` is only kept current for Documents/HR/Identity/Patients.
- **Known limitation, pre-existing, not introduced by this module**: the frontend's product/batch picker calls Products' `GET /api/v1/products/{id}/batches` directly, which is gated `[RequireFeature("products")]`. A tenant with Pharmacy enabled but Products disabled would see that picker fail even though Pharmacy's own endpoints are correctly gated on `pharmacy`. This is really a gap in ADR-026/FeatureCatalog's per-tenant module toggling — nothing today enforces or even declares module *dependencies* (e.g. "Pharmacy requires Products"), so any module built on another module's HTTP surface would have the same issue. Not fixed here since it's a cross-cutting concern beyond this module's scope, not a Pharmacy-specific bug.

---

### ADR-026: Platform-level per-tenant module configuration, enforced by filtering the JWT at login
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review's "per-tenant configuration store" finding was vague by the time it came up in this backlog — every hospital already gets its own isolated database, so hospital-side settings (Branding, etc.) are already per-tenant by construction. Asked the user what they actually meant: Platform-level settings, set by a Platform Admin rather than a hospital's own admin — specifically, which business-domain modules a hospital's staff can use at all, and a subscription tier.

**Decision**
- `Tenant` gained `EnabledModules` (defaults to every module in the new `HMS.Shared.Kernel.ModuleCatalog` — the 11 keys mirrored from `PermissionSeedData.cs`) and `SubscriptionTier` (freeform string, default "Standard"). Stored as a comma-joined string (module keys are guaranteed comma-free kebab-case identifiers) rather than a native Postgres array, with an explicit `ValueComparer` so EF Core's change tracking is correct.
- New Platform endpoints: `GET/PUT /api/platform/hospitals/{id}/configuration`.
- **Enforcement reuses the existing authorization pipeline instead of adding a new one.** `ITenantContext` (already the per-request tenant state bag `TenantResolutionMiddleware` populates before any hospital request reaches a controller) gained `EnabledModules`. `AuthenticationService.LoginAsync` now strips any permission whose `Permission.Module` isn't in that list out of the JWT it issues — so a disabled module's `[RequirePermission]` gates reject every user at that hospital exactly the way they already reject a role that was never granted the permission. No new middleware, no new authorization requirement, no per-request Platform-DB lookup on the hot path.
- This is necessarily a login-time check, not a live one: a user already holding a JWT keeps whatever permissions it was issued with until it expires (hospital tokens have no revocation store — see ADR-020's identical limitation for the Platform side). A module toggle takes effect on that user's *next* login.
- Frontend: a "Configure" action per hospital on the Platform dashboard opens a dialog with a checkbox per module (reusing `ROLE_MODULES`' labels, `frontend/web/src/features/roles/modules.ts`) and a subscription-tier field.

**Consequences**
- No existing tenant loses access: the migration's column default is the *full* module list (applied to every existing row by Postgres when the `ALTER TABLE ADD COLUMN ... DEFAULT` runs), not an empty/restrictive one.
- **Live-verified end to end** — this changes what a JWT actually grants, so a bug could either fail to restrict (security gap) or over-restrict (lock out a whole hospital): configured a real tenant to disable Pharmacy, logged in as that hospital's Super Admin, confirmed `GET /api/v1/products` now returns 403 while `GET /api/v1/users` (a still-enabled module) returns 200, then re-enabled Pharmacy and confirmed access returned.
- **Caught and fixed during that live verification, not by the unit tests, a second unrelated pre-existing bug**: `IdentityModule.cs` never registered `IValidator<ChangePasswordRequest>` in DI — the validator class shipped in ADR-023 (PR #64), but the registration line was missed, and ADR-023 explicitly skipped live verification ("follows an already-proven pattern... fully covered by unit tests"). The result: **every hospital login has been throwing a 500** (`AuthenticationController`'s constructor can't be resolved — it now takes the change-password validator too) since PR #64 merged, invisible to unit tests because DI container resolution isn't something a mocked-constructor unit test exercises. Fixed by adding the missing `services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>()` line, then audited every other validator class in Identity and Platform against their module's DI registration to confirm no sibling gaps.
- Not unit-tested at the `AuthenticationService.LoginAsync` filter level: `RolePermission.Permission`'s navigation property has a private setter with no EF-free way to populate it in a pure in-memory domain test (confirmed no existing test in this codebase ever populates it either — every `AuthenticationServiceTests` role is created with zero permissions attached). Covered by the live verification above instead, same posture as ADR-018/019/020's identical "can't be meaningfully unit-tested here" call.
- Module keys are not validated against `ModuleCatalog` server-side — an unrecognized key is inert (it simply never matches any `Permission.Module`), and this is a Platform-Admin-only internal tool, so a typo's blast radius is low and accepted rather than adding a second place the catalog must stay in sync.
- Subscription tier is display/storage only — not wired to any billing or enforcement logic yet.

---

### ADR-025: Tenant delete is soft-delete only, with a server-enforced confirmation step
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that tenant destructive operations needed safeguards (soft-delete, dry-run, confirmation) before a delete/purge/export API could be added at all — at the time, no delete capability of any kind existed, only Activate/Deactivate. Asked the user how far this should go: hard-delete (actually dropping a tenant's database) is unrecoverable without a separate backup/restore story this app doesn't have, so the user chose soft-delete only — no database-drop capability is built.

**Decision**
- `DELETE /api/platform/hospitals/{id}?confirmHospitalCode=xxx` soft-deletes a `Tenant` (reusing `Entity.IsDeleted`/`DeletedAt`/`DeletedBy`, already present on every aggregate — no migration needed). This blocks the hospital's staff from signing in for free: `TenantDirectory`'s lookups already go through `ITenantRepository`, whose EF Core query filter (`HasQueryFilter(t => !t.IsDeleted)`) already excludes soft-deleted tenants, so an unresolvable `X-Hospital-Code` naturally rejects login without any new code. The tenant's own database is never touched.
- `confirmHospitalCode` must match the tenant's actual hospital code (case-insensitive) — a **server-enforced** "type to confirm," not just a frontend dialog a script could skip past.
- `GET /api/platform/hospitals/{id}/delete-preview` is a dry-run shown before the confirm dialog — deliberately Platform-side data only (hospital name/code/status/registered-date). No cross-tenant-database row counts: since a soft-delete never touches the tenant's own database, there is nothing over there to preview or warn about.
- Added `Entity.Restore(Guid? updatedBy)` to the shared kernel (symmetric with the existing `SoftDelete`) — soft-delete is fully reversible via `POST /api/platform/hospitals/{id}/restore`, and a new `GET /api/platform/hospitals/deleted` list (bypassing the query filter via `IgnoreQueryFilters()`) is the only way to find a soft-deleted tenant to restore.
- Frontend: the Platform dashboard gained an "Active Hospitals" / "Deleted Hospitals" toggle, a delete confirmation dialog (shows the dry-run preview, requires typing the hospital code, submit disabled until it matches), and a Restore action on the deleted list.

**Consequences**
- **Live-verified end to end**, not just unit-tested — this is the first tenant-destructive capability this app has ever had, and a bug here could lock a live hospital's staff out of the app: registered a throwaway test hospital, previewed and deleted it (confirmed the frontend disables the confirm button on a mismatched code, and that a mismatched code is also rejected server-side), confirmed it disappeared from the active list and total count, confirmed it appeared on the Deleted list, restored it, confirmed it reappeared active with its original status, then deleted it again to leave a clean state.
- **Caught and fixed during that live verification, not by the unit tests**: hospital registration (`POST /api/platform/hospitals`, shipped in ADR for idempotency, PR #56) has been completely broken in any real browser since it shipped — `CorsConfiguration.cs`'s allowed-headers list never included `Idempotency-Key`, so the browser's CORS preflight silently blocked the actual POST from ever reaching the server (`net::ERR_FAILED`, no server-side log at all). This was invisible to prior verification because that used `curl`, which doesn't enforce CORS. Fixed by adding `Idempotency-Key` to `WithHeaders(...)` — a one-line fix, included in this PR since it was directly blocking this fix's own live verification and is a trivial, unambiguous correction once found.
- No new migration: `Tenant` already inherited `IsDeleted`/`DeletedAt`/`DeletedBy` from `Entity`; this fix only adds behavior around columns that already existed.
- Closes out the backlog's "tenant delete/purge/export API" item as explicitly out of scope, not merely deferred: a true hard-delete/purge/export capability was the user's own explicit non-choice here (soft-delete only, not "soft-delete now, hard-delete later"). If purge/export becomes a real need later, it deserves its own design pass addressing backup/restore first, not a bolt-on to this ADR's scope.

---

### ADR-024: TOTP-based MFA for Platform Admin accounts
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that Platform Admin accounts (the only accounts that can register/deactivate/delete hospital tenants) had no second factor — a leaked or brute-forced password (however unlikely given ADR-017/ADR-023's lockout and complexity rules) was the only thing standing between an attacker and the entire multi-tenant platform.

**Decision**
Built RFC 6238 TOTP-based MFA, self-service per Platform Admin account:
- `PlatformUser` gained `MfaEnabled`/`MfaSecret` (the secret is encrypted at rest via `IPlatformMfaSecretProtector`, wrapping ASP.NET Core's built-in Data Protection API — no new infrastructure, just `builder.Services.AddDataProtection()`, already part of the shared framework).
- New endpoints, all under `/api/platform/auth/mfa/*`: `GET status`, `POST setup` (generates a secret, returns it once as manual-entry text plus an `otpauth://` URI), `POST enable` (confirms a code, the only way `MfaEnabled` turns on), `POST disable` (requires a valid current code, not just an authenticated session — same reasoning as requiring the current password to change a password).
- Login became two-step for an MFA-enabled account: `LoginAsync` returns a short-lived (5-minute), single-use `PlatformMfaChallenge` token instead of a real JWT once the password checks out; a new `POST /mfa/verify` exchanges that token plus a TOTP code for the real token. `PlatformLoginResponse` now has an `MfaRequired` discriminator.
- TOTP itself uses the Otp.NET library (`Directory.Packages.props`) rather than hand-rolling HMAC-based one-time-code math — a security primitive is not the place to save a dependency.
- New `frontend/web/src/pages/platform/PlatformSecuritySettingsPage.tsx` (linked from the dashboard header) drives setup/enable/disable; `PlatformLoginPage` gained the code-entry second step.

**Deliberately not built: QR-code rendering for setup.** The manual-entry key and `otpauth://` URI (copyable text) cover the same authenticator apps a QR scan would, without adding an image-generation dependency to the frontend for a single screen — can be added later as a pure UX improvement if manual entry proves annoying in practice.

**Consequences**
- **Live-verified end to end**, not just unit-tested (this gates the only account type that can provision/deactivate every tenant — the standing "unit tests are enough" default was set aside here): logged in as the seeded Platform Admin, set up MFA, confirmed the code, logged out, logged back in and confirmed the password step alone no longer completes login (`MfaRequired: true`), and confirmed the real token only issues after the correct TOTP code.
- **Caught and fixed during that live verification, not by the unit tests**: the first `IPlatformMfaChallengeStore` implementation consumed the challenge token on the *first* verify attempt regardless of whether the code was right — one mistyped digit permanently burned the login attempt (correct code on retry was rejected as "challenge invalid/expired"). Fixed by splitting `ConsumeAsync` into a non-consuming `ValidateAsync` (peek) called before checking the code, and `ConsumeAsync` called only after the code is confirmed correct — a wrong code can now be retried until it's right or the 5-minute window naturally expires. This is exactly the class of bug the existing mocked unit tests couldn't catch (they asserted the *intended* call sequence, which was the bug) — a reminder that this backlog's "unit tests are enough" default has real limits for anything with request/response state spanning two calls.
- New migration `AddPlatformUserMfaAndChallenges` (`platform.platform_users.mfa_enabled`/`mfa_secret`, new `platform.platform_mfa_challenges` table).
- `platform_mfa_challenges` rows are never pruned — same accepted-for-now posture as ADR-020's `revoked_tokens`, low volume, worth a scheduled cleanup once this app has a background-job mechanism.
- Recovery if a Platform Admin loses their authenticator device isn't self-service (no backup codes) — the only path today is another Platform Admin (or direct DB access, for the single-seeded-admin case) clearing `MfaEnabled`/`MfaSecret`. Backup codes are a reasonable follow-up, not built here to keep this pass scoped to the core second-factor gate.

---

### ADR-023: Password-complexity policy hardened; forced password change replaces a full invite/reset-link email flow
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that a hospital's Super Admin never goes through an invite/reset-link flow: the Platform Admin creating the hospital literally types the new Super Admin's password into the `CreateHospitalRequest` form (`CreateHospitalRequestValidator.SuperAdminPassword`, min 8 characters, no complexity rule), which is provisioned verbatim by `IdentityModule.ProvisionTenantSuperAdminAsync`. The same pattern exists for any ordinary hospital user: `UsersController.SetPassword` (`identity-administration.edit`) is an admin resetting a password the user never chose, again with only an 8-character minimum. In both cases, a second party ends up knowing the account's password with no mechanism for the account owner to ever rotate it away from what that person chose — and no self-service "change my own password" endpoint existed anywhere in the Identity module to do so.

**Decision**
Two changes, scoped to what's buildable without inventing infrastructure this app doesn't have:

1. **Password complexity, centralized.** Added `HMS.Shared.Kernel.PasswordPolicy` (min 10 characters, upper+lower+digit+special-character regex) and applied it everywhere a password is set: `CreateHospitalRequestValidator.SuperAdminPassword`, `SetPasswordRequestValidator.Password`, and the new `ChangePasswordRequestValidator.NewPassword` below — one definition instead of three independently-drifting 8-character minimums. Mirrored on the frontend (`passwordPolicy.ts`, applied to `hospitalValidation.ts` and `userValidation.ts`'s `setPasswordSchema`).

2. **Forced password change substitutes for an invite/reset-link email.** `User` gained `MustChangePassword`, set `true` by `SetPasswordHash` (anyone-but-the-user-themselves setting a password — hospital registration's Super Admin creation, and `UsersController.SetPassword`) and cleared by a new `ChangeOwnPassword` (self-service only). A new self-service endpoint, `POST /api/v1/auth/change-password` (`AuthenticationService.ChangePasswordAsync`, verifies the current password before rotating it — this is the first "change my own password" capability the Identity module has ever had), is the only way to clear the flag. `LoginResponse.User.MustChangePassword` surfaces the flag; the frontend's `ProtectedRoute` redirects any authenticated user carrying it to a new `/change-password` page and blocks every other route until they submit a successful change.

**Deliberately not built: an actual email-based invite/reset-link system.** No mail-sending infrastructure (SMTP/SendGrid/etc.) exists anywhere in this codebase — building one from scratch, including template design and a provider choice, for a single flow would be speculative infrastructure this app has no other use for yet (same reasoning as ADR-019's alerting deferral and ADR-021's cloud-secrets-manager deferral). Forced-change-on-first-use closes the actual security gap (the password a second party chose stops being valid the moment the account owner logs in) without that infrastructure.

**Also deliberately not built: hard server-side gating of every other endpoint while `MustChangePassword` is true.** The flag is enforced by the frontend redirect only — a determined API caller with the still-issued bearer token could keep calling other endpoints while the flag is set. A true server-side block would need a central chokepoint (e.g. `JwtConfiguration`'s `OnTokenValidated`, the same seam ADR-020 used for Platform-token revocation), but that requires a tenant-scoped `IdentityDbContext` lookup at a point in the pipeline before tenant resolution has necessarily completed — a real design question, not a drop-in addition. Left as an explicit follow-up rather than risking a rushed, possibly-broken middleware change.

**Consequences**
- New migration `AddMustChangePasswordToUsers` (`identity.users.must_change_password`, default `false` — existing rows are unaffected).
- The dev-seeded default Super Admin (`IdentityDataSeeder`, `SuperAdminSeedOptions` — see ADR-021) also goes through `SetPasswordHash`, so it now requires a change-password on first login too. This is intentional, not an oversight: that password is a known value from `dotnet user-secrets`/`appsettings`, exactly the case this fix targets.
- `PlatformUser` (Platform Admin accounts) was deliberately left out of this fix — there is no `PlatformUser` creation endpoint anywhere in the codebase today (only `PlatformDataSeeder`), so the "someone else chooses your password" problem this ADR addresses doesn't currently exist for that account type.
- Covered by unit tests: `UserTests` (`SetPasswordHash` sets the flag, `ChangeOwnPassword` clears it without touching the audit columns), `AuthenticationServiceTests` (login surfaces the flag; `ChangePasswordAsync` success/wrong-current-password/unknown-user cases).
- Not live-verified against a running server — the change follows an already-proven pattern (`SetPasswordAsync`/`LoginAsync` themselves) and is fully covered by the unit tests above; per the standing practice on this backlog, live verification is reserved for changes with no other validation path.

---

### ADR-022: Permission-granularity gaps closed for Documents, Users, HR, IPD, Products, Calendar
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that most hospital-facing controllers were protected only by the global "any authenticated user" fallback policy (ADR from the earlier authorization-hardening pass), with no permission-level (`[RequirePermission]`) gating — unlike Masters, Finance/Billing, Roles, and Patients, which already got this treatment in earlier PRs (#51). Specifically named: Documents, Users (Identity), HR (5 controllers), IPD (5 controllers), Products (8 controllers), and Calendar.

**Decision**
Added `[Authorize]` + `[RequirePermission("<module>.<action>")]` to every action across all 19 remaining controllers in those 6 modules (~101 actions), mapping HTTP verb to action (GET→`view`, POST→`create`, PUT/PATCH→`edit`, DELETE→`delete`; state-change-only POSTs like activate/deactivate/publish/discharge map to `edit`), using the existing seeded permission catalog (`PermissionSeedData.cs`) rather than inventing new modules. The catalog-to-code-module mapping was inferred from the catalog's own frontend labels (`ROLE_MODULES` in `modules.ts`) since it isn't 1:1 with folder names:
- Users, Permissions → `identity-administration` (label: "Roles, Users & Settings" — explicitly names Users)
- IPD (Admissions, Wards, Beds, AdmissionCharges, Dashboard) → `clinical-care`
- Products (all 8 controllers) → `pharmacy`
- Documents → `records-compliance`
- HR (Shifts, ShiftAssignments, ShiftSwapRequests, StaffAvailability, WeeklyRosters) → `workforce-admin`
- Calendar (Events) → `engagement`

**Consequences**
- Verified live: confirmed every previously-open route across all 6 modules now returns `401` with no token (where before some accepted anonymous requests entirely, per the earlier audit), and returns `200` for the seeded Super Admin token — the second check is the important one, since it proves the `module.action` strings actually match the real seeded catalog rather than just compiling.
- No new permission-catalog entries or migration needed — every mapping used an existing seeded module.
- `pharmacy`/`clinical-care`/`records-compliance`/`workforce-admin`/`engagement` were previously unused by any controller (only referenced in the frontend's static `ROLE_MODULES` list) — a role granted one of these before this change had no actual effect; now it does.
- Documents already had a bespoke owner-type-based authorization layer (`DocumentActor`, 403 responses per owner type) — `[RequirePermission]` is additive defense-in-depth there, not a replacement.

---

### ADR-021: Local dev secrets move to `dotnet user-secrets`; no cloud secrets-manager built yet
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that `appsettings.Development.json` hardcoded default admin/super-admin passwords (and the JWT signing key) in plaintext, checked into git. Production `appsettings.json` already ships these same keys as empty strings, relying on the deploying environment to override them — a reasonable pattern, but nothing analogous existed for local dev, and no actual secrets-manager mechanism (nor documentation of the override convention) existed anywhere.

**Decision**
Moved the three genuinely sensitive dev values — `Jwt:SigningKey`, `SuperAdminSeed:Password`, `PlatformAdminSeed:Password` — out of `appsettings.Development.json` and into [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (`dotnet user-secrets init` added `<UserSecretsId>` to `HMS.Api.csproj`; ASP.NET Core loads user-secrets automatically in Development, no code change needed). These now throw the existing `InvalidOperationException` at startup if unset, same as production already does — fails loudly, not silently. Local Postgres connection strings were deliberately left alone (they only ever point at a developer's own local database, not a meaningfully sensitive secret). Documented the full pattern — dev via user-secrets, staging/prod via the `Section__Key` environment-variable override convention ASP.NET Core already supports — in `docs/Configuration.md`'s previously-empty "Secrets Handling" section.

**Deliberately not done: an actual cloud secrets-manager integration** (Azure Key Vault, AWS Secrets Manager, etc.). This app has no committed-to hosting target yet (`docs/Deployment.md` is still a stub) — building against an unconfirmed target would be speculative infrastructure nobody can verify. Explicitly documented as the still-open gap so it isn't mistaken for "solved."

**Consequences**
- Verified live: full app startup (migrations + seeding through every module) and a real login round-trip (issue a token, then use it against `GET /me`) both succeed reading the signing key and seed passwords from user-secrets, with zero plaintext secrets left in `appsettings.Development.json`.
- A fresh clone now requires the three `dotnet user-secrets set` commands documented in `Configuration.md` before `dotnet run` works — a one-time, documented setup cost, traded for no longer having real (if low-stakes, dev-only) credentials in git history.
- CI is unaffected — `build.yml`'s `dotnet build`/`dotnet test` never starts the host, so it never needed these values.

---

### ADR-020: Server-side revocation for Platform tokens; httpOnly-cookie storage deliberately deferred
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that the Platform session token sits in `sessionStorage` (script-readable, no httpOnly option) with no server-side revocation — a leaked or "logged out" token stayed valid until natural JWT expiry, since `logout()` only ever cleared client-side storage. This bundles two separable concerns: (A) how the token is stored client-side, and (B) whether the server can actually invalidate a token before it naturally expires.

**Decision**
Built (B) — server-side revocation — in full: `PlatformJwtTokenGenerator` now issues a `jti` claim on every Platform token; a new `platform.revoked_tokens` table + `IRevokedTokenStore` (a public seam, like `ITenantProvisioner`) lets `JwtConfiguration`'s `OnTokenValidated` reject an already-revoked `jti` on every request, and a new `POST /api/platform/auth/logout` (`[Authorize(Policy = "Platform")]`) revokes the caller's own token. The frontend's `PlatformAuthContext.logout()` now calls it (best-effort, fire-and-forget — a network failure must never block a local logout). The revocation check only activates for tokens carrying a `PlatformUserId` claim, so hospital-user tokens are untouched — matches the finding's scope.

**Deliberately deferred: (A), moving the token out of `sessionStorage` into an httpOnly cookie.** This codebase has zero existing cookie/CSRF infrastructure anywhere — every endpoint today is Bearer-token-only, which is itself immune to CSRF by construction (a forged cross-site request can't attach an `Authorization` header). Switching the Platform token to a cookie would trade one class of risk (XSS-based token theft, mitigated by httpOnly) for another (CSRF) that this app has no existing defense for, and building one (SameSite handling across the frontend's different local dev origin, or a double-submit CSRF token scheme) is a real architectural addition, not a small tweak — especially since frontend (port 5173) and API (port 58158) are cross-origin in local dev, which itself constrains cookie `SameSite`/`Secure` options. Doing this properly deserves its own dedicated pass with its own design, not a bolt-on here.

**Consequences**
- Verified live: logged in as the seeded Platform Admin, confirmed the token works (`GET /me` → 200), called `POST /logout` (→ 204), then confirmed the *same* token is rejected on every subsequent request including a second logout call (→ 401 both times) — the actual revocation behavior, not just that the code compiles.
- This can't be meaningfully unit-tested — there's no abstraction boundary to mock the JWT bearer middleware pipeline behind, and `HMS.IntegrationTests` (where a real pipeline test would belong) is excluded from CI (see ADR-018/ADR-019's identical caveat). Live verification is the actual coverage here.
- `revoked_tokens` rows are never pruned — acceptable near-term (Platform Admin logout volume is low), but worth a scheduled cleanup (`DELETE WHERE expires_at < now()`) once this app has a background-job mechanism, since an expired row is safe to discard (the token would already fail JWT `exp` validation on its own).
- Still open: the underlying `sessionStorage` storage and the shared signing key/issuer/audience between hospital and platform tokens (ADR-013's "Consequences" already flagged the latter as unresolved).

---

### ADR-019: A failed provisioning rollback becomes a durable, dashboard-visible alert, not just a log line
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that when tenant provisioning fails and the rollback (`DROP DATABASE`) *also* fails, `TenantProvisioningService` only logged "manual cleanup required" — nothing surfaced the failure to a human, so an orphaned tenant database could silently sit there indefinitely with no signal beyond a log line nobody may ever read.

**Decision**
Added a new `ProvisioningAlert` record (Platform module, `platform.provisioning_alerts`) and `IProvisioningAlertStore` (`RaiseAsync`/`GetCountAsync`), a public seam like `ITenantProvisioner`/`ITenantMigrationService` so `HMS.Api`'s `TenantProvisioningService` can write to it without the Platform module knowing about database provisioning. When the rollback's `DROP DATABASE` fails, the existing log line is elevated to `LogCritical` and a `ProvisioningAlert` row is raised (best-effort — if writing the alert itself fails, that's logged too, but must never mask or throw over the original rollback failure). `TenantDashboardStatsResponse` gained a `ProvisioningAlertCount` field; the Platform Portal dashboard now shows a fourth stat tile, "Provisioning Alerts," styled with a warning color when nonzero.

This is deliberately not a full external alerting integration (email/Slack/PagerDuty) — nothing like that exists anywhere in this codebase yet, and building one from scratch for a single failure path would be a large, speculative addition requiring new credentials/config. Persisting the failure durably and surfacing it on the existing dashboard is what actually closes "silently sit there," without inventing infrastructure this app doesn't have a use for yet.

**Consequences**
- Verified live: logged in as the seeded Platform Admin, confirmed `GET /api/platform/hospitals/stats` returns `provisioningAlertCount`, and the dashboard renders the new tile correctly (0 in the normal case).
- No resolve/acknowledge lifecycle — rows accumulate. Acceptable for now since this only fires on a genuine double-failure (provisioning fails *and* the rollback also fails), expected to be rare; add a resolve action if that assumption turns out wrong.
- `TenantProvisioningService`'s raw Npgsql `CREATE`/`DROP DATABASE` calls remain untested by `HMS.UnitTests` (no abstraction to mock them behind, and `HMS.IntegrationTests` is excluded from CI — see ADR-018's identical caveat) — verified instead via the live dashboard check above and the existing `PlatformDashboardServiceTests`/build.

---

### ADR-018: API-wide rate limiting via ASP.NET Core's built-in RateLimiter
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that there was no rate limiting anywhere on the API host — nothing stopped a flood of requests (from one client or distributed across many) regardless of authentication state, and the per-account lockout added in ADR-017 is per-account, not per-IP, so it doesn't help against an attacker spraying guesses across many different usernames/emails.

**Decision**
Added `RateLimitingConfiguration` (`HMS.Api/Configuration`), using ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware — no extra package needed, already part of the shared framework. Two layers, both partitioned per client IP (`HttpContext.Connection.RemoteIpAddress`, not `X-Forwarded-For` — nothing here is a trusted reverse proxy yet, so an attacker-controlled header must not be able to bypass the limiter):
- A **global limiter** applied to every request by default: 200 requests/minute per IP, generous enough not to interfere with normal UI usage (dashboard polling, React Query refetches).
- A stricter **"Login" policy** (10 requests/minute per IP), applied explicitly via `[EnableRateLimiting("Login")]` to both login actions — the per-IP complement to the per-account lockout.

Rejections return `429 Too Many Requests` with the same `ApiErrorResponse` shape every other error uses. The middleware runs early in the pipeline (right after CORS, before authentication/authorization/tenant resolution) so a flood is rejected before spending any JWT-validation or DB work. The policy name constant lives in `HMS.Shared.Infrastructure` (not `HMS.Api.Configuration`) since modules must never reference `HMS.Api` — only `HMS.Api` may register policies, but modules need the same name to apply them.

**Consequences**
- `HMS.IntegrationTests` is excluded from CI (`dotnet test --filter "FullyQualifiedName!~HMS.IntegrationTests"`, `build.yml`) because it needs Docker/Testcontainers, which aren't available in this environment or in CI — so this change is verified by build success and the standard, well-documented ASP.NET Core `RateLimiter` API surface, not by an automated end-to-end test hitting real HTTP 429s. Worth adding a real integration test once Docker is available in CI.
- Thresholds are hardcoded, not configurable, for the same reason as ADR-017's thresholds — one deployment target, no evidence yet of needing per-environment tuning.

---

### ADR-017: Account lockout after 5 wrong-password attempts, on both login endpoints
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that neither login endpoint (hospital `AuthenticationService.LoginAsync` nor Platform `PlatformAuthenticationService.LoginAsync`) had any account lockout or brute-force throttling — an attacker could try unlimited passwords against a known username/email with no penalty.

**Decision**
Added `FailedLoginAttempts`/`LockedOutUntil` to both `User` (hospital-side, per-tenant) and `PlatformUser`. A wrong password increments the counter and persists it immediately (even though the overall login still fails); reaching 5 attempts sets a 15-minute lockout. A locked-out account is rejected before the password is even checked — no point spending a hash comparison on an account that can't log in regardless. A successful login resets the counter. Both endpoints keep returning the exact same generic `InvalidLogin`/`IDENTITY.INVALID_LOGIN` message and error code used for every other rejection reason (per the existing, explicitly-documented "never reveal which check failed" convention in both services) — a locked-out account looks identical to a wrong password from the outside; only the server logs distinguish it (`LogWarning` instead of `LogInformation`).

**Consequences**
- Thresholds (5 attempts, 15-minute lockout) are hardcoded constants, not configurable — there's one deployment target and no evidence yet of needing per-environment tuning; revisit if that changes.
- Only wrong-password attempts count toward the threshold — a login rejected for a nonexistent username, an inactive account, or a login-type/role mismatch doesn't increment anything, since those aren't password-guessing attempts against a specific account's credential.
- This is per-account throttling, not per-IP — an attacker distributing guesses across many usernames from one IP is unaffected (that's what finding "No rate limiting anywhere on the API host" — tracked separately — is for).

---

### ADR-016: Stop returning DatabaseName to the frontend
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that `CreateHospitalResponse`/`TenantListItemResponse` returned the tenant's internal PostgreSQL database name straight to the browser, and `HospitalTable.tsx` rendered it as a visible column — an infrastructure implementation detail with no product reason to reach the client, and a minor information-disclosure surface (it reveals the exact naming scheme used to derive one tenant's database from another's).

**Decision**
Removed `DatabaseName` from both response contracts and their mapping code in `HospitalRegistrationService`/`PlatformDashboardService`. Removed the corresponding `databaseName` field from the frontend's DTO mirrors and the "Database Name" column from `HospitalTable.tsx`. The backend's own internal use of `Tenant.DatabaseName` (connection-string resolution via `TenantDirectory`, migrations, logging) is untouched — this is purely about what crosses the API boundary.

**Consequences**
- No behavior change for Platform Admins beyond one fewer (and not useful) table column.
- Any future "show ops/support which physical database backs a tenant" need should be a deliberately separate, more tightly-scoped surface (e.g. gated to `SuperAdmin` only, per ADR-014), not the same response every Platform Admin already receives for the dashboard list.

---

### ADR-015: Hospital registration requires an Idempotency-Key header
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that hospital registration had no idempotency/retry protection — a client-perceived timeout followed by a resubmit could double-provision a hospital database, since the only existing guard (checking `platform.tenants` for the hospital code) only catches a duplicate *after* a prior request has already finished writing that row, not while provisioning is still in flight.

**Decision**
`POST /api/platform/hospitals` now requires an `Idempotency-Key` header (400 if missing). A new `platform.idempotency_keys` table, guarded by a unique index on `key`, is used to atomically "reserve" a key before provisioning starts: the first request to insert wins and proceeds; a concurrent request with the same key gets `409 PLATFORM.IDEMPOTENCY_KEY_IN_PROGRESS` instead of racing into a second provisioning; a later retry after the first request finished gets the original cached `Result<CreateHospitalResponse>` replayed verbatim (`409`/`201` matching the original outcome) instead of re-executing; and a key reused for a different request body gets `409 PLATFORM.IDEMPOTENCY_KEY_REUSED`. The frontend (`useCreateHospitalMutation`) generates one key per page-mount (`crypto.randomUUID()`), reused across retries of the same submission attempt, never regenerated per `mutate()` call.

**Consequences**
- Verified live against the real API + Postgres: a genuine concurrent double-submit (two requests firing ~150ms apart with the same key) produced exactly one `Provisioned hospital` log line and one `IN_PROGRESS` rejection — confirmed via the running server, not just unit tests.
- Scoped narrowly to this one endpoint (`IHospitalRegistrationIdempotencyStore`), not a generic ASP.NET Core idempotency middleware — no other endpoint needs this yet.
- Existing/older API clients that don't send the header now get a hard `400` on hospital creation — acceptable since the only consumer is this repo's own Platform Portal frontend, which was updated in the same change.

---

### ADR-014: PlatformUser gets a two-tier role (SuperAdmin / SupportUser), not a full permission catalog
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that `PlatformUser` had no role/permission model at all — every Platform Admin who logs in has identical, ungated power over every hospital tenant, including destructive actions (register a hospital, enable/disable a hospital, trigger a tenant migration). At the same time, there is currently no API to create additional Platform Admins at all — only one account is ever seeded (`PlatformAdminSeed`) — so building a full dynamic permission catalog (mirroring `HMS.Modules.Identity`'s hospital-side Roles/Permissions module) would be speculative infrastructure with no consumer.

**Decision**
Add a `PlatformRole` enum (`SuperAdmin`, `SupportUser`) directly on `PlatformUser`, carried in the JWT as a `PlatformRole` claim (mirroring the existing `PlatformUserId`/`Email`/`FullName`/`LoginType` claim shape). A new `PlatformSuperAdmin` authorization policy (`LoginType==platform` AND `PlatformRole==SuperAdmin`) gates the three destructive/high-privilege `HospitalsController` actions — `Create`, `UpdateStatus`, `Migrate`. Read actions (`GetAll`, `GetStats`) stay on the existing `Platform` policy, so a `SupportUser`-role token can view the dashboard but not mutate anything. The seeded Platform Admin becomes `SuperAdmin` (both in the seeder and via the migration's column default, which backfills the one existing row).

**Consequences**
- This is a fixed two-tier privilege split, not a dynamic per-tenant permission model — the review's own "support user can configure permissions per tenant" idea still has no foundation beyond this.
- No `PlatformUser` CRUD/invite API exists yet, so `SupportUser` is currently unreachable in practice — this PR lays the foundation and enforces it at the policy layer, without building unused management UI for a role nobody can yet hold.
- The Platform Portal frontend does not yet gate any UI on role (e.g. hiding "Register Hospital" for a SupportUser) — tracked separately; harmless today since there's no way to create a SupportUser account through the app.

---

### ADR-013: Platform tenant lifecycle actions record the calling Platform Admin, not `null`
**Date:** 2026-08-19
**Status:** Accepted

**Context**
The architecture/security review flagged that hospital registration and enable/disable both hardcoded `createdBy`/`updatedBy` to `null` on the `Tenant` aggregate, even though `Entity` already supports a real actor id — there was simply nothing populating it from the caller. That leaves the platform's most destructive/high-privilege actions (provisioning a hospital, enabling/disabling one) with no audit trail of which Platform Admin performed them.

**Decision**
Read the calling Platform Admin's id from the `PlatformUserId` JWT claim (via a new `ClaimsPrincipalExtensions.GetPlatformUserId()`, mirroring the existing hospital-side `GetUserId()`) in `HospitalsController`, and thread it through `IHospitalRegistrationService.RegisterAsync` and `IPlatformDashboardService.UpdateStatusAsync` as an `actorId` parameter, exactly like the `actorId`/`updatedBy` pattern already used across Masters, Billing, and Patients. `Tenant.Create` and `Tenant.SetStatus` already accepted this parameter — only the callers needed wiring.

**Consequences**
- `platform.tenants.created_by`/`updated_by` now reflect the real acting Platform Admin.
- `Migrate` was left out of scope — `Tenant` isn't mutated by that action, so there's nothing to attribute.
- This is a narrow, mechanical fix; it does not address the larger gap that `PlatformUser` still has no role/permission model (tracked separately).

---

### ADR-012: Billing ships as a real backend module — one unified Invoice/InvoiceLineItem/Payment engine, not four parallel billing blocks; Department/Consultant/Service stay free-text, not Masters-backed FKs
**Date:** 2026-08-18
**Status:** Accepted

**Context**
`HMS.Modules.Billing` was an empty scaffold (no Domain/Application/Infrastructure/Endpoints — see the earlier architecture review) while the frontend shipped a fully-built Billing UI (invoice ledger, per-category billing cards, discount approval, payment recording) against `mockBillingStore.ts`, a browser-local mock with no real persistence. This repo's own `docs/BusinessRequirementsAnalysis.md` had already flagged the client's original brief modeling billing as four separate near-duplicate blocks (OP/Consultation, Radiology, Lab, Procedure) as a revenue-leakage risk, and recommended a single invoice engine instead — but that recommendation had never been acted on, and no target database schema existed to build against.

**Decision**
1. **One unified invoice engine.** `Invoice` (header) owns a collection of `InvoiceLineItem`s, each carrying a `BillingType` enum (Consultation/Radiology/Laboratory/Procedure) as a plain category field — not four separate tables. This matches the frontend's own `BillingItem` shape (already normalized this way) and the docs' recommendation; it also means the "OP Billing" vs "Consultation Billing" naming inconsistency the requirements review flagged has nothing to attach to anymore.
2. **A real `Payment` table**, separate from `InvoiceLineItem`, recording amount/method/who/when per payment — the mock store only ever flipped a line item's status with no receipt trail at all.
3. **`DepartmentId`/`ConsultantId`/`ServiceId` on a line item stay free-text strings, not Masters-backed Guid FKs.** The frontend's billing catalog (`frontend/web/src/features/billing/billingCatalog.ts`) is its own hardcoded pricing/staff reference data — slug-style IDs like `"cardiology"`/`"dr-nirmala"` with fee multipliers — entirely independent of Masters' real Department/Consultant directory that Patient registration and IPD use. Validating these fields against Masters would have rejected every invoice the current UI can actually produce. Started this module out validating them as Guids against `IDepartmentService`/`IConsultantService` (mirroring IPD's `Admission`), caught the mismatch before merging, and reworked to free-text before generating the migration rather than shipping the broken validation and patching later. Unifying the two catalogs (making Billing's pricing data reference real Masters departments/consultants) is a real future improvement, not done here — it would mean rebuilding `billingCatalog.ts` against `mastersApi`/`consultantsApi`, out of scope for "replace the mock store."
4. **`PatientId` is still validated for real** against `IPatientService.GetByIdAsync` — Patients has a real backend and the frontend always has a genuine patient Guid to send, unlike Department/Consultant/Service.
5. **Permission gating covers every action, including reads** (`finance-billing.view` on GETs, `.create`/`.edit` on mutations) — matches `RolesController`'s now-current end-to-end pattern (see the identity-administration ADR), not the older reads-are-baseline-only pattern, since an invoice is patient financial data.
6. **Frontend**: `apiBillingRepository.ts` mirrors `apiRoleRepository.ts`'s live-API-with-mock-fallback pattern exactly — same shape, same `NetworkError`-only fallback. `PatientDetails.tsx`'s Billing tab and `features/reports/incomeExpenseReport.ts` (previously synchronous mock reads) were converted to real React Query hooks (`usePatientInvoicesQuery`, `useInvoicesForReportQuery`) in the process.

**Consequences**
Verified end-to-end against a live `hms-api-dev`/`hms-web-dev`/local Postgres: a real patient registration with a Consultation billing line produced a real invoice (`INV-2026-000001`) retrievable from the ledger, the patient's Billing tab, and the Income & Expense report, with no "Demo data" fallback badge; a second invoice created via the standalone OPD Billing Entry screen correctly incremented to `INV-2026-000002`; Record Payment posted a real `Payment` row and flipped the line item to Paid; an anonymous request to the new endpoints was correctly rejected with 401. `dotnet test` — 50 architecture tests + 360 unit tests, all green (11 new for Billing). `tsc --noEmit` and `eslint` clean.

The Income & Expense report's income side now depends on `getAllInvoicesForReport()`, which fetches a single page at the server's `PagedRequest.MaxPageSize` (100) — a hospital with more than 100 invoices in a report's date range will only see the first 100 until this gets a dedicated report endpoint. This is a stated, known limitation (see the function's own doc comment), not a silent gap.

### ADR-011: Roles Management's permission catalog (`ROLE_MODULES`) was missing `identity-administration` — fixed by adding it, not by fetching `GET /api/v1/permissions` dynamically
**Date:** 2026-08-18
**Status:** Accepted

**Context**
A review of the Billing/Roles "mock-data" concern found Roles Management further along than assumed: `apiRoleRepository.ts` already calls the real `RolesController` end-to-end (list/get/create/update, with `activate`/`deactivate` follow-up calls to work around create/update not carrying `status`), falling back to `mockRolesStore.ts` only on a genuine `NetworkError` — the same resilience pattern used by Patients and Masters, not a "fake by default" implementation. `RolesListPage.tsx` already surfaces a "Demo data — API not connected" badge whenever that fallback is active.

The one real bug: `frontend/web/src/features/roles/modules.ts`'s hand-maintained `ROLE_MODULES` list (used to render the permission matrix, build a new role's empty permission grid, and reconcile the backend's flat `permissionKeys` into that grid) had 10 entries, but the real seeded catalog (`PermissionSeedData.cs`) has 11 — `identity-administration`, the module gating Roles/Users/Masters/Settings themselves, was missing. Consequence was worse than a display gap: `apiRoleRepository.ts`'s `toPermissionKeys()` only emits keys for modules in `ROLE_MODULES`, so saving an edit to any role that already held `identity-administration.*` permissions (Super Admin, if ever edited through this UI) would have silently stripped them on save — the matrix never showed them as granted, so nothing would prompt an editor to notice they'd be lost.

**Decision**
Added `{ id: 'identity-administration', label: 'Roles, Users & Settings' }` to `ROLE_MODULES` and corrected the file's doc comment (it claimed "Roles Management has no backend yet," which was already false before this change). Considered fetching the catalog live from `GET /api/v1/permissions` (`PermissionsController` already exists and is read-only/system-seeded) instead of hand-mirroring it, which would remove this whole class of drift permanently — but that requires threading a resolved module list through `apiRoleRepository.ts`'s four exported functions and both Roles pages' loading states, a real refactor across ~8-10 files. Given the catalog is explicitly documented as static and system-seeded (not something an admin edits), the same low-drift-risk tradeoff already accepted for `config/navigation.ts`'s hand-mirrored permission-group strings applies here — a one-line fix now, with the dynamic-fetch version left as a follow-up if the catalog starts changing often enough for manual sync to become a real problem.

**Consequences**
The permission matrix, new-role defaults, and DTO reconciliation all pick up the fix for free — every call site in this module already iterates `ROLE_MODULES` rather than a separately-maintained list, so no other file needed to change. Verified against the live backend: role permission counts changed from "X / 40" to the correct "X / 44" everywhere (confirmed against Super Admin, who holds all 44), and a role created with `identity-administration.view`/`.edit` in its `permissionKeys` round-tripped through the real `RolesController` correctly. Anyone adding a 12th permission module to `PermissionSeedData.cs` in the future must remember to mirror it here too, same as they already must for `config/navigation.ts` — this is a known manual-sync point, not a solved one.

### ADR-010: Masters and Finance & Billing routes/controllers gated with `identity-administration.*`/`finance-billing.*`, following RolesController's pattern — HR/IPD hub landing pages and Documents/Calendar's create-only gating are known, separate gaps left open
**Date:** 2026-08-18
**Status:** Accepted

**Context**
A architecture/product review of `main` found that Finance (`/finance/accounts`) and every Masters reference-data route had no permission gating anywhere — not a `RequirePermissionRoute` on the frontend route, not `[Authorize]`/`[RequirePermission]` on the 19 Masters controllers' mutating actions. Any authenticated hospital user, any role, could reach the invoice ledger or edit master pricing/tax/department data via direct URL or a direct API call, relying solely on the Security Hardening Phase A `FallbackPolicy` (any valid hospital token). HR and IPD had already been fixed the same way Roles was (`RolesController`'s `[Authorize] [RequirePermission("identity-administration.*")]` pattern, `hrRoutes`/`ipdRoutes`' `RequirePermissionRoute` wrapper) — Masters and Finance were the two highest-visibility modules that hadn't been.

**Decision**
1. **Backend:** all 19 Masters controllers (`Suppliers`, `Departments`, `Consultants`, `Products*`, `Warehouses`, etc.) now require `[Authorize] [RequirePermission("identity-administration.<action>")]` on every action — `.create`/`.edit`/`.delete` on mutations, `.view` on both GET actions. This started out mirroring `RolesController`'s original pattern (GETs at the baseline Hospital policy only), but `RolesController` was tightened to gate GETs too while this change was in flight (`0d7e0f5`, "Enforce identity-administration.view on role read endpoints") — Masters was updated to match that same end-to-end standard before landing, rather than merging a pattern that was already superseded. `identity-administration.*` was chosen (over a new `masters.*` permission group) because Settings — the nav leaf Masters lives under — already scopes to it ("Roles & permissions, master data, and system configuration", `config/navigation.ts`), and the keys already exist in `PermissionSeedData.cs`; no new permission catalog rows were needed.
2. **Frontend:** `mastersRoutes` and `financeRoutes` in `routes.tsx` are now wrapped in `RequirePermissionRoute` (`identity-administration.view` / `finance-billing.view`), matching `hrRoutes`/`ipdRoutes`. Finance's landing page (`/finance/accounts`) was pulled out of the ungated `specialPages`/`moduleRoutes` map entirely and into `financeRoutes` itself, since — unlike HR/IPD, whose hub pages are still only reachable through the ungated generic map — Finance's landing page is the invoice ledger the review flagged directly.
3. **Deliberately not touched in this pass:** HR's (`/admin/hr`) and IPD's (`/clinical/ipd`) own hub/landing pages remain reachable through the ungated `moduleRoutes` map the same way Finance's used to be — only their sub-routes are gated. Documents and Calendar still only gate their "Create" button, not the page body (list/preview/download/delete render regardless of permission). Both are real, already-identified gaps, but distinct from the Finance/Masters fix that was asked for here — closing them is follow-up work, not bundled into this change.

**Consequences**
A user without `identity-administration.view`/`finance-billing.view` now gets a real 401/403 from the API and an inline "you don't have permission" screen from the frontend at Masters and Finance, instead of full read/write access by default. Verified end-to-end against a live `hms-api-dev`/`hms-web-dev` pair: `dotnet build`/`dotnet test` (46 architecture tests + 350 unit tests, all green), `tsc --noEmit` clean, and a browser session confirming both the positive path (Super Admin, who holds every permission, still reaches both) and the negative path (permission stripped from the session, both routes render the denied screen instead of content). Anyone picking up the HR/IPD hub-page gap or the Documents/Calendar button-only gap should log it as its own ADR when addressed, per the pattern here.

### ADR-009: Documents module ships as a real backend without full platform-wide RBAC, without existence validation for most owner types, and without a real virus scanner — each gap is enforced-narrow and logged, not silently absent
**Date:** 2026-08-06
**Status:** Accepted

**Context**
The Documents module (see [docs/modules/Documents/DocumentManagement.md](modules/Documents/DocumentManagement.md)) aggregates the most sensitive artifact of every other module in one place — patient consent forms, vendor contracts, billing invoices — behind one generic API. Building it to the same "no authentication yet, actorId: null" standard as every other module's controller (see ADR pattern in PatientsController) would have meant shipping the highest-value attack surface in the system on the weakest foundation. At the same time, standing up full ASP.NET Core policy-based authorization (docs/ApiStandards.md §9), existence-checking all ten owner types, and integrating a real antivirus engine are each platform-wide or external-dependency undertakings disproportionate to one module's first iteration.

**Decision**
Three narrow, explicitly-scoped compromises, each with a stated boundary and a clear seam to close it later:
1. `DocumentsController` is the first controller to require `[Authorize]` on every action and derive a real actor from JWT claims. `DocumentAccessPolicy` enforces a real, in-code role-to-owner-type (plus classification) access table today, rather than the dynamic database-backed policy model described in docs/ApiStandards.md §9 — closing that gap fully is platform-wide work affecting every module's controllers, not something Documents should do unilaterally.
2. Owner-existence validation (US-1) is implemented for Patient only, since it's the only owner type with both a real backend module and code this change could verify end-to-end. The other nine either have no backend at all or were left unwired rather than guessed at. An unregistered owner type is accepted with a logged warning, not silently treated as validated.
3. The virus-scan pipeline (US-9) — queue, background worker, Pending/Available/Quarantined state machine — is real, but `IVirusScanner`'s registered implementation (`NullVirusScanner`) always reports clean. No ClamAV or equivalent is available in this environment. This is stated in code comments, in the module doc's Risks section, and here, rather than left to be discovered later.

**Consequences**
Documents can be exercised and reviewed end-to-end today without waiting on platform-wide RBAC or an external AV dependency. Each of the three gaps has a single, well-defined swap-in point (`DocumentAccessPolicy`, `IDocumentOwnerExistenceChecker` per owner type, `IVirusScanner`) rather than being scattered through the module — closing any one of them does not require touching `DocumentService` or `DocumentsController`. Anyone deploying this module against real patient data must treat all three as open items, not completed work, until closed.

### ADR-008: Patient Edit form is scoped to demographic/contact/medical fields only — encounter and billing are not editable there
**Date:** 2026-08-01
**Status:** Accepted

**Context**
`PatientEditForm.tsx` referenced "the MVP-scope ADR" in a code comment without one actually existing in this log — a dangling reference. The underlying scope decision itself is real and was reaffirmed while reviewing the registration wizard: `PatientRegistrationForm.tsx` collects four things a returning edit shouldn't casually touch — Registration Details (encounter type, department, consultant, admission type) and Billing (charges, discounts, payment status) both have consequences beyond the patient record itself (an encounter is tied to a specific visit; a bill is tied to a specific `Billing`/`BillingItem` record created once at registration — see `frontend/web/src/features/billing/`). Folding either into a general-purpose "edit patient" form invites silent, hard-to-audit changes to data that other flows depend on.

**Decision**
`PatientEditForm` stays limited to Patient Information, Contact Information, and Medical Information — the same three tabs it has today. Registration Details and Billing are not exposed there, now or as part of any near-term follow-up; changes to either belong in their own dedicated flows (e.g., a future "amend registration" or "adjust billing" action scoped and audited on its own terms), not bolted onto patient demographic edits.

**Consequences**
`PatientEditForm` intentionally has no draft-persistence or Billing step, unlike `PatientRegistrationForm` — that asymmetry is by design, not a gap to close. Anyone tempted to add a fifth tab to the edit form should treat that as a new decision requiring its own review, not a natural extension of this one.

### ADR-007: Frontend HTTP client must not swallow `AbortError`, and queries use `networkMode: 'always'`
**Date:** 2026-07-23
**Status:** Accepted

**Context**
While smoke-testing the Users web feature against a live dev server, a query against a deliberately-unavailable backend got stuck showing a loading state indefinitely instead of settling into an error. Investigation traced it to two compounding issues in `frontend/shared/api-client/httpClient.ts` and the default React Query configuration: (1) the HTTP client wrapped every `fetch` rejection — including a deliberate `AbortError` from query cancellation — into a generic `NetworkError`, which defeats React Query's ability to distinguish "cancelled on purpose" from "really failed"; (2) the default `networkMode: 'online'` can pause retries based on perceived connectivity rather than surfacing a prompt error.

**Decision**
`HttpClient.request()` now rethrows `AbortError` unchanged instead of wrapping it. `queryClient`'s default `networkMode` is set to `'always'` for both queries and mutations, so a failed request always settles into a catchable error rather than an indefinite paused/loading state.

**Consequences**
Network failures now reliably reach the UI's error-handling path (docs/FrontendArchitecture.md §8) instead of occasionally hanging. Both web and mobile `queryClient.ts` carry this configuration; any future module reusing the shared HTTP client gets the fix for free.

### ADR-006: Users module layers are `internal`, with `InternalsVisibleTo` granted only to the unit test project
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The approved backend architecture states that only a module's `Contracts` namespace is public; `Domain`/`Application`/`Infrastructure` should be `internal` so other modules and the host can't accidentally reach into them. The first implementation pass left everything `public` for expediency.

**Decision**
`User`, `IUserService`/`UserService`, `IUserRepository`/`UserRepository`, `UserErrorCodes`, `UserMappingExtensions`, and both validators are `internal`. `IdentityDbContext` and `UserConfiguration` remain `public` (Database.Migrations legitimately needs them — the one sanctioned cross-project exception the architecture already documents). `HMS.Modules.Identity` grants `InternalsVisibleTo("HMS.UnitTests")` so unit tests can exercise internals directly; no other assembly gets this grant. `HMS.ArchitectureTests` enforces this boundary going forward via NetArchTest rules, since reflection can inspect internal types without needing the grant.

**Consequences**
Every future module should follow the same pattern: internal by default, `Contracts` public, `InternalsVisibleTo` scoped only to that module's own unit test project, and an architecture test asserting it.

### ADR-005: EF Core migration for `identity.users` is hand-authored; `Designer.cs`/`ModelSnapshot.cs` are intentionally omitted
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The .NET SDK is not installed in the environment this module was built in, so `dotnet ef migrations add` could not be run. The migration's `Up()`/`Down()` methods are ordinary, reviewable C# and were hand-written to match `UserConfiguration` exactly. The `.Designer.cs` and `ModelSnapshot.cs` files, however, are tool-generated, reflection-driven, and must be byte-for-byte consistent with the live model — a hand-written approximation risks being subtly wrong in a way that silently corrupts the *next* migration's diff.

**Decision**
Ship the hand-authored `Up()`/`Down()` migration as a reviewable reference, but do not fabricate the Designer/Snapshot files. A README next to the migration documents the exact `dotnet ef migrations add` command to run (and diff against) once the SDK is available.

**Consequences**
The migration cannot be verified to actually apply until someone runs it against a real PostgreSQL instance with the real tooling. This is a known gap, not a silent one — see the Phase 10 quality checklist in the Users module delivery notes.

### ADR-004: Controller-based endpoints (not Minimal API) for module HTTP surfaces
**Date:** 2026-07-23
**Status:** Accepted

**Context**
The backend architecture document left the `Endpoints/` layer's implementation mechanism open ("Minimal API or Controllers"). Building the first reference module forced a concrete choice.

**Decision**
Use ASP.NET Core MVC controllers (`[ApiController]`, `ControllerBase`). Each module's controller lives in its own `Endpoints/` folder but requires a `FrameworkReference` to `Microsoft.AspNetCore.App` since modules are plain class libraries, not Web SDK projects.

**Consequences**
This is now the precedent every future module follows for consistency. Controllers were chosen over Minimal API for familiarity (attribute routing, model binding, filters are well-understood by a team with mixed experience levels) — not because Minimal API is unsuitable; this could be revisited if a future module's needs push toward Minimal API instead, but the choice should stay uniform across modules rather than mixed.

### ADR-003: Manual DTO↔entity mapping instead of AutoMapper/Mapster
**Date:** 2026-07-23
**Status:** Accepted

**Context**
docs/Architecture.md §7 recommended Mapster (or AutoMapper as an alternative) for Application↔Contracts mapping. With a single entity in the first module, either library is pure overhead.

**Decision**
Use a small static extension method (`UserMappingExtensions.ToResponse()`) instead of adding a mapping library dependency.

**Consequences**
Revisit once a module has enough DTOs/entities that hand-written mapping becomes repetitive boilerplate rather than a one-line method — likely by the third or fourth module. Until then, every module should follow this same manual-mapping pattern rather than introducing a library independently.

### ADR-002: Central Package Management (`Directory.Packages.props`) adopted for the whole backend
**Date:** 2026-07-22
**Status:** Accepted

**Context**
Building the first module required adding several new NuGet packages (EF Core, Npgsql, FluentValidation, testing libraries) across multiple projects.

**Decision**
Every package version is declared once in `Directory.Packages.props`; individual `.csproj` files reference packages by name only.

**Consequences**
Adding a package to a new module means adding/reusing a `<PackageVersion>` entry at the root and a bare `<PackageReference Include="..." />` in the project — no version number is ever repeated. Keeps every module on the same package versions by construction.

### ADR-001: The `User` entity carries no credential/authentication fields
**Date:** 2026-07-22
**Status:** Accepted

**Context**
The Users module was explicitly scoped to exclude authentication, JWT, login, and refresh tokens for this iteration (see docs/modules/Identity/Users.md).

**Decision**
`identity.users` (and the `User` domain entity) has no password hash, credential, or session-related column. Only profile data (name, email, phone) and the standard audit columns exist.

**Consequences**
When the Authentication iteration begins, credential fields are added to this same table as purely additive columns — no redesign of the existing schema, entity, or API contracts is expected. If that assumption turns out to be wrong (e.g., credentials need a separate table for security-isolation reasons), record that as a new ADR when it happens.
