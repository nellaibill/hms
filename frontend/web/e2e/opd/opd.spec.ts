import fs from 'node:fs';
import { expect, test } from '../fixtures/auth.fixture';
import { OpdPage } from '../pages/OpdPage';
import { OpdConsultationPage } from '../pages/OpdConsultationPage';
import { PatientRegistrationPage } from '../pages/PatientRegistrationPage';
import { isoDateFromToday, newPatient } from '../support/testData';

test.describe('OPD work-list', () => {
  let opd: OpdPage;

  test.beforeEach(async ({ page }) => {
    opd = new OpdPage(page);
    await opd.navigate();
  });

  test('opens on today\'s Patient List with every filter and tab', async () => {
    const today = isoDateFromToday();
    await expect(opd.fromDate).toHaveValue(today);
    await expect(opd.toDate).toHaveValue(today);
    await expect(opd.departmentFilter).toBeEnabled();
    await expect(opd.statusFilter).toHaveText('All statuses');
    await expect(opd.search).toBeEditable();
    await expect(opd.refreshButton).toBeVisible();
    await expect(opd.exportButton).toBeVisible();

    for (const name of ['Patient List', 'Consultation List', 'Investigations List', 'Procedures List', 'Admissions List'] as const) {
      await expect(opd.tab(name)).toBeVisible();
    }
    await expect(opd.tab('Patient List')).toHaveAttribute('aria-selected', 'true');
    await expect(opd.panel.getByRole('heading', { level: 2, name: /^OPD Patients \(/ })).toBeVisible();
  });

  test('needs a department before a consultant can be picked', async ({ page }) => {
    await expect(opd.consultantFilter).toBeDisabled();
    await opd.departmentFilter.click();
    await page.getByRole('dialog').getByRole('option').first().click();
    await expect(opd.consultantFilter).toBeEnabled();
  });

  test('filtering by department shows only that department', async ({ page }) => {
    await opd.fromDate.fill(isoDateFromToday(-30));
    await opd.departmentFilter.click();
    const option = page.getByRole('dialog').getByRole('option').first();
    const department = (await option.textContent())!.trim().replace(/\s*\([^()]*\)$/, '');
    await option.click();
    await expect(opd.departmentFilter).toContainText(department);

    // Either matching rows or the empty state — never a row from another department.
    await expect(opd.rows.first().or(opd.panel.getByText('No OPD patients found'))).toBeVisible();
    for (const row of await opd.rows.all()) {
      await expect(row.getByRole('cell').nth(6)).toHaveText(department);
    }
  });

  test('filtering by status shows only that status', async ({ page }) => {
    await opd.fromDate.fill(isoDateFromToday(-30));
    await opd.statusFilter.click();
    await page.getByRole('option', { name: 'Waiting', exact: true }).click();
    await expect(opd.statusFilter).toHaveText('Waiting');

    await expect(opd.rows.first().or(opd.panel.getByText('No OPD patients found'))).toBeVisible();
    for (const row of await opd.rows.all()) {
      await expect(row.getByRole('cell').nth(8)).toHaveText('Waiting');
    }
  });

  test('shows an empty state for a date range with no visits', async () => {
    const future = isoDateFromToday(365);
    await opd.toDate.fill(future);
    await opd.fromDate.fill(future);
    await expect(opd.panel.getByText('No OPD patients found')).toBeVisible();
    await expect(opd.panel.getByText('Try a different date range or filter.')).toBeVisible();
  });

  test('shows an empty state when the search matches nobody', async () => {
    await opd.search.fill('zz-no-such-patient-zz');
    await expect(opd.panel.getByText('No OPD patients found')).toBeVisible();
  });

  test('switches between the five list tabs', async () => {
    await opd.openTab('Consultation List');
    await expect(opd.panel.getByRole('columnheader', { name: 'Consultant' }).or(opd.panel.getByText(/^No .* found$/))).toBeVisible();

    await opd.openTab('Investigations List');
    await expect(opd.panel.getByRole('table').or(opd.panel.getByText('No investigations found'))).toBeVisible();

    await opd.openTab('Procedures List');
    await expect(opd.panel.getByRole('table').or(opd.panel.getByText(/^No procedures found/))).toBeVisible();

    await opd.openTab('Admissions List');
    await expect(opd.panel.getByRole('button', { name: 'Request Admission' })).toBeVisible();

    await opd.openTab('Patient List');
    await expect(opd.panel.getByRole('heading', { level: 2, name: /^OPD Patients \(/ })).toBeVisible();
  });

  test('exports the Patient List as CSV', async ({ page }) => {
    const downloadPromise = page.waitForEvent('download');
    await opd.exportButton.click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(/^opd-patients-\d{4}-\d{2}-\d{2}\.csv$/);
    const csv = fs.readFileSync(await download.path(), 'utf8');
    expect(csv).toContain('Patient Name');
    expect(csv).toContain('UHID');
  });
});

test.describe('OPD consultation — end to end', () => {
  // Registers a real patient + today's OP visit in the dev tenant ("Etoe… Regression").
  test('takes a new OP visit from Waiting through a completed consultation', async ({ page }) => {
    test.setTimeout(120_000);
    const patient = await test.step('register a patient with an OP visit', async () =>
      new PatientRegistrationPage(page).registerWithOpVisit(newPatient()),
    );
    const opd = new OpdPage(page);
    const consultation = new OpdConsultationPage(page);
    let consultationUrl = '';

    await test.step('the visit is in today\'s Patient List as Waiting / Not Billed', async () => {
      await opd.navigate();
      await opd.search.fill(patient.uhid);
      const row = opd.patientRow(patient.uhid);
      await expect(row).toHaveCount(1);
      await expect(row).toContainText(patient.fullName);
      await expect(row).toContainText(patient.primaryPhone);
      await expect(row).toContainText(patient.consultant);
      await expect(row).toContainText(patient.department);
      await expect(row).toContainText('Waiting');
      await expect(row).toContainText('Not Billed');
      await expect(row.getByRole('button', { name: 'Consult' })).toBeVisible();
    });

    await test.step('the consultant\'s queue in the Consultation List counts the patient', async () => {
      await opd.openTab('Consultation List');
      const consultantRow = opd.rows.filter({ hasText: patient.consultant });
      await expect(consultantRow).toHaveCount(1);
      // Total Patients / Waiting columns are at least 1 now.
      await expect(consultantRow.getByRole('cell').nth(3)).toHaveText(/^[1-9]\d*$/);
      await opd.openTab('Patient List');
    });

    await test.step('Consult starts the consultation', async () => {
      const started = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/start-consultation'));
      await opd.patientRow(patient.uhid).getByRole('button', { name: 'Consult' }).click();
      expect((await started).ok()).toBe(true);

      await expect(page).toHaveURL(/\/clinical\/opd\/consultations\/[0-9a-f-]{36}$/);
      consultationUrl = new URL(page.url()).pathname;
      await expect(consultation.heading).toBeVisible();
      await expect(page.getByRole('heading', { level: 1, name: patient.fullName })).toBeVisible();
      await expect(consultation.header).toContainText(patient.uhid);
      await expect(consultation.header).toContainText(patient.consultant);
    });

    await test.step('Complete needs the presenting complaints', async () => {
      const completes: string[] = [];
      page.on('request', (r) => r.method() === 'POST' && r.url().endsWith('/complete') && completes.push(r.url()));
      await consultation.completeButton.click();
      await expect(page.getByText('Presenting complaints are required to complete a consultation.')).toBeVisible();
      expect(completes, 'nothing sent while invalid').toHaveLength(0);
    });

    let diagnosis = '';
    await test.step('Save Draft keeps the notes, investigation and diagnosis across a reload', async () => {
      await consultation.height.fill('170');
      await consultation.weight.fill('70');
      await expect(page.getByText('BMI')).toBeVisible();
      await expect(page.getByText('24.2')).toBeVisible(); // 70 / 1.7² = 24.2
      await consultation.presentingComplaints.fill('Fever for three days');
      await consultation.planOfManagement.fill('Paracetamol 500 mg SOS');
      await consultation.addInvestigation('CBC');
      await expect(consultation.tableRow('CBC Laboratory Routine')).toBeVisible();
      diagnosis = await consultation.addFirstDiagnosis();
      await expect(consultation.tableRow(diagnosis)).toBeVisible();

      const saved = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/draft'));
      await consultation.saveDraftButton.click();
      expect((await saved).ok()).toBe(true);
      await expect(consultation.toast).toContainText('Draft saved');

      await page.reload();
      await expect(consultation.height).toHaveValue('170');
      await expect(consultation.weight).toHaveValue('70');
      await expect(consultation.presentingComplaints).toHaveValue('Fever for three days');
      await expect(consultation.planOfManagement).toHaveValue('Paracetamol 500 mg SOS');
      await expect(consultation.tableRow('CBC Laboratory Routine')).toBeVisible();
      await expect(consultation.tableRow(diagnosis)).toBeVisible();
    });

    await test.step('Complete locks the note', async () => {
      const completed = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/complete'));
      await consultation.completeButton.click();
      expect((await completed).ok()).toBe(true);
      await expect(consultation.toast).toContainText('Consultation completed');

      await expect(consultation.header).toContainText('Completed');
      await expect(consultation.presentingComplaints).toBeDisabled();
      await expect(consultation.height).toBeDisabled();
      await expect(consultation.saveDraftButton).toHaveCount(0);
      await expect(consultation.completeButton).toHaveCount(0);
      await expect(consultation.reopenButton).toBeVisible();
    });

    await test.step('the Patient List now shows Completed with a View action', async () => {
      await opd.navigate();
      await opd.search.fill(patient.uhid);
      const row = opd.patientRow(patient.uhid);
      await expect(row).toContainText('Completed');
      await expect(row.getByRole('button', { name: 'Consult' })).toHaveCount(0);
      await row.getByRole('button', { name: 'View' }).click();
      await expect(page).toHaveURL(new RegExp(`${consultationUrl}$`));
    });

    await test.step('Reopen asks first, then makes the note editable again', async () => {
      await consultation.reopenButton.click();
      const dialog = page.getByRole('dialog', { name: 'Reopen this consultation?' });
      await dialog.getByRole('button', { name: 'Cancel' }).click();
      await expect(dialog).toBeHidden();
      await expect(consultation.presentingComplaints).toBeDisabled();

      await consultation.reopenButton.click();
      const reopened = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/reopen'));
      await dialog.getByRole('button', { name: 'Reopen Consultation' }).click();
      expect((await reopened).ok()).toBe(true);
      await expect(consultation.toast).toContainText('Consultation reopened');
      await expect(consultation.presentingComplaints).toBeEditable();
      await expect(consultation.presentingComplaints).toHaveValue('Fever for three days');
      await expect(consultation.completeButton).toBeVisible();
    });
  });

  test('will not complete a consultation without height and weight', async ({ page }) => {
    test.setTimeout(120_000);
    const patient = await new PatientRegistrationPage(page).registerWithOpVisit(newPatient());
    const opd = new OpdPage(page);
    await opd.navigate();
    await opd.search.fill(patient.uhid);
    await opd.patientRow(patient.uhid).getByRole('button', { name: 'Consult' }).click();

    const consultation = new OpdConsultationPage(page);
    await consultation.presentingComplaints.fill('Headache');
    const completeResponse = page.waitForResponse((r) => r.request().method() === 'POST' && r.url().endsWith('/complete'));
    await consultation.completeButton.click();

    // The page's own check lets blank vitals through as 0 (its "Height is required…" message
    // can't trigger), so it's the API that refuses — the outcome that matters: not completed.
    expect((await completeResponse).status()).toBe(400);
    await expect(page.getByText('Height must be greater than 0.')).toBeVisible();
    await expect(page.getByText('Weight must be greater than 0.')).toBeVisible();
    await expect(consultation.header).toContainText('In Consultation');
    await expect(consultation.completeButton).toBeVisible();
    await expect(consultation.presentingComplaints).toBeEditable();
  });
});
