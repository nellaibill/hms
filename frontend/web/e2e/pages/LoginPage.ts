import type { Locator, Page } from '@playwright/test';
import type { TestCredentials } from '../support/env';

/** The hospital sign-in page at /login (src/pages/auth/LoginPage.tsx). */
export class LoginPage {
  readonly heading: Locator;
  readonly hospitalCodeInput: Locator;
  readonly roleSelect: Locator;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly signInButton: Locator;
  /** The form's single error slot — client-side validation and API failures both render here. */
  readonly errorAlert: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Sign in' });
    this.hospitalCodeInput = page.getByLabel('Hospital code');
    this.roleSelect = page.getByRole('combobox', { name: 'Sign in as' });
    this.usernameInput = page.getByLabel('Username');
    this.passwordInput = page.getByLabel('Password');
    // Matches both the idle "Sign in" and in-flight "Signing in…" labels.
    this.signInButton = page.getByRole('button', { name: /^sign(ing)? in/i });
    this.errorAlert = page.getByRole('alert');
  }

  async navigate() {
    await this.page.goto('/login');
  }

  async enterHospitalCode(hospitalCode: string) {
    await this.hospitalCodeInput.fill(hospitalCode);
  }

  async selectRole(roleLabel: string) {
    await this.roleSelect.click();
    await this.page.getByRole('option', { name: roleLabel, exact: true }).click();
  }

  async enterUsername(username: string) {
    await this.usernameInput.fill(username);
  }

  async enterPassword(password: string) {
    await this.passwordInput.fill(password);
  }

  async clickLogin() {
    await this.signInButton.click();
  }

  /** Fills every field and submits. Does not wait for or assert the outcome. */
  async login({ hospitalCode, role, username, password }: TestCredentials) {
    await this.enterHospitalCode(hospitalCode);
    await this.selectRole(role);
    await this.enterUsername(username);
    await this.enterPassword(password);
    await this.clickLogin();
  }
}
