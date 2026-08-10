import type { CreatePatientRequest, PagedPatients, Patient, PatientListQuery, UpdatePatientRequest } from '@hms/shared';
import { calculateAge } from './detailedAge';
import { MOCK_PATIENTS } from './mockPatients';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catch in the patients hooks) — lets Patient Management be demoed for client sign-off
 * before the backend is wired up. Persisted to localStorage (not a real backend, just
 * survives page refreshes during a demo) — remove alongside the fallback catches once the
 * backend is live.
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

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(patients));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let patients: Patient[] = loadPatients();
let nextSeq = patients.reduce((max, p) => Math.max(max, Number(p.id.replace('mock-', '')) || 0), 0) + 1;

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

export function createMockPatient(request: CreatePatientRequest): Patient {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const patient: Patient = {
    id: `mock-${String(seq).padStart(3, '0')}`,
    uhid: `NH2026${String(seq).padStart(4, '0')}`,
    title: request.title,
    firstName: request.firstName,
    lastName: request.lastName,
    dateOfBirth: request.dateOfBirth,
    age: calculateAge(request.dateOfBirth),
    gender: request.gender,
    bloodGroup: request.bloodGroup,
    addressLine1: request.addressLine1,
    addressLine2: request.addressLine2,
    addressLine3: request.addressLine3,
    district: request.district,
    state: request.state,
    pincode: request.pincode,
    primaryPhone: request.primaryPhone,
    primaryPhoneRelation: request.primaryPhoneRelation,
    alternatePhone: request.alternatePhone,
    email: request.email,
    profession: request.profession,
    emergencyContactRelationship: request.emergencyContactRelationship,
    emergencyContactName: request.emergencyContactName,
    emergencyContactPhone: request.emergencyContactPhone,
    hasKnownAllergy: request.hasKnownAllergy,
    allergyType: request.allergyType,
    allergySeverity: request.allergySeverity,
    currentRegistration: {
      id: `mock-reg-${String(seq).padStart(3, '0')}`,
      registrationNumber: `REG2026${String(seq).padStart(4, '0')}`,
      encounterType: request.registration.encounterType,
      modeOfArrival: request.registration.modeOfArrival,
      departmentId: request.registration.departmentId,
      consultantId: request.registration.consultantId,
      admissionType: request.registration.admissionType,
      referralSource: request.registration.referralSource,
      category: request.registration.category,
      createdAt: now,
    },
    createdAt: now,
  };
  patients = [patient, ...patients];
  persist();
  return patient;
}

export function updateMockPatient(id: string, request: UpdatePatientRequest): Patient {
  const existing = getMockPatientById(id);
  if (!existing) {
    throw new Error(`Mock patient ${id} not found.`);
  }
  const updated: Patient = {
    ...existing,
    ...request,
    age: calculateAge(request.dateOfBirth),
    updatedAt: new Date().toISOString(),
  };
  patients = patients.map((p) => (p.id === id ? updated : p));
  persist();
  return updated;
}

export function deleteMockPatient(id: string): void {
  patients = patients.filter((p) => p.id !== id);
  persist();
}
