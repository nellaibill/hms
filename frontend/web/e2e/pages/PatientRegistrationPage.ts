import { expect, type Locator, type Page } from '@playwright/test';
import type { NewPatient } from '../support/testData';

/** The 4-tab New Patient Registration wizard at /patients/registration/new. */
export class PatientRegistrationPage {
  readonly heading: Locator;
  /** The "Please fix the following before continuing:" summary above the active tab. */
  readonly errorSummary: Locator;
  readonly nextButton: Locator;
  readonly previousButton: Locator;
  readonly cancelButton: Locator;
  readonly saveAndProceedButton: Locator;
  readonly registerButton: Locator;

  constructor(readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1, name: 'New Patient Registration' });
    this.errorSummary = page.getByRole('tabpanel').getByRole('alert');
    this.nextButton = page.getByRole('button', { name: 'Next' });
    this.previousButton = page.getByRole('button', { name: 'Previous' });
    this.cancelButton = page.getByRole('button', { name: 'Cancel' });
    this.saveAndProceedButton = page.getByRole('button', { name: /Save and proceed to Registration|Saving…/ });
    this.registerButton = page.getByRole('button', { name: /Register Patient|Registering…/ });
  }

  async navigate() {
    await this.page.goto('/patients/registration/new');
    await expect(this.heading).toBeVisible();
  }

  tab(name: 'Patient Information' | 'Contact Information' | 'Medical Information' | 'Registration Details'): Locator {
    return this.page.getByRole('tab', { name: new RegExp(`^${name}`) });
  }

  field(label: string): Locator {
    return this.page.getByRole('tabpanel').getByLabel(label, { exact: true });
  }

  /** Radix Select (role=combobox) — Title, Gender, Marital status, Mode of arrival, etc. */
  async choose(comboboxName: string, option: string) {
    await this.page.getByRole('combobox', { name: comboboxName }).click();
    await this.page.getByRole('option', { name: option, exact: true }).click();
  }

  /** The searchable pop-over pickers (State, District, Department, Consultant): a button that opens a dialog with a search box. */
  async pick(buttonName: string, option?: string) {
    await this.page.getByRole('button', { name: buttonName, exact: true }).click();
    const popover = this.page.getByRole('dialog');
    if (option) {
      await popover.getByRole('textbox').fill(option);
      await popover.getByRole('option', { name: option, exact: true }).click();
    } else {
      await popover.getByRole('option').first().click();
    }
    await expect(popover).toBeHidden();
  }

  async fillPatientInformation(patient: NewPatient) {
    await this.field('First name').fill(patient.firstName);
    await this.field('Last name').fill(patient.lastName);
    await this.field('Date of birth').fill(patient.dateOfBirth);
    await this.choose('Marital status', patient.maritalStatus);
  }

  async fillContactInformation(patient: NewPatient) {
    await this.field('Address line 1 (door no. & building name)').fill(patient.addressLine1);
    await this.pick('State', patient.state);
    await this.pick('District', patient.district);
    await this.field('Pincode').fill(patient.pincode);
    await this.field('Primary phone').fill(patient.primaryPhone);
    await this.field('Name').fill(patient.emergencyContactName);
    await this.field('Phone').fill(patient.emergencyContactPhone);
  }

  async fillMedicalInformation(patient: NewPatient) {
    // Default arrival source is Doctor Referral, which needs the referring department.
    await this.field('Department').fill(patient.referringDepartment);
    await this.field('Aadhaar number').fill(patient.aadhaarNumber);
  }

  /** Picks the first department and its first consultant — any valid pair will do. */
  async fillRegistrationDetails() {
    await this.pick('Department');
    await this.pick('Consultant');
  }

  /** Tabs 1–3 filled and advanced through; ends on Medical Information, not yet saved. */
  async completeUpToMedicalInformation(patient: NewPatient) {
    await this.fillPatientInformation(patient);
    await this.nextButton.click();
    await expect(this.tab('Contact Information')).toHaveAttribute('aria-selected', 'true');
    await this.fillContactInformation(patient);
    await this.nextButton.click();
    await expect(this.tab('Medical Information')).toHaveAttribute('aria-selected', 'true');
    await this.fillMedicalInformation(patient);
  }
}
