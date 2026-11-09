import { expect, test } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';

// A fresh browser context that has never signed in — no saved session restored.

test.describe('Unauthenticated access', () => {
  for (const path of ['/dashboard', '/', '/patients/enquiry', '/admin/settings']) {
    test(`redirects ${path} to the login page`, async ({ page }) => {
      await page.goto(path);
      await expect(page).toHaveURL(/\/login$/);
      await expect(new LoginPage(page).heading).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Executive Dashboard' })).toHaveCount(0);
      await expect(page.getByRole('navigation')).toHaveCount(0);
    });
  }

  // Sessions have no time limit (ADR-131), so a stored session is only dropped once the API
  // rejects its token — the first 401 signs the user out.
  test('signs out a stored session the API rejects', async ({ page }) => {
    await page.addInitScript(() => {
      window.sessionStorage.setItem(
        'hms-session',
        JSON.stringify({
          token: 'invalid-token',
          user: {
            id: '00000000-0000-0000-0000-000000000000',
            name: 'Stale User',
            role: 'admin',
            username: 'stale.user',
            email: 'stale@example.com',
            roleName: 'Admin',
            permissionKeys: [],
            featureKeys: [],
            hiddenMenus: [],
            mustChangePassword: false,
          },
        }),
      );
    });
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
    expect(await page.evaluate(() => sessionStorage.getItem('hms-session'))).toBeNull();
  });
});
