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

  test('ignores an expired stored session', async ({ page }) => {
    await page.addInitScript(() => {
      window.sessionStorage.setItem(
        'hms-session',
        JSON.stringify({ token: 'expired-token', expiresAt: Date.now() - 60_000, user: { name: 'Expired User' } }),
      );
    });
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
    expect(await page.evaluate(() => sessionStorage.getItem('hms-session'))).toBeNull();
  });
});
