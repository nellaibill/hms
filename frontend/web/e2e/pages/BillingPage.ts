import { expect, type Locator, type Page } from '@playwright/test';

export const ACCOUNTS_TABS = [
  { label: 'Recent Bills', path: '/finance/accounts', heading: 'Accounts and Finance' },
  { label: 'All Invoices', path: '/finance/accounts/invoices', heading: 'Accounts and Finance' },
  { label: 'Income & Expense', path: '/finance/accounts/reports', heading: 'Income & Expense Report' },
  { label: 'Hospital Profit', path: '/finance/accounts/reports/profit', heading: 'Hospital Profit Report' },
  { label: 'Laboratory', path: '/finance/accounts/reports/laboratory', heading: 'Laboratory Report' },
  { label: 'Radiology', path: '/finance/accounts/reports/radiology', heading: 'Radiology Report' },
  { label: 'Consultant', path: '/finance/accounts/reports/consultant', heading: 'Consultant Profit Report' },
] as const;

/** OPD Billing Entry at /finance/accounts/new: pick a patient, then bill their visit. */
export class InvoiceCreatePage {
  readonly heading: Locator;
  readonly collectPaymentButton: Locator;
  readonly consultationType: Locator;
  readonly consultationCharge: Locator;
  readonly paymentModes: Locator;
  readonly paymentAmounts: Locator;
  readonly addPaymentMethodButton: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1, name: 'OPD Billing Entry' });
    this.collectPaymentButton = page.getByRole('button', { name: 'Collect Payment' });
    this.consultationType = page.getByRole('button', { name: 'Consultation type', exact: true });
    this.consultationCharge = page.getByLabel('Consultation charge (₹)');
    this.paymentModes = page.getByRole('combobox', { name: 'Payment Mode' });
    this.paymentAmounts = page.getByLabel('Amount (₹)');
    this.addPaymentMethodButton = page.getByRole('button', { name: 'Add another payment method' });
  }

  async navigate() {
    await this.page.goto('/finance/accounts/new');
    await expect(this.heading).toBeVisible();
  }

  /** Finds the patient by UHID in the picker and selects their latest visit. */
  async selectPatient(uhid: string) {
    await this.page.getByLabel('UHID').fill(uhid);
    await this.page.getByRole('button', { name: 'Search' }).click();
    await this.page.getByRole('row', { name: new RegExp(uhid) }).getByRole('button', { name: 'Select' }).click();
    await expect(this.page.getByRole('button', { name: 'Change patient' })).toBeVisible();
    await expect(this.page.getByRole('main')).toContainText(`· ${uhid}`);
  }

  /** Searchable pop-over picker inside the billing cards (Department, Consultant, Consultation type). */
  async pick(buttonName: string, option?: string) {
    await this.page.getByRole('button', { name: buttonName, exact: true }).click();
    const popover = this.page.getByRole('dialog');
    await (option ? popover.getByRole('option', { name: option, exact: true }) : popover.getByRole('option').first()).click();
    await expect(popover).toBeHidden();
  }

  async setPayment(index: number, mode: 'Cash' | 'Card' | 'UPI' | 'Bank Transfer', amount: number) {
    await this.paymentModes.nth(index).click();
    await this.page.getByRole('option', { name: mode, exact: true }).click();
    await this.paymentAmounts.nth(index).fill(String(amount));
  }

  /** The Billing Summary card's text (line items, totals, Net Payable). */
  get summary(): Locator {
    return this.page.getByRole('heading', { name: 'Billing Summary' }).locator('xpath=ancestor::*[.//button[normalize-space()="Collect Payment"]][1]');
  }
}
