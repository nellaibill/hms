import { expect, test } from '../fixtures/auth.fixture';
import { PatientRegistrationPage } from '../pages/PatientRegistrationPage';
import { isoDateFromToday, newPatient } from '../support/testData';

test.describe('Reception & Registration hub', () => {
  test('offers the new-patient and existing-patient flows', async ({ page }) => {
    await page.goto('/patients/registration');
    await expect(page.getByRole('heading', { level: 1, name: 'Reception & Registration' })).toBeVisible();

    await page.getByRole('link', { name: /^New Patient Registration/ }).click();
    await expect(page).toHaveURL(/\/patients\/registration\/new$/);
    await expect(page.getByRole('heading', { level: 1, name: 'New Patient Registration' })).toBeVisible();

    await page.goto('/patients/registration');
    await page.getByRole('link', { name: /^Old Patient Registration/ }).click();
    await expect(page).toHaveURL(/\/patients\/enquiry$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Old Patient Registration' })).toBeVisible();
  });
});

test.describe('New patient registration — validation', () => {
  let wizard: PatientRegistrationPage;

  test.beforeEach(async ({ page }) => {
    wizard = new PatientRegistrationPage(page);
    await wizard.navigate();
  });

  test('opens on Patient Information with sensible defaults', async ({ page }) => {
    await expect(wizard.tab('Patient Information')).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByRole('combobox', { name: 'Title' })).toHaveText('Mr');
    await expect(page.getByRole('combobox', { name: 'Gender' })).toHaveText('Male');
    await expect(page.getByRole('combobox', { name: 'Blood group' })).toHaveText('Unknown');
    await expect(wizard.previousButton).toHaveCount(0);
    await expect(wizard.nextButton).toBeEnabled();
  });

  test('lists every missing Patient Information field and stays on the tab', async () => {
    await wizard.nextButton.click();

    await expect(wizard.errorSummary).toContainText('Please fix the following before continuing:');
    await expect(wizard.errorSummary.getByRole('listitem')).toHaveText([
      'First name is required',
      'Last name is required',
      'Date of birth is required',
    ]);
    await expect(wizard.tab('Patient Information')).toHaveAttribute('aria-selected', 'true');
    await expect(wizard.tab('Patient Information')).toHaveAccessibleName(/This section has errors/);
  });

  test('does not let you skip ahead to a later tab while the current one is invalid', async () => {
    await wizard.tab('Contact Information').click();
    await expect(wizard.tab('Patient Information')).toHaveAttribute('aria-selected', 'true');
    await expect(wizard.tab('Contact Information')).toHaveAttribute('aria-selected', 'false');
  });

  test('rejects digits in names', async () => {
    await wizard.field('First name').fill('John3');
    await wizard.field('Last name').fill('Smith');
    await wizard.field('Date of birth').fill('1990-01-01');
    await wizard.nextButton.click();
    await expect(wizard.errorSummary.getByRole('listitem')).toContainText(['Enter letters only.']);
  });

  test('rejects a date of birth in the future', async () => {
    await wizard.field('First name').fill('Future');
    await wizard.field('Last name').fill('Baby');
    await wizard.field('Date of birth').fill(isoDateFromToday(30));
    await wizard.nextButton.click();
    await expect(wizard.errorSummary.getByRole('listitem')).toContainText(['Date of birth cannot be in the future']);
  });

  test('requires Married or Unmarried for an adult', async ({ page }) => {
    const patient = newPatient();
    await wizard.field('First name').fill(patient.firstName);
    await wizard.field('Last name').fill(patient.lastName);
    await wizard.field('Date of birth').fill(patient.dateOfBirth);
    // Marital status left at its default, N/A.
    await wizard.nextButton.click();

    await expect(wizard.errorSummary.getByRole('listitem')).toHaveText([
      'Marital status must be Married or Unmarried for patients 18 or older.',
    ]);
    // Shows the computed age once a valid date of birth is entered.
    await expect(page.getByText(/^Age: \d+ Years/)).toBeVisible();

    await wizard.choose('Marital status', 'Married');
    await wizard.nextButton.click();
    await expect(wizard.tab('Contact Information')).toHaveAttribute('aria-selected', 'true');
  });

  test('lists every missing Contact Information field', async () => {
    await wizard.fillPatientInformation(newPatient());
    await wizard.nextButton.click();
    await expect(wizard.tab('Contact Information')).toHaveAttribute('aria-selected', 'true');

    await wizard.nextButton.click();
    await expect(wizard.errorSummary.getByRole('listitem')).toHaveText([
      'Address is required',
      'District is required',
      'State is required',
      'Pincode must be 6 digits',
      'Primary phone is required',
      'Emergency contact name is required',
      'Emergency contact phone is required',
    ]);
  });

  test('checks phone and pincode formats, and needs a state before a district', async ({ page }) => {
    const patient = newPatient();
    await wizard.fillPatientInformation(patient);
    await wizard.nextButton.click();

    await expect(page.getByRole('button', { name: 'District', exact: true })).toBeDisabled();
    await wizard.pick('State', patient.state);
    await expect(page.getByRole('button', { name: 'District', exact: true })).toBeEnabled();

    await wizard.field('Pincode').fill('60001');
    await wizard.field('Primary phone').fill('12345');
    await wizard.nextButton.click();
    await expect(wizard.errorSummary.getByRole('listitem')).toContainText([
      'Pincode must be 6 digits',
      'Phone number must be exactly 10 digits.',
    ]);
  });

  test('keeps entered values when going back a tab', async () => {
    const patient = newPatient();
    await wizard.fillPatientInformation(patient);
    await wizard.nextButton.click();
    await wizard.previousButton.click();

    await expect(wizard.tab('Patient Information')).toHaveAttribute('aria-selected', 'true');
    await expect(wizard.field('First name')).toHaveValue(patient.firstName);
    await expect(wizard.field('Date of birth')).toHaveValue(patient.dateOfBirth);
  });

  test('requires the referring department for a Doctor Referral', async ({ page }) => {
    const patient = newPatient();
    await wizard.completeUpToMedicalInformation(patient);
    await wizard.field('Department').fill('');

    const patientCreates: string[] = [];
    page.on('request', (r) => r.method() === 'POST' && /\/api\/v1\/patients$/.test(r.url()) && patientCreates.push(r.url()));
    await wizard.saveAndProceedButton.click();

    await expect(wizard.errorSummary.getByRole('listitem')).toContainText(['Enter the referring department.']);
    await expect(wizard.tab('Medical Information')).toHaveAttribute('aria-selected', 'true');
    expect(patientCreates, 'no patient should be saved while the form is invalid').toHaveLength(0);
  });

  test('asks before discarding a partly filled form', async ({ page }) => {
    await wizard.field('First name').fill('Unsaved');
    await wizard.cancelButton.click();

    const dialog = page.getByRole('dialog', { name: 'Discard unsaved changes?' });
    await expect(dialog).toBeVisible();

    await dialog.getByRole('button', { name: 'Keep editing' }).click();
    await expect(dialog).toBeHidden();
    await expect(wizard.field('First name')).toHaveValue('Unsaved');

    await wizard.cancelButton.click();
    await dialog.getByRole('button', { name: 'Discard and leave' }).click();
    await expect(page).toHaveURL(/\/patients\/registration$/);
  });

  test('leaves an untouched form without asking', async ({ page }) => {
    // KNOWN BUG: the wizard reports unsaved changes on a form nobody has touched, so Cancel
    // (and Back) show "Discard unsaved changes?". Cause: PatientRegistrationForm's
    // defaultValues.arrivalSource has no `doctorReferral` key, but registering the referral
    // Department field adds `doctorReferral: {}`, so React Hook Form's deep-compare isDirty
    // turns true while dirtyFields stays empty. test.fail() keeps the suite green and will
    // flag this test ("expected to fail, but passed") once the bug is fixed — then delete it.
    test.fail(true, 'Known bug: untouched New Patient form is reported as dirty');
    await wizard.cancelButton.click();
    await expect(page).toHaveURL(/\/patients\/registration$/);
    await expect(page.getByRole('dialog')).toHaveCount(0);
  });
});

