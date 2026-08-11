import type { PagedPatients, Patient, PatientListQuery } from '@hms/shared';
import { calculateAge } from './detailedAge';
import { MOCK_PATIENTS } from './mockPatients';

/**
 * Offline fallback used only for *browsing* (list/search, single lookup) when the real API
 * is unreachable — lets the search screen still show something instead of a dead error, with
 * the "Demo data" banner (see PatientsListPage) making clear it isn't live data. Writes
 * (create/update/delete) deliberately do NOT fall back here — silently "succeeding" a
 * create/edit/delete against this fake local store, with no duplicate check and a real
 * hard-delete where the UI promises a soft one, is worse than just failing loudly. See
 * usePatientMutations.ts.
 */
const STORAGE_KEY = 'hms-mock-patients';

// Age is never trusted from storage — same invariant as the backend's Patient.Age computed
// property ("always derived from DateOfBirth, never stored"). Whatever age a record was
// saved with can go stale the moment a birthday passes, so every patient that leaves this
// module (list results, single lookups) gets its age recalculated fresh from dateOfBirth
// first, keeping age search/display correct no matter how long ago the record was seeded.
function withCurrentAge(patient: Patient): Patient {
  return { ...patient, age: calculateAge(patient.dateOfBirth) };
}

function loadPatients(): Patient[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Patient[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return [...MOCK_PATIENTS];
}

let patients: Patient[] = loadPatients();

function compareBy(field: string, direction: 1 | -1) {
  return (a: Patient, b: Patient) => {
    let left: string;
    let right: string;
    switch (field) {
      case 'uhid':
        left = a.uhid;
        right = b.uhid;
        break;
      case 'createdAt':
        left = a.createdAt;
        right = b.createdAt;
        break;
      case 'lastName':
      default:
        left = a.lastName;
        right = b.lastName;
        break;
    }
    return left.localeCompare(right) * direction;
  };
}

export function listMockPatients(query: PatientListQuery): PagedPatients {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const name = query.name?.trim().toLowerCase();
  const uhid = query.uhid?.trim().toLowerCase();
  const phone = query.phone?.trim().toLowerCase();
  const age = query.age;

  // Each provided field narrows the result further (AND, not OR) — matches "search using
  // any one or a combination" rather than the old single fuzzy search box.
  let items = patients.map(withCurrentAge);
  if (name) {
    items = items.filter((p) => `${p.firstName} ${p.lastName}`.toLowerCase().includes(name));
  }
  if (uhid) {
    items = items.filter((p) => p.uhid.toLowerCase().includes(uhid));
  }
  if (phone) {
    items = items.filter((p) => p.primaryPhone.toLowerCase().includes(phone));
  }
  if (age !== undefined && !Number.isNaN(age)) {
    items = items.filter((p) => p.age === age);
  }

  const sort = query.sort ?? '-createdAt';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = sort.startsWith('-') ? sort.slice(1) : sort;
  items = [...items].sort(compareBy(field, direction));

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return {
    items: pageItems,
    meta: { page, pageSize, totalCount, totalPages },
  };
}

export function getMockPatientById(id: string): Patient | undefined {
  const patient = patients.find((p) => p.id === id);
  return patient && withCurrentAge(patient);
}
