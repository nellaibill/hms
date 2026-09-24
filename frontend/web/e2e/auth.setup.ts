import fs from 'node:fs';
import path from 'node:path';
import { expect, test as setup } from '@playwright/test';
import { AUTH_STATE_FILE, getTestCredentials } from './support/env';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

// One real sign-in per run, shared by every spec in the 'authenticated' project — keeps the
// suite well under the API's 10-logins-per-minute limit.
setup('sign in and save the hospital session', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();
  await loginPage.login(getTestCredentials());

  const dashboard = new DashboardPage(page);
  await dashboard.verifyLoaded();

  const session = await page.evaluate(() => window.sessionStorage.getItem('hms-session'));
  expect(session, 'sessionStorage["hms-session"] should be set after sign-in').toBeTruthy();

  fs.mkdirSync(path.dirname(AUTH_STATE_FILE), { recursive: true });
  fs.writeFileSync(AUTH_STATE_FILE, session!);
});
