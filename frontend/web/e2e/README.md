# HMS web — Playwright E2E regression suite

Browser-level regression tests for the hospital web app (`frontend/web`), using
[Playwright Test](https://playwright.dev) (Chromium). Covered so far:
**login → dashboard → basic navigation → logout**, **patient registration**, **OPD**
(work-list + consultation), and **billing** (OPD Billing Entry, invoices, reports).

## One-time setup

```bash
cd frontend/web
npm install
npx playwright install chromium
cp .env.e2e.example .env.e2e   # then fill it in, see below
```

`.env.e2e` is git-ignored. Fill it with a **dedicated, non-production test account**:

| Variable             | Meaning                                                       |
| -------------------- | ------------------------------------------------------------- |
| `TEST_BASE_URL`      | Where the web app runs (default `http://localhost:5173`)      |
| `TEST_HOSPITAL_CODE` | Tenant code typed into "Hospital code"                        |
| `TEST_ROLE`          | "Sign in as" option label, e.g. `Super Admin` (the default)   |
| `TEST_USERNAME`      | Test user's username                                          |
| `TEST_PASSWORD`      | Test user's password                                          |

Use a Super Admin in a tenant with all modules enabled so every sidebar destination is
covered. Navigation tests for modules the user can't see are **skipped** with a reason, not
passed. The account must not have "must change password" pending. In CI, set these as
secrets or environment variables instead of using a file.

## Running

Start the API (`hms-api-dev`) first. The web app is started automatically if it isn't already
running (Playwright `webServer`). Then:

```bash
npm run test:e2e            # headless, every project
npm run test:e2e:headed     # watch it in a real browser window
npm run test:e2e:ui         # Playwright UI mode (time-travel debugging)
npm run test:e2e:report     # open the last HTML report
npx playwright test --debug # step through with the inspector
npx playwright test e2e/auth/login.spec.ts   # a single file
```

Failures keep a screenshot and a video, and a trace on the retry, all linked from the HTML
report (`playwright-report/`). Open a trace with `npx playwright show-trace <zip>`.

## Layout

```
e2e/
  auth.setup.ts            one real sign-in per run; saves the session
  fixtures/auth.fixture.ts restores that session for authenticated specs
  pages/                   page objects: LoginPage, DashboardPage, PatientRegistrationPage,
                           OpdPage, OpdConsultationPage, BillingPage
  support/env.ts           reads TEST_* variables
  support/testData.ts      unique test records (names start with "Etoe")
  auth/
    login.spec.ts          sign-in form, validation, invalid + valid login (always a real login)
    unauthenticated.spec.ts  protected routes redirect to /login when signed out
    logout.spec.ts         log out, then prove the session is gone
  dashboard/
    dashboard.spec.ts      dashboard content and app shell
    navigation.spec.ts     sidebar → module pages
  patients/
    registration.spec.ts   hub, wizard validation per tab, full register → search → view → edit, Patient Enquiry
  opd/
    opd.spec.ts            OPD filters/tabs/export; new OP visit → Consult → draft → complete → reopen
  billing/
    billing.spec.ts        Accounts tabs/reports/All Invoices; bill a visit (validation, split payment,
                           ledger, detail, consultant report, double-billing guard)
```

Projects in `playwright.config.ts`:

- **signed-out**: `login.spec.ts` and `unauthenticated.spec.ts`. Always a fresh browser,
  never a saved session.
- **setup**: signs in once and writes `e2e/.auth/session.json` (git-ignored).
- **authenticated**: everything else. Depends on `setup`.

## Things to know

- **The session is in `sessionStorage`**, not cookies or localStorage, so Playwright's
  `storageState` can't save it. `auth.fixture.ts` restores `sessionStorage['hms-session']`
  with an init script, once per tab, so a logout inside a test still sticks.
- **Login rate limit.** The API allows 10 `POST /api/v1/auth/login` calls per minute per IP.
  The suite makes about 6 login calls per run and uses one worker. If you re-run it several
  times in a row and see HTTP 429s, wait a minute.
- **Account lockout.** Repeated wrong passwords lock the real account, so invalid-login tests
  only ever use a made-up username or hospital code. Never add a wrong-password test against
  the shared test user.
- **Billing needs a consultant with a consultation type.** Most `lhs` consultants have none, so
  their consultations can't be billed. The billing test uses E.N.T / Dr. C. Ravikumar (see
  `billableConsultant` in `support/testData.ts`). Override it with `TEST_BILLING_DEPARTMENT` and
  `TEST_BILLING_CONSULTANT`.
- **Test data.** The patient, OPD and billing end-to-end tests (4 per run) each create a real patient plus an OP visit in
  whichever tenant `.env.e2e` points at (currently the `lhs` dev tenant) on every run. Names
  start with `Etoe` and have the last name `Regression`, so they're easy to find. The
  backend only allows letters in names, so an "E2E-123" style name isn't possible.
- **Known bugs** are pinned with `test.fail(...)` and a comment explaining the cause. Such a
  test counts as passing while the bug exists. Once someone fixes the bug, Playwright reports
  "expected to fail, but passed", which is your cue to remove the `test.fail` line.
- **Selectors.** Use roles and labels first (`getByRole`, `getByLabel`). The app has no
  `data-testid`s, and so far none have been needed.
