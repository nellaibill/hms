# HMS web — Playwright E2E regression suite

Browser-level regression tests for the hospital web app (`frontend/web`), using
[Playwright Test](https://playwright.dev) (Chromium). Milestone 1 covers
**login → dashboard → basic navigation → logout** only.

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

Start the API (`hms-api-dev`) and the web app (`npm run dev`) first, then:

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
  pages/                   LoginPage, DashboardPage page objects
  support/env.ts           reads TEST_* variables
  auth/
    login.spec.ts          sign-in form, validation, invalid + valid login (always a real login)
    unauthenticated.spec.ts  protected routes redirect to /login when signed out
    logout.spec.ts         log out, then prove the session is gone
  dashboard/
    dashboard.spec.ts      dashboard content and app shell
    navigation.spec.ts     sidebar → module pages
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
- **Selectors.** Use roles and labels first (`getByRole`, `getByLabel`). The app has no
  `data-testid`s, and so far none have been needed.
