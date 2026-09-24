import { expect, type Locator, type Page } from '@playwright/test';

/** One OPD consultation note at /clinical/opd/consultations/:consultationId. */
export class OpdConsultationPage {
  readonly heading: Locator;
  readonly header: Locator;
  readonly height: Locator;
  readonly weight: Locator;
  readonly presentingComplaints: Locator;
  readonly planOfManagement: Locator;
  readonly investigationName: Locator;
  readonly addInvestigationButton: Locator;
  readonly saveDraftButton: Locator;
  readonly completeButton: Locator;
  readonly reopenButton: Locator;
  /** Toast notifications ("Draft saved", "Consultation completed", …). */
  readonly toast: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1, name: 'OPD Consultation' });
    // Patient summary strip: name heading + UHID, consultant, department, status.
    this.header = page.getByRole('main');
    this.height = page.getByLabel('Height (cm) *');
    this.weight = page.getByLabel('Weight (kg) *');
    this.presentingComplaints = page.getByLabel('Presenting Complaints *');
    this.planOfManagement = page.getByRole('textbox', { name: 'Plan of Management' });
    this.investigationName = page.getByRole('textbox', { name: 'Investigation', exact: true });
    this.addInvestigationButton = page.getByRole('button', { name: 'Add Investigation' });
    this.saveDraftButton = page.getByRole('button', { name: /^(Save Draft|Saving…)/ });
    this.completeButton = page.getByRole('button', { name: /^(Complete Consultation|Completing…)/ });
    this.reopenButton = page.getByRole('button', { name: /^(Reopen|Reopening…)$/ });
    this.toast = page.getByRole('status');
  }

  async addInvestigation(name: string) {
    await this.investigationName.fill(name);
    await this.addInvestigationButton.click();
  }

  /** Picks the first diagnosis from the searchable diagnosis master and adds it. Returns its name. */
  async addFirstDiagnosis(): Promise<string> {
    await this.page.getByRole('button', { name: 'Search diagnosis…' }).click();
    const option = this.page.getByRole('dialog').getByRole('option').first();
    const name = (await option.textContent())!.trim();
    await option.click();
    await this.page.getByRole('button', { name: 'Add', exact: true }).click();
    return name;
  }

  /** Closes any toasts so they can't cover the action bar. */
  async dismissToasts() {
    const dismiss = this.page.getByRole('button', { name: 'Dismiss notification' });
    while ((await dismiss.count()) > 0) await dismiss.first().click();
    await expect(dismiss).toHaveCount(0);
  }

  tableRow(name: string): Locator {
    return this.page.getByRole('row', { name: new RegExp(name) });
  }
}
