import type { Patient, PatientVisit } from '@hms/shared';
import { patientsApi } from '@/services/apiClient';

// Firing every remaining page in one Promise.all was tried first and confirmed live, against
// a tenant with 7,000+ patients (70+ pages at the server's MaxPageSize of 100), to blow through
// the API's global rate limiter (200 req/min per IP, shared across every browser tab hitting
// this dev API) and exhaust connections (500s, ERR_CONNECTION_REFUSED) well before it exhausted
// the page list. A small fixed concurrency still gets most of the parallelism benefit over
// one-page-at-a-time without either failure mode.
const CONCURRENT_PAGE_FETCHES = 5;

interface PageResult<T> {
  items: T[];
  meta: { totalPages: number };
}

interface FetchAllPagesResult<T> {
  items: T[];
  /** True when more matching rows exist beyond `maxPages` — the caller stopped short of a
   * complete fetch rather than walking an effectively unbounded result set. */
  truncated: boolean;
}

/** Walks pages in small concurrent batches, same reasoning CONCURRENT_PAGE_FETCHES documents,
 * but stops at `maxPages` rather than walking however many pages actually exist — confirmed
 * live that even *date-range-filtered* patients can still run into the thousands on a
 * synthetic/bulk-imported dev tenant (12,000+ patients inside a 90-day window here), so
 * unbounded page-walking isn't safe even with the range filter from ADR-070's first cut. A
 * demographic breakdown chart doesn't need an exact census to be useful — a large bounded
 * sample is enough — so this trades completeness for a guaranteed request ceiling and
 * discloses the tradeoff via `truncated` rather than silently sampling. */
async function fetchAllPages<T>(fetchPage: (page: number) => Promise<PageResult<T>>, maxPages: number): Promise<FetchAllPagesResult<T>> {
  const first = await fetchPage(1);
  const { totalPages } = first.meta;
  const all = [...first.items];
  const pagesToFetch = Math.min(totalPages, maxPages);
  if (pagesToFetch <= 1) return { items: all, truncated: totalPages > maxPages };

  const remainingPages = Array.from({ length: pagesToFetch - 1 }, (_, index) => index + 2);
  for (let i = 0; i < remainingPages.length; i += CONCURRENT_PAGE_FETCHES) {
    const batch = remainingPages.slice(i, i + CONCURRENT_PAGE_FETCHES);
    const results = await Promise.all(batch.map((page) => fetchPage(page)));
    for (const result of results) all.push(...result.items);
  }
  return { items: all, truncated: totalPages > maxPages };
}

/** `to` (a plain YYYY-MM-DD date) needs pushing to end-of-day before sending — the backend
 * compares the full CreatedAt timestamp with `<= To`, so sending just the bare date would mean
 * "up to midnight" and silently exclude every row later that same day. */
function toEndOfDay(to: string | undefined): string | undefined {
  return to ? `${to}T23:59:59.999` : undefined;
}

/** Capped at 2,000 patients (20 pages at the server's MaxPageSize of 100) — see
 * fetchAllPages's own comment for why a hard cap is necessary even with the server-side
 * date-range filter added for this feature. Returns the most recently registered patients
 * within range first (the endpoint's own default sort), so a truncated result still favors
 * the most currently-relevant rows over an arbitrary cutoff. */
const MAX_PATIENT_PAGES = 20;

export interface PatientReportData {
  patients: Patient[];
  /** True when the selected range matched more than MAX_PATIENT_PAGES pages of patients — the
   * charts below are based on a bounded sample, not a full count, when this is true. */
  truncated: boolean;
}

export async function getAllPatientsForReport(from: string, to: string): Promise<PatientReportData> {
  const result = await fetchAllPages((page) => patientsApi.getPatients({ page, pageSize: 100, from, to: toEndOfDay(to) }), MAX_PATIENT_PAGES);
  return { patients: result.items, truncated: result.truncated };
}

/** Unpaginated, cross-patient visit list for Patient Reports — same page-walking and
 * date-range-filtering approach as patients above. Visits are a real clinical action (not a
 * bulk-imported demographic dataset), so they're far smaller in practice, but still capped as
 * a safety net rather than trusting that to hold forever. */
const MAX_VISIT_PAGES = 50;

export async function getAllPatientVisitsForReport(from: string, to: string): Promise<PatientVisit[]> {
  const result = await fetchAllPages((page) => patientsApi.getAllVisits({ page, pageSize: 100, from, to: toEndOfDay(to) }), MAX_VISIT_PAGES);
  return result.items;
}
