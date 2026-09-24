import { expect, test } from '../fixtures/auth.fixture';

// Sidebar leaves backed by a real page (not the shared PlaceholderPage), with the banner
// heading each renders. Source of truth: src/config/navigation.ts + each page component.
const destinations = [
  { label: 'Patient Enquiry', path: '/patients/enquiry', heading: 'Old Patient Registration' },
  { label: 'Out Patient Department (OPD)', path: '/clinical/opd', heading: 'Out Patient Department (OPD)' },
  { label: 'In Patient Department (IPD)', path: '/clinical/ipd', heading: 'In Patient Department (IPD)' },
  { label: 'Pharmacy', path: '/pharmacy', heading: 'Pharmacy' },
  { label: 'Central Laboratory', path: '/diagnostics/lab', heading: 'Central Laboratory' },
  { label: 'Accounts and Finance', path: '/finance/accounts', heading: 'Accounts and Finance' },
  { label: 'Human Resource Management (HR)', path: '/admin/hr', heading: 'Human Resource Management (HR)' },
  { label: 'Settings', path: '/admin/settings', heading: 'Settings' },
];

const escapeRegExp = (value: string) => value.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&');

test.describe('Sidebar navigation from the dashboard', () => {
  test.beforeEach(async ({ dashboardPage }) => {
    await dashboardPage.navigate();
    await dashboardPage.verifyLoaded();
  });

  for (const { label, path, heading } of destinations) {
    test(`${label} → ${path}`, async ({ page, dashboardPage }) => {
      const link = dashboardPage.navLink(label);
      // Leaves are hidden when the tenant lacks the module or the user lacks the permission.
      test.skip((await link.count()) === 0, `"${label}" is not in this user's sidebar (feature/permission not granted)`);

      await link.click();

      await expect(page).toHaveURL(new RegExp(`${escapeRegExp(path)}$`));
      await expect(page.getByRole('heading', { level: 1, name: heading, exact: true })).toBeVisible();
      await expect(link).toHaveAttribute('aria-current', 'page');
      await expect(dashboardPage.navLink('Dashboard')).not.toHaveAttribute('aria-current', 'page');
    });
  }

  test('returns to the dashboard from another module', async ({ page, dashboardPage }) => {
    await dashboardPage.navLink('Settings').click();
    await expect(page).toHaveURL(/\/admin\/settings$/);

    await dashboardPage.navLink('Dashboard').click();
    await dashboardPage.verifyLoaded();
  });

  test('keeps the session across a full reload of a module page', async ({ page, dashboardPage }) => {
    await dashboardPage.navLink('Settings').click();
    await expect(page.getByRole('heading', { level: 1, name: 'Settings' })).toBeVisible();
    await page.reload();
    await expect(page).toHaveURL(/\/admin\/settings$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Settings' })).toBeVisible();
  });
});
