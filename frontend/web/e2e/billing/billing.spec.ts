import { expect, test } from '../fixtures/auth.fixture';
import { ACCOUNTS_TABS, InvoiceCreatePage } from '../pages/BillingPage';
import { PatientRegistrationPage } from '../pages/PatientRegistrationPage';
import { billableConsultant, newPatient } from '../support/testData';

/** "Label ₹1,234" — the page renders label and amount in separate elements, so allow any spacing. */
const money = (label: string, amount: number) => new RegExp(String.raw`${label}\s*₹${amount.toLocaleString('en-IN')}(?![\d,])`);

const isInvoiceCreate = (r: { request(): { method(): string }; url(): string }) =>
  r.request().method() === 'POST' && /\/api\/v1\/billing\/invoices$/.test(r.url());

test.describe('Accounts and Finance', () => {
  test('lands on Recent Patient Bills with the section tabs', async ({ page }) => {
    await page.goto('/finance/accounts');
    await expect(page.getByRole('heading', { level: 1, name: 'Accounts and Finance' })).toBeVisible();
    for (const { label } of ACCOUNTS_TABS) {
      await expect(page.getByRole('main').getByRole('link', { name: label, exact: true })).toBeVisible();
    }
    await expect(page.getByRole('link', { name: 'New Invoice' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 2, name: 'Recent Patient Bills' })).toBeVisible();
    for (const column of ['Invoice #', 'Patient Name', 'UHID', 'Consultant(s)', 'Payment']) {
      await expect(page.getByRole('columnheader', { name: column, exact: true })).toBeVisible();
    }
  });

  for (const { label, path, heading } of ACCOUNTS_TABS.slice(1)) {
    test(`${label} tab opens ${path}`, async ({ page }) => {
      await page.goto('/finance/accounts');
      await page.getByRole('main').getByRole('link', { name: label, exact: true }).click();
      await expect(page).toHaveURL(new RegExp(`${path}$`));
      await expect(page.getByRole('heading', { level: 1, name: heading })).toBeVisible();
    });
  }

  const reportSections = [
    { path: '/finance/accounts/reports', section: /^Income/ },
    { path: '/finance/accounts/reports/profit', section: /^Billed Services/ },
    { path: '/finance/accounts/reports/laboratory', section: /^Billed Services/ },
    { path: '/finance/accounts/reports/radiology', section: /^Billed Services/ },
    { path: '/finance/accounts/reports/consultant', section: /^By Consultant/ },
  ];
  for (const { path, section } of reportSections) {
    test(`${path} shows the default-range report without a search`, async ({ page }) => {
      await page.goto(path);
      await expect(page.getByRole('main').getByLabel('From', { exact: true })).toHaveValue(/^\d{4}-\d{2}-\d{2}$/);
      await expect(page.getByRole('main').getByLabel('To', { exact: true })).toHaveValue(/^\d{4}-\d{2}-\d{2}$/);
      await expect(page.getByRole('heading', { level: 2, name: section })).toBeVisible();
      await expect(page.getByText('No data to display')).toHaveCount(0);
    });
  }

  test('All Invoices search and payment-status filter narrow the list', async ({ page }) => {
    await page.goto('/finance/accounts/invoices');
    const rows = page.getByRole('table').getByRole('rowgroup').nth(1).getByRole('row');
    await expect(rows.first()).toBeVisible();

    const firstInvoice = (await rows.first().getByRole('cell').first().textContent())!.trim();
    await page.getByRole('main').getByRole('searchbox', { name: 'Search' }).fill(firstInvoice);
    await expect(rows).toHaveCount(1);
    await expect(rows.first()).toContainText(firstInvoice);

    await page.getByRole('button', { name: 'Reset' }).click();
    await expect(page.getByRole('main').getByRole('searchbox', { name: 'Search' })).toHaveValue('');

    await page.getByRole('combobox', { name: 'Filter by payment status' }).click();
    await page.getByRole('option', { name: 'Paid only', exact: true }).click();
    await expect(rows.first()).toBeVisible();
    for (const row of await rows.all()) {
      await expect(row.getByRole('cell').nth(5)).toHaveText('Paid');
    }
  });
});

test.describe('OPD Billing Entry — patient picker', () => {
  test('needs a filter before searching, and says when nobody matches', async ({ page }) => {
    const billing = new InvoiceCreatePage(page);
    await billing.navigate();
    const search = page.getByRole('button', { name: 'Search' });
    await expect(search).toBeDisabled();
    await expect(page.getByText('Recent visits')).toBeVisible();

    await page.getByLabel('UHID').fill('NO-SUCH-UHID-000');
    await search.click();
    await expect(page.getByText('No patients found matching the search criteria.')).toBeVisible();
  });
});

