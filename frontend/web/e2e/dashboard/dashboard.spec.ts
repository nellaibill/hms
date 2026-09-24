import { expect, test } from '../fixtures/auth.fixture';

function readSession(page: import('@playwright/test').Page) {
  return page.evaluate(() => JSON.parse(sessionStorage.getItem('hms-session') ?? 'null'));
}

test.describe('Dashboard', () => {
  test.beforeEach(async ({ dashboardPage }) => {
    await dashboardPage.navigate();
  });

  test('loads at /dashboard with the Executive Dashboard banner', async ({ page, dashboardPage }) => {
    await dashboardPage.verifyLoaded();
    await expect(page.getByText('Live overview across every department, updated in real time.')).toBeVisible();
  });

  test('redirects the site root to the dashboard', async ({ page, dashboardPage }) => {
    await page.goto('/');
    await dashboardPage.verifyLoaded();
  });

  test('shows the app shell: header, sidebar and header menus', async ({ page, dashboardPage }) => {
    await dashboardPage.verifyLoaded();
    await expect(page.getByRole('banner')).toBeVisible();
    await expect(dashboardPage.sidebarNav).toBeVisible();
    await expect(dashboardPage.navLink('Dashboard')).toHaveAttribute('aria-current', 'page');
    await expect(dashboardPage.profileMenuButton).toBeVisible();
    await expect(page.getByRole('button', { name: 'Notifications' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pending Tasks' })).toBeVisible();
  });

  test('profile menu identifies the signed-in user', async ({ page, dashboardPage }) => {
    const session = await readSession(page);
    await dashboardPage.openProfileMenu();
    const menu = page.getByRole('menu');
    await expect(menu).toContainText(session.user.name);
    await expect(dashboardPage.logoutMenuItem).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(menu).toBeHidden();
  });

  test('shows the sections every user sees', async ({ dashboardPage }) => {
    await expect(dashboardPage.sectionHeading('Calendar – Notifications and Events')).toBeVisible();
    await expect(dashboardPage.sectionHeading('Plans and Projects – Status')).toBeVisible();
  });

  test('shows exactly the permission-gated sections this user is entitled to', async ({ page, dashboardPage }) => {
    await dashboardPage.verifyLoaded();
    const permissions: string[] = (await readSession(page))?.user?.permissionKeys ?? [];
    // Mirrors DashboardPage.tsx: each section renders exactly when its permission is held.
    const gated: Array<[string, string]> = [
      ['Statistical Data', 'patient-management.view'],
      ['Department-wise Income & Expenses', 'finance-billing.view'],
      ['Month-wise Income & Expense', 'finance-billing.view'],
      ['Present HR', 'workforce-admin.view'],
    ];
    for (const [title, permission] of gated) {
      await expect(dashboardPage.sectionHeading(title), `"${title}" should follow ${permission}`).toHaveCount(
        permissions.includes(permission) ? 1 : 0,
      );
    }
  });

  test('notifications card loads its unread count', async ({ page }) => {
    await expect(page.getByText(/^\d+ unread$/)).toBeVisible();
    await expect(page.getByText('Loading notifications…')).toHaveCount(0);
  });

  test('loads without uncaught page errors', async ({ page, dashboardPage }) => {
    const errors: Error[] = [];
    page.on('pageerror', (error) => errors.push(error));
    await page.reload();
    await dashboardPage.verifyLoaded();
    await expect(dashboardPage.sectionHeading('Plans and Projects – Status')).toBeVisible();
    expect(errors, errors.map((e) => e.message).join('\n')).toEqual([]);
  });
});
