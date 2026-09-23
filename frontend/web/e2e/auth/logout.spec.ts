import { expect, test } from '../fixtures/auth.fixture';
import { LoginPage } from '../pages/LoginPage';

// Starts from the session saved by auth.setup.ts (a real sign-in earlier in the same run);
// this spec's subject is signing *out*, so it doesn't spend another rate-limited login.

test.describe('Logout', () => {
  test.beforeEach(async ({ dashboardPage }) => {
    await dashboardPage.navigate();
    await dashboardPage.verifyLoaded();
  });

  test('logs out from the profile menu and returns to the login page', async ({ page, dashboardPage }) => {
    await dashboardPage.openProfileMenu();
    await expect(dashboardPage.logoutMenuItem).toBeVisible();
    await dashboardPage.logoutMenuItem.click();

    const loginPage = new LoginPage(page);
    await expect(page).toHaveURL(/\/login$/);
    await expect(loginPage.heading).toBeVisible();
    await expect(dashboardPage.heading).toHaveCount(0);
    await expect(dashboardPage.sidebarNav).toHaveCount(0);
    // A normal logout is not a session expiry — no error banner.
    await expect(loginPage.errorAlert).toHaveCount(0);

    // The client-side session is really gone, not just hidden.
    expect(await page.evaluate(() => sessionStorage.getItem('hms-session'))).toBeNull();
  });

  test('blocks the dashboard after logout, including on reload and Back', async ({ page, dashboardPage }) => {
    await dashboardPage.logout();
    await expect(page).toHaveURL(/\/login$/);

    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
    await expect(dashboardPage.heading).toHaveCount(0);

    await page.reload();
    await expect(page).toHaveURL(/\/login$/);

    await page.goBack();
    await expect(page).toHaveURL(/\/login$/);
    await expect(dashboardPage.heading).toHaveCount(0);
  });

  test('blocks other protected routes after logout', async ({ page, dashboardPage }) => {
    await dashboardPage.logout();
    await expect(page).toHaveURL(/\/login$/);

    for (const path of ['/patients/enquiry', '/admin/settings', '/']) {
      await page.goto(path);
      await expect(page, `${path} should redirect to /login`).toHaveURL(/\/login$/);
    }
  });
});
