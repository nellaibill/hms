import { expect, type Locator, type Page } from '@playwright/test';

export type OpdTabName = 'Patient List' | 'Consultation List' | 'Investigations List' | 'Procedures List' | 'Admissions List';

/** The OPD work-list at /clinical/opd — filter bar plus five list tabs. */
export class OpdPage {
  readonly heading: Locator;
  readonly fromDate: Locator;
  readonly toDate: Locator;
  readonly departmentFilter: Locator;
  readonly consultantFilter: Locator;
  readonly statusFilter: Locator;
  readonly search: Locator;
  readonly refreshButton: Locator;
  readonly exportButton: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1, name: 'Out Patient Department (OPD)' });
    this.fromDate = page.getByLabel('From Date');
    this.toDate = page.getByLabel('To Date');
    this.departmentFilter = page.getByRole('button', { name: 'Filter by department' });
    this.consultantFilter = page.getByRole('button', { name: 'Filter by consultant' });
    this.statusFilter = page.getByRole('combobox', { name: 'Filter by status' });
    // The header's global search box is also labelled "Search", so target the OPD one by placeholder.
    this.search = page.getByPlaceholder('Patient Name / UHID / Phone');
    this.refreshButton = page.getByRole('button', { name: 'Refresh' });
    this.exportButton = page.getByRole('button', { name: 'Export' });
  }

  async navigate() {
    await this.page.goto('/clinical/opd');
    await expect(this.heading).toBeVisible();
  }

  tab(name: OpdTabName): Locator {
    return this.page.getByRole('tab', { name });
  }

  async openTab(name: OpdTabName) {
    await this.tab(name).click();
    await expect(this.tab(name)).toHaveAttribute('aria-selected', 'true');
  }

  get panel(): Locator {
    return this.page.getByRole('tabpanel');
  }

  /** Data rows (header row excluded) of the active tab's table. */
  get rows(): Locator {
    return this.panel.getByRole('table').getByRole('rowgroup').nth(1).getByRole('row');
  }

  /** The Patient List row for one patient, matched by UHID. */
  patientRow(uhid: string): Locator {
    return this.rows.filter({ hasText: uhid });
  }
}
