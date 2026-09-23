import { expect, test, type Page } from '@playwright/test';
import { getTestCredentials, LOGIN_API_PATH } from '../support/env';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';

// Every test here exercises the real sign-in form from a fresh, signed-out browser context —
// no saved session is reused.

const VALIDATION_MESSAGE = 'Enter your hospital code, username, and password to continue.';
const INVALID_LOGIN_MESSAGE = 'Invalid username or password.';

/** Counts POSTs to the login API so validation tests can prove nothing was sent. */
function trackLoginRequests(page: Page) {
  const calls: string[] = [];
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().includes(LOGIN_API_PATH)) calls.push(request.url());
  });
  return calls;
}

/**
 * A username no seeded account uses. Invalid-login tests deliberately never send a wrong
 * password for the real test account: the backend locks an account out after repeated
 * failures (User.FailedLoginAttempts), which would break every other test.
 */
function unknownUsername() {
  return `e2e-no-such-user-${Date.now()}`;
}

test.describe('Login page', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigate();
  });

  test('loads with every sign-in control', async ({ page }) => {
    await expect(page).toHaveURL(/\/login$/);
    await expect(loginPage.heading).toBeVisible();
    await expect(loginPage.hospitalCodeInput).toBeVisible();
    await expect(loginPage.hospitalCodeInput).toBeEditable();
    await expect(loginPage.roleSelect).toBeVisible();
    await expect(loginPage.roleSelect).toHaveText(/Super Admin/);
    await expect(loginPage.usernameInput).toBeEditable();
    await expect(loginPage.passwordInput).toBeEditable();
    await expect(loginPage.signInButton).toHaveText('Sign in');
    await expect(loginPage.signInButton).toBeEnabled();
    await expect(loginPage.errorAlert).toHaveCount(0);
  });

  test('masks the password field', async () => {
    await expect(loginPage.passwordInput).toHaveAttribute('type', 'password');
    await loginPage.enterPassword('typed-secret');
    await expect(loginPage.passwordInput).toHaveValue('typed-secret');
    // Still masked after input — the value is never rendered as plain text.
    await expect(loginPage.passwordInput).toHaveAttribute('type', 'password');
  });

  test('lists every role in the "Sign in as" dropdown', async ({ page }) => {
    await loginPage.roleSelect.click();
    const options = page.getByRole('option');
    await expect(options).toHaveText([
      'Super Admin',
      'Hospital Administrator',
      'Receptionist',
      'Doctor / Consultant',
      'Nurse',
      'Lab Technician',
      'Radiologist',
      'Pharmacist',
      'HR Officer',
      'Accounts Officer',
    ]);
    await page.getByRole('option', { name: 'Nurse' }).click();
    await expect(loginPage.roleSelect).toHaveText(/Nurse/);
  });

  test.describe('client-side validation', () => {
    test('rejects an empty username without calling the API', async ({ page }) => {
      const loginCalls = trackLoginRequests(page);
      await loginPage.enterHospitalCode('any-hospital');
      await loginPage.enterPassword('any-password');
      await loginPage.clickLogin();

      await expect(loginPage.errorAlert).toHaveText(VALIDATION_MESSAGE);
      await expect(page).toHaveURL(/\/login$/);
      expect(loginCalls).toHaveLength(0);
    });

    test('rejects an empty password without calling the API', async ({ page }) => {
      const loginCalls = trackLoginRequests(page);
      await loginPage.enterHospitalCode('any-hospital');
      await loginPage.enterUsername('any-user');
      await loginPage.clickLogin();

      await expect(loginPage.errorAlert).toHaveText(VALIDATION_MESSAGE);
      await expect(page).toHaveURL(/\/login$/);
      expect(loginCalls).toHaveLength(0);
    });

    test('rejects an empty hospital code without calling the API', async ({ page }) => {
      const loginCalls = trackLoginRequests(page);
      await loginPage.enterUsername('any-user');
      await loginPage.enterPassword('any-password');
      await loginPage.clickLogin();

      await expect(loginPage.errorAlert).toHaveText(VALIDATION_MESSAGE);
      expect(loginCalls).toHaveLength(0);
    });

    test('rejects a completely empty form without calling the API', async ({ page }) => {
      const loginCalls = trackLoginRequests(page);
      await loginPage.clickLogin();

      await expect(loginPage.errorAlert).toHaveText(VALIDATION_MESSAGE);
      await expect(page).toHaveURL(/\/login$/);
      expect(loginCalls).toHaveLength(0);
    });

    test('treats whitespace-only fields as empty', async ({ page }) => {
      const loginCalls = trackLoginRequests(page);
      await loginPage.enterHospitalCode('   ');
      await loginPage.enterUsername('   ');
      await loginPage.enterPassword('   ');
      await loginPage.clickLogin();

      await expect(loginPage.errorAlert).toHaveText(VALIDATION_MESSAGE);
      expect(loginCalls).toHaveLength(0);
    });
  });

  test.describe('against the API', () => {
    test('rejects an unknown username and stays signed out', async ({ page }) => {
      const { hospitalCode } = getTestCredentials();
      await loginPage.enterHospitalCode(hospitalCode);
      await loginPage.enterUsername(unknownUsername());
      await loginPage.enterPassword('not-the-password');

      const responsePromise = page.waitForResponse((r) => r.url().includes(LOGIN_API_PATH) && r.request().method() === 'POST');
      await loginPage.clickLogin();
      const response = await responsePromise;

      expect(response.ok()).toBe(false);
      await expect(loginPage.errorAlert).toHaveText(INVALID_LOGIN_MESSAGE);
      await expect(page).toHaveURL(/\/login$/);
      await expect(loginPage.signInButton).toBeEnabled();
      expect(await page.evaluate(() => sessionStorage.getItem('hms-session'))).toBeNull();
    });

    test('rejects an unknown hospital code with the same generic message', async ({ page }) => {
      await loginPage.enterHospitalCode(`e2e-no-such-hospital-${Date.now()}`);
      await loginPage.enterUsername(unknownUsername());
      await loginPage.enterPassword('not-the-password');
      await loginPage.clickLogin();

      // Never reveals whether the hospital code or the credentials were wrong.
      await expect(loginPage.errorAlert).toHaveText(INVALID_LOGIN_MESSAGE);
      await expect(page).toHaveURL(/\/login$/);
      expect(await page.evaluate(() => sessionStorage.getItem('hms-session'))).toBeNull();
    });

    test('disables the button and shows progress while signing in', async ({ page }) => {
      const { hospitalCode } = getTestCredentials();
      // Hold the real request until the in-flight state has been checked, then let it
      // through unchanged — no mocked response.
      let releaseRequest!: () => void;
      const released = new Promise<void>((resolve) => (releaseRequest = resolve));
      await page.route(`**${LOGIN_API_PATH}`, async (route) => {
        await released;
        await route.continue();
      });

      await loginPage.enterHospitalCode(hospitalCode);
      await loginPage.enterUsername(unknownUsername());
      await loginPage.enterPassword('not-the-password');
      await loginPage.clickLogin();

      await expect(loginPage.signInButton).toBeDisabled();
      await expect(loginPage.signInButton).toHaveText('Signing in…');

      releaseRequest();
      await expect(loginPage.errorAlert).toHaveText(INVALID_LOGIN_MESSAGE);
      await expect(loginPage.signInButton).toBeEnabled();
      await expect(loginPage.signInButton).toHaveText('Sign in');
    });

    test('signs in with valid credentials and lands on the dashboard', async ({ page }) => {
      const credentials = getTestCredentials();
      const responsePromise = page.waitForResponse((r) => r.url().includes(LOGIN_API_PATH) && r.request().method() === 'POST');
      await loginPage.login(credentials);
      expect((await responsePromise).status()).toBe(200);

      const dashboard = new DashboardPage(page);
      await dashboard.verifyLoaded();
      await expect(dashboard.sidebarNav).toBeVisible();

      // Authenticated for real: a session token is stored and the profile menu names this user's role.
      const session = await page.evaluate(() => JSON.parse(sessionStorage.getItem('hms-session') ?? 'null'));
      expect(session?.token).toBeTruthy();
      expect(session?.user?.username?.toLowerCase()).toBe(credentials.username.toLowerCase());
      await dashboard.openProfileMenu();
      await expect(page.getByRole('menu')).toContainText(credentials.role);
    });

    test('returns to the originally requested page after signing in', async ({ page }) => {
      await page.goto('/admin/settings');
      await expect(page).toHaveURL(/\/login$/);

      await loginPage.login(getTestCredentials());
      await expect(page).toHaveURL(/\/admin\/settings$/);
      await expect(page.getByRole('heading', { level: 1, name: 'Settings' })).toBeVisible();
    });
  });
});
