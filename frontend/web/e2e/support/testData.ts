/**
 * Unique, recognisable test data. Every record the suite creates in the dev tenant starts
 * with "Etoe" (the backend's name rules allow letters only, so no "E2E-123" style names)
 * and carries a per-run suffix, so they're easy to find and never collide with each other.
 */

/** Encodes digits as letters (0→a … 9→j) so a timestamp can live inside a letters-only name. */
function lettersFrom(value: number): string {
  return String(value)
    .split('')
    .map((digit) => String.fromCharCode(97 + Number(digit)))
    .join('');
}

function randomDigits(length: number): string {
  let digits = '';
  while (digits.length < length) digits += Math.floor(Math.random() * 10);
  return digits;
}

export interface NewPatient {
  firstName: string;
  lastName: string;
  /** yyyy-mm-dd, as the native date input expects. */
  dateOfBirth: string;
  maritalStatus: 'Married' | 'Unmarried';
  addressLine1: string;
  state: string;
  district: string;
  pincode: string;
  primaryPhone: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  referringDepartment: string;
  aadhaarNumber: string;
}

export function newPatient(): NewPatient {
  const suffix = lettersFrom(Date.now() % 1_000_000_000);
  return {
    firstName: `Etoe${suffix}`,
    lastName: 'Regression',
    dateOfBirth: '1990-05-15',
    maritalStatus: 'Unmarried',
    addressLine1: '12 Regression Street',
    state: 'Tamil Nadu',
    district: 'Chennai',
    pincode: '600001',
    primaryPhone: `9${randomDigits(9)}`,
    emergencyContactName: 'Kin Regression',
    emergencyContactPhone: `8${randomDigits(9)}`,
    referringDepartment: 'General Medicine',
    // Aadhaar numbers are unique per patient; a first digit of 9 keeps it a valid-looking 12 digits.
    aadhaarNumber: `9${randomDigits(11)}`,
  };
}

/** Today's local date plus `days`, as yyyy-mm-dd (local, not UTC — the app filters by local day). */
export function isoDateFromToday(days = 0): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
