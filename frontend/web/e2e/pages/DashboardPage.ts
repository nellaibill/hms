import { expect, type Locator, type Page } from '@playwright/test';

/** The authenticated app shell (header + sidebar) with the Executive Dashboard at /dashboard. */
export class DashboardPage {
  readonly heading: Locator;
  readonly sidebarNav: Locator;
  readonly profileMenuButton: Locator;
  readonly logoutMenuItem: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1, name: 'Executive Dashboard' });
    this.sidebarNav = page.getByRole('navigation');
    this.profileMenuButton = page.getByRole('button', { name: 'User Login / Profile Details' });
    this.logoutMenuItem = page.getByRole('menuitem', { name: 'Log out' });
  }

  async navigate() {
    await this.page.goto('/dashboard');
  }

  /** Waits until the dashboard route and its banner heading are showing. */
  async verifyLoaded() {
    await expect(this.page).toHaveURL(/\/dashboard$/);
    await expect(this.heading).toBeVisible();
  }

  sectionHeading(title: string): Locator {
    return this.page.getByRole('heading', { level: 2, name: title });
  }

  navLink(label: string): Locator {
    return this.sidebarNav.getByRole('link', { name: label, exact: true });
  }

  async openProfileMenu() {
    await this.profileMenuButton.click();
  }

  async logout() {
    await this.openProfileMenu();
    await this.logoutMenuItem.click();
  }
}