test.describe('New patient registration — end to end', () => {
  // Creates a real patient + OP visit in the dev tenant (named "Etoe…" so they're easy to spot).
  test('registers a patient, then finds, views and edits them', async ({ page }) => {
    const patient = newPatient();
    const wizard = new PatientRegistrationPage(page);
    let patientUrl = '';
    let uhid = '';

    await test.step('fill Patient, Contact and Medical Information', async () => {
      await wizard.navigate();
      await wizard.completeUpToMedicalInformation(patient);
    });

    await test.step('save the patient and move on to Registration Details', async () => {
      const created = page.waitForResponse((r) => r.request().method() === 'POST' && /\/api\/v1\/patients$/.test(r.url()));
      await wizard.saveAndProceedButton.click();
      const response = await created;
      expect(response.status()).toBe(201);
      // Responses use the standard { data: … } envelope (docs/ApiStandards.md).
      uhid = (await response.json()).data?.uhid;
      expect(uhid, 'API should assign a UHID').toMatch(/\S+/);

      await expect(wizard.tab('Registration Details')).toHaveAttribute('aria-selected', 'true');
      await expect(page.getByText('Patient registered successfully')).toBeVisible();
      await expect(page.getByRole('tabpanel').getByText(uhid, { exact: true })).toBeVisible();
    });

    await test.step('Registration Details needs a department and consultant', async () => {
      await wizard.registerButton.click();
      await expect(wizard.errorSummary.getByRole('listitem')).toHaveText(['Department is required', 'Consultant is required']);
      await expect(page.getByRole('button', { name: 'Consultant', exact: true })).toBeDisabled();
    });

    await test.step('register the OP visit and land on the patient page', async () => {
      await wizard.fillRegistrationDetails();
      const visit = page.waitForResponse((r) => r.request().method() === 'POST' && /\/api\/v1\/patients\/[^/]+\/visits$/.test(r.url()));
      await wizard.registerButton.click();
      expect((await visit).status()).toBe(201);

      await expect(page).toHaveURL(/\/patients\/registration\/[0-9a-f-]{36}$/);
      patientUrl = new URL(page.url()).pathname;
    });

    await test.step('patient page shows what was entered', async () => {
      const main = page.getByRole('main');
      await expect(main.getByRole('heading', { level: 1, name: `Mr ${patient.firstName} ${patient.lastName}` })).toBeVisible();
      await expect(main).toContainText(uhid);
      await expect(main).toContainText(patient.primaryPhone);
      await expect(main).toContainText(patient.addressLine1);
      await expect(main).toContainText(`${patient.district}, ${patient.state} — ${patient.pincode}`);
      await expect(main).toContainText(patient.emergencyContactName);
      // ID proof is masked, showing only the last 4 digits.
      await expect(main).toContainText(`XXXX XXXX ${patient.aadhaarNumber.slice(-4)}`);
      await expect(main).not.toContainText(patient.aadhaarNumber);
      const recentVisits = main.getByRole('table');
      await expect(recentVisits.getByRole('row')).toHaveCount(2); // header + the one OP visit
      await expect(recentVisits.getByRole('row').nth(1)).toContainText('OP');
    });

    await test.step('Patient Enquiry finds the patient by UHID', async () => {
      await page.goto('/patients/enquiry');
      await page.getByLabel('UHID').fill(uhid);
      await page.getByRole('button', { name: 'Search' }).click();

      const rows = page.getByRole('table').getByRole('rowgroup').nth(1).getByRole('row');
      await expect(rows).toHaveCount(1);
      await expect(rows.first()).toContainText(uhid);
      await rows.first().getByRole('link', { name: `Mr ${patient.firstName} ${patient.lastName}` }).click();
      await expect(page).toHaveURL(new RegExp(`${patientUrl}$`));
    });

    await test.step('edit the profession and see it saved', async () => {
      await page.getByRole('link', { name: 'Edit Patient' }).click();
      await expect(page.getByRole('heading', { level: 1, name: `Edit ${patient.firstName} ${patient.lastName}` })).toBeVisible();
      await expect(page.getByLabel('First name', { exact: true })).toHaveValue(patient.firstName);

      await page.getByRole('button', { name: 'Next' }).click();
      await page.getByLabel('Profession', { exact: true }).fill('Regression Tester');
      await page.getByRole('button', { name: 'Next' }).click();

      const updated = page.waitForResponse((r) => r.request().method() === 'PUT' && r.url().includes('/api/v1/patients/'));
      await page.getByRole('button', { name: 'Save Changes' }).click();
      expect((await updated).ok()).toBe(true);

      await expect(page).toHaveURL(new RegExp(`${patientUrl}$`));
      await expect(page.getByRole('main')).toContainText('Regression Tester');
    });
  });
});