test.describe('OPD Billing Entry — end to end', () => {
  // Registers a real patient + OP visit and saves one real paid invoice in the dev tenant.
  test('bills a consultation with a split payment and guards against double billing', async ({ page }) => {
    test.setTimeout(150_000);
    const patient = await test.step('register a patient with an OP visit to a billable consultant', async () =>
      new PatientRegistrationPage(page).registerWithOpVisit(newPatient(), billableConsultant),
    );
    const billing = new InvoiceCreatePage(page);
    const invoiceCreates: string[] = [];
    page.on('request', (r) => r.method() === 'POST' && /\/api\/v1\/billing\/invoices$/.test(r.url()) && invoiceCreates.push(r.url()));

    await test.step('selecting the patient pre-fills the visit\'s department and consultant', async () => {
      await billing.navigate();
      await billing.selectPatient(patient.uhid);
      await expect(page.getByRole('button', { name: 'Department', exact: true })).toHaveText(billableConsultant.department);
      await expect(page.getByRole('button', { name: 'Consultant', exact: true })).toHaveText(billableConsultant.consultant);
    });

    await test.step('a consultation needs a consultation type', async () => {
      await billing.consultationCharge.fill('100');
      await billing.collectPaymentButton.click();
      await expect(page.getByText('Consultation type is required')).toBeVisible();
      expect(invoiceCreates).toHaveLength(0);
    });

    let charge = 0;
    await test.step('picking a consultation type sets and locks the charge', async () => {
      await billing.pick('Consultation type');
      const typeLabel = (await billing.consultationType.textContent())!;
      charge = Number(typeLabel.match(/₹([\d,]+)/)![1].replace(/,/g, ''));
      expect(charge).toBeGreaterThan(0);
      await expect(billing.consultationCharge).toHaveValue(String(charge));
      await expect(billing.consultationCharge).toBeDisabled();
      await expect(billing.summary).toContainText(money('Net Payable', charge));
    });

    await test.step('payment needs a mode, is capped at the amount due, and must cover the bill', async () => {
      await expect(billing.paymentAmounts.first()).toHaveValue(String(charge)); // pre-filled with Net Payable
      await billing.collectPaymentButton.click();
      await expect(page.getByText('Payment mode is required')).toBeVisible();

      // Typing more than is due is capped to the amount due — there's no change-back flow.
      await billing.setPayment(0, 'Cash', charge + 50);
      await expect(billing.paymentAmounts.first()).toHaveValue(String(charge));

      await billing.setPayment(0, 'Cash', charge - 50);
      await billing.collectPaymentButton.click();
      await expect(page.getByText('Full payment is required before this invoice can be saved', { exact: true })).toBeVisible();
      expect(invoiceCreates, 'nothing saved while payment is invalid').toHaveLength(0);
    });

    let invoiceNumber = '';
    await test.step('a Cash + UPI split that adds up saves a paid invoice', async () => {
      await billing.addPaymentMethodButton.click();
      // The second row is capped at what's still due (₹50), so asking for more still gives 50.
      await billing.setPayment(1, 'UPI', 500);
      await expect(billing.paymentAmounts.nth(1)).toHaveValue('50');
      await expect(billing.summary).toContainText(money(`Total entered\\s*₹${charge.toLocaleString('en-IN')}\\s*of`, charge));

      const created = page.waitForResponse(isInvoiceCreate);
      await billing.collectPaymentButton.click();
      const response = await created;
      expect(response.status()).toBe(201);
      invoiceNumber = (await response.json()).data.invoiceNumber;

      await expect(page.getByRole('status').filter({ hasText: `Invoice ${invoiceNumber} saved.` })).toBeVisible();
      const saved = page.getByRole('main');
      await expect(saved.getByRole('heading', { level: 3, name: `${patient.fullName} · ${patient.uhid}` })).toBeVisible();
      await expect(saved).toContainText('Paid');
      await expect(saved).toContainText(money('Net amount', charge));
      await expect(page.getByRole('button', { name: 'Bill Another Patient' })).toBeVisible();
    });

    await test.step('the invoice is in Recent Bills and All Invoices, and its detail page cannot be voided once paid', async () => {
      await page.goto('/finance/accounts');
      const recent = page.getByRole('row').filter({ hasText: invoiceNumber });
      await expect(recent).toContainText(patient.uhid);
      await expect(recent).toContainText(money('Paid', charge));

      await page.goto('/finance/accounts/invoices');
      await page.getByRole('main').getByRole('searchbox', { name: 'Search' }).fill(invoiceNumber);
      const row = page.getByRole('row').filter({ hasText: invoiceNumber });
      await expect(row).toHaveCount(1);
      await expect(row).toContainText('Paid');
      await row.getByRole('link', { name: 'View' }).click();

      await expect(page).toHaveURL(/\/finance\/accounts\/[0-9a-f-]{36}$/);
      await expect(page.getByRole('main')).toContainText(`Invoice ${invoiceNumber}`);
      await expect(page.getByRole('main')).toContainText('Paid');
      // Void is only offered on invoices with no payment recorded.
      await expect(page.getByRole('button', { name: 'Void Invoice' })).toHaveCount(0);
    });

    await test.step('the Consultant report attributes the revenue to the consultant', async () => {
      await page.goto('/finance/accounts/reports/consultant');
      await page.getByRole('button', { name: 'Search', exact: true }).click();
      await expect(page.getByRole('main').getByText(billableConsultant.consultant).first()).toBeVisible();
    });

    await test.step('billing the same visit again does not pre-fill a second consultation, and warns before adding one', async () => {
      await billing.navigate();
      await billing.selectPatient(patient.uhid);
      await expect(page.getByRole('button', { name: 'Expand Consultation Billing' })).toBeVisible();

      await page.getByRole('button', { name: 'Expand Consultation Billing' }).click();
      await billing.pick('Department', billableConsultant.department);
      await billing.pick('Consultant', billableConsultant.consultant);
      await billing.pick('Consultation type');
      await billing.setPayment(0, 'Cash', charge);

      const before = invoiceCreates.length;
      await billing.collectPaymentButton.click();
      const dialog = page.getByRole('dialog', { name: 'Consultation already billed for this visit' });
      await expect(dialog).toBeVisible();
      await dialog.getByRole('button', { name: 'Cancel' }).click();
      await expect(dialog).toBeHidden();
      expect(invoiceCreates, 'Cancel must not save a second consultation').toHaveLength(before);
    });
  });
});
