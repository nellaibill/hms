import fs from 'node:fs';
import { test as base } from '@playwright/test';
import { AUTH_STATE_FILE } from '../support/env';
import { DashboardPage } from '../pages/DashboardPage';

const SESSION_KEY = 'hms-session';
const RESTORED_MARKER_KEY = 'e2e-session-restored';

/**
 * Test fixture for specs that need a signed-in user but aren't testing sign-in itself.
 *
 * The app keeps its session in sessionStorage (features/auth/AuthContext.tsx), which
 * Playwright's built-in storageState does not capture — so auth.setup.ts saves that one
 * value and this fixture puts it back before any page script runs. It restores only once
 * per tab (tracked by a marker key), so after an in-app logout a reload or direct visit to a
 * protected URL really is signed out — which logout.spec.ts relies on.
 */
export const test = base.extend<{ dashboardPage: DashboardPage }>({
  page: async ({ page }, use) => {
    if (!fs.existsSync(AUTH_STATE_FILE)) {
      throw new Error(`No saved session at ${AUTH_STATE_FILE} — run through the 'authenticated' project so auth.setup.ts runs first.`);
    }
    const session = fs.readFileSync(AUTH_STATE_FILE, 'utf8');
    await page.addInitScript(
      ([key, value, marker]) => {
        if (window.sessionStorage.getItem(marker)) return;
        window.sessionStorage.setItem(key, value);
        window.sessionStorage.setItem(marker, '1');
      },
      [SESSION_KEY, session, RESTORED_MARKER_KEY] as const,
    );
    await use(page);
  },
  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page));
  },
});

export { expect } from '@playwright/test';
