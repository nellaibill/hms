import type { Patient, PatientVisit } from '@hms/shared';
import { patientsApi } from '@/services/apiClient';

/** Unpaginated patient list for Patient Reports — walks every page at the server's maximum
 * page size (PagedRequest.MaxPageSize = 100), same "a single big-page request silently
 * truncates" fix already applied to Finance's getAllInvoicesForReport and Masters'
 * masterStoreFactory.getAll(). Patient has no date-range filter server-side, so the caller
 * (usePatientsForReportQuery) always fetches everything and filters by range client-side. */
export async function getAllPatientsForReport(): Promise<Patient[]> {
  const all: Patient[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const paged = await patientsApi.getPatients({ page, pageSize: 100 });
    all.push(...paged.items);
    totalPages = paged.meta.totalPages;
    page++;
  } while (page <= totalPages);
  return all;
}

/** Unpaginated, cross-patient visit list for Patient Reports — same page-walking fix as above,
 * but `from`/`to` are pushed server-side (PatientVisitsController's GetAll accepts them
 * directly), unlike patients above, so this only ever fetches the rows the selected date range
 * actually needs. `to` (a plain YYYY-MM-DD date) is pushed to end-of-day before sending — the
 * backend compares the full CreatedAt timestamp with `<= To`, so sending just the bare date
 * would mean "up to midnight" and silently exclude every visit later that same day. */
export async function getAllPatientVisitsForReport(from?: string, to?: string): Promise<PatientVisit[]> {
  const toEndOfDay = to ? `${to}T23:59:59.999` : undefined;
  const all: PatientVisit[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const paged = await patientsApi.getAllVisits({ page, pageSize: 100, from, to: toEndOfDay });
    all.push(...paged.items);
    totalPages = paged.meta.totalPages;
    page++;
  } while (page <= totalPages);
  return all;
}