test.describe('Patient Enquiry', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/patients/enquiry');
    await expect(page.getByRole('heading', { level: 1, name: 'Old Patient Registration' })).toBeVisible();
  });

  test('lists recently active patients before any search', async ({ page }) => {
    await expect(page.getByRole('table').getByRole('columnheader', { name: 'UHID' })).toBeVisible();
    await expect(page.getByRole('table').getByRole('rowgroup').nth(1).getByRole('row').first()).toBeVisible();
  });

  test('enables Search only once a filter is entered', async ({ page }) => {
    const search = page.getByRole('button', { name: 'Search' });
    await expect(search).toBeDisabled();
    await page.getByLabel('UHID').fill('P-');
    await expect(search).toBeEnabled();
    await page.getByLabel('UHID').fill('');
    await expect(search).toBeDisabled();
  });

  test('shows an empty state for a UHID that does not exist', async ({ page }) => {
    await page.getByLabel('UHID').fill('NO-SUCH-UHID-000');
    await page.getByRole('button', { name: 'Search' }).click();
    await expect(page.getByText('No patients found matching the search criteria.')).toBeVisible();
  });

  test('New Patient opens the registration wizard', async ({ page }) => {
    await page.getByRole('link', { name: 'New Patient' }).click();
    await expect(page).toHaveURL(/\/patients\/registration\/new$/);
  });
});
