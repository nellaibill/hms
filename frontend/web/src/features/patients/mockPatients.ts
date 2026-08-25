import type { Patient } from '@hms/shared';

/**
 * Seed data for the demo/offline fallback (see mockPatientsStore.ts) — lets the Patient
 * Management UI still show something instead of a dead error when the real API is
 * unreachable. Not used once the API is reachable. A small, hand-maintainable set (not the
 * full realistic roster this used to carry) since every field now has to match the real
 * backend's nested Address/Allergies[]/EmergencyContacts[] shape exactly — State/District use
 * placeholder ids (there's no offline Masters data to resolve a real one against; StateName/
 * DistrictName correctly fall back to a truncated id display for these, same as they would
 * for any id they can't resolve).
 *
 * mock-001 through mock-006 are also referenced by mockBillingStore.ts's demo invoices — keep
 * those six ids stable if this file is ever regenerated.
 *
 * No `age` here on purpose — it's derived from dateOfBirth below (and again on every read in
 * mockPatientsStore.ts), matching the backend's Patient.Age computed property.
 */
const PLACEHOLDER_STATE_ID = '00000000-0000-0000-0000-0000000000a1';
const PLACEHOLDER_DISTRICT_ID = '00000000-0000-0000-0000-0000000000b1';

export const MOCK_PATIENTS: Array<Omit<Patient, 'age'>> = [
  {
    id: 'mock-001',
    uhid: 'NH20260001',
    title: 'Mr',
    firstName: 'Aravind',
    lastName: 'Nadar',
    dateOfBirth: '1988-04-12',
    gender: 'Male',
    bloodGroup: 'OPositive',
    maritalStatus: 'Married',
    primaryPhone: '9442112345',
    email: 'aravind.nadar@example.com',
    profession: 'Textile Merchant',
    modeOfArrivalSource: 'DoctorReferral',
    address: {
      addressLine1: '12, Bharathiyar Street',
      addressLine3: 'Palayamkottai',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627002',
    },
    allergies: [],
    emergencyContacts: [{ id: 'mock-001-ec1', relationship: 'Spouse', name: 'Meena Nadar', phone: '9442112346' }],
    rowVersion: '1',
    createdAt: '2026-07-20T09:15:00.000Z',
  },
  {
    id: 'mock-002',
    uhid: 'NH20260002',
    title: 'Mrs',
    firstName: 'Kavitha',
    lastName: 'Pillai',
    dateOfBirth: '1975-11-02',
    gender: 'Female',
    bloodGroup: 'APositive',
    maritalStatus: 'Married',
    primaryPhone: '9843223456',
    email: 'kavitha.pillai@example.com',
    profession: 'School Teacher',
    modeOfArrivalSource: 'PatientOrRelativeReferral',
    address: {
      addressLine1: '45, Perumal Koil Street',
      addressLine3: 'Tirunelveli Town',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627001',
    },
    allergies: [{ id: 'mock-002-al1', allergyType: 'Drug', specify: 'Penicillin', severity: 'Moderate' }],
    emergencyContacts: [{ id: 'mock-002-ec1', relationship: 'Son', name: 'Ravi Pillai', phone: '9843223457' }],
    rowVersion: '1',
    createdAt: '2026-07-21T11:40:00.000Z',
  },
  {
    id: 'mock-003',
    uhid: 'NH20260003',
    title: 'Master',
    firstName: 'Praveen',
    lastName: 'Iyer',
    dateOfBirth: '2018-02-19',
    gender: 'Male',
    bloodGroup: 'BPositive',
    maritalStatus: 'NA',
    primaryPhone: '9345561234',
    modeOfArrivalSource: 'DoctorReferral',
    address: {
      addressLine1: '7, Nethaji Road',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627003',
    },
    allergies: [],
    emergencyContacts: [{ id: 'mock-003-ec1', relationship: 'Father', name: 'Suresh Iyer', phone: '9345561235' }],
    rowVersion: '1',
    createdAt: '2026-07-22T10:05:00.000Z',
  },
  {
    id: 'mock-004',
    uhid: 'NH20260004',
    title: 'Ms',
    firstName: 'Nandhini',
    lastName: 'Thevar',
    dateOfBirth: '1996-07-30',
    gender: 'Female',
    bloodGroup: 'ABPositive',
    maritalStatus: 'Unmarried',
    primaryPhone: '9788112233',
    email: 'nandhini.thevar@example.com',
    profession: 'Software Engineer',
    modeOfArrivalSource: 'OnlineAdvertisement',
    modeOfArrivalChannel: 'Google',
    address: {
      addressLine1: '23, Anna Nagar',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627011',
    },
    allergies: [{ id: 'mock-004-al1', allergyType: 'Food', specify: 'Shellfish', severity: 'Severe' }],
    emergencyContacts: [{ id: 'mock-004-ec1', relationship: 'Mother', name: 'Lakshmi Thevar', phone: '9788112234' }],
    rowVersion: '1',
    createdAt: '2026-07-23T14:20:00.000Z',
  },
  {
    id: 'mock-005',
    uhid: 'NH20260005',
    title: 'Mr',
    firstName: 'Vignesh',
    lastName: 'Rajan',
    dateOfBirth: '1982-09-14',
    gender: 'Male',
    bloodGroup: 'OPositive',
    maritalStatus: 'Married',
    primaryPhone: '9600445566',
    profession: 'Auto Driver',
    modeOfArrivalSource: 'DoctorReferral',
    idProofType: 'Aadhaar',
    idProofNumber: '234567890123',
    address: {
      addressLine1: '9, Market Street',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627004',
    },
    allergies: [],
    emergencyContacts: [{ id: 'mock-005-ec1', relationship: 'Brother', name: 'Ganesh Rajan', phone: '9600445567' }],
    rowVersion: '1',
    createdAt: '2026-07-24T08:50:00.000Z',
  },
  {
    id: 'mock-006',
    uhid: 'NH20260006',
    title: 'Mrs',
    firstName: 'Keerthana',
    lastName: 'Chettiar',
    dateOfBirth: '1990-01-25',
    gender: 'Female',
    bloodGroup: 'BNegative',
    maritalStatus: 'Married',
    primaryPhone: '9894556677',
    email: 'keerthana.chettiar@example.com',
    profession: 'Homemaker',
    modeOfArrivalSource: 'OfflineAdvertisement',
    modeOfArrivalChannel: 'Newspapers',
    address: {
      addressLine1: '61, South Car Street',
      stateId: PLACEHOLDER_STATE_ID,
      districtId: PLACEHOLDER_DISTRICT_ID,
      pincode: '627006',
    },
    allergies: [{ id: 'mock-006-al1', allergyType: 'Environmental', specify: 'Dust', severity: 'Mild' }],
    emergencyContacts: [{ id: 'mock-006-ec1', relationship: 'Spouse', name: 'Muthu Chettiar', phone: '9894556678' }],
    rowVersion: '1',
    createdAt: '2026-07-25T16:30:00.000Z',
  },
];
