/**
 * Central reference data for the Tirunelveli hospital demo — mock-only, consumed by the
 * auth, patients, and dashboard modules so doctor/department names stay consistent across
 * every screen instead of each feature inventing its own.
 */

export interface DoctorProfile {
  name: string;
  specialty: string;
}

export const DOCTORS: DoctorProfile[] = [
  { name: 'Dr. Karthikeyan', specialty: 'General Medicine' },
  { name: 'Dr. Revathi', specialty: 'Cardiology' },
  { name: 'Dr. Suresh Kumar', specialty: 'Orthopedics' },
  { name: 'Dr. Meenakshi', specialty: 'Pediatrics' },
  { name: 'Dr. Balasubramanian', specialty: 'Gynecology' },
  { name: 'Dr. Priya', specialty: 'Neurology' },
  { name: 'Dr. Senthil Kumar', specialty: 'Dermatology' },
  { name: 'Dr. Lakshmi', specialty: 'ENT' },
  { name: 'Dr. Arun Kumar', specialty: 'General Surgery' },
  { name: 'Dr. Nirmala', specialty: 'Radiology' },
];

export function doctorFor(specialty: string): DoctorProfile {
  const match = DOCTORS.find((doctor) => doctor.specialty === specialty);
  if (!match) {
    throw new Error(`No doctor on the roster covers "${specialty}".`);
  }
  return match;
}

export const DEPARTMENTS = [
  'General Medicine',
  'Cardiology',
  'Orthopedics',
  'Pediatrics',
  'Gynecology',
  'Neurology',
  'ICU',
  'Emergency',
  'Laboratory',
  'Pharmacy',
  'Radiology',
] as const;

export interface Locality {
  area: string;
  district: 'Tirunelveli' | 'Tenkasi';
  pincode: string;
}

/** Tirunelveli city localities plus nearby towns — Sankarankovil, Tenkasi, and Alangulam fall
 * under the neighbouring Tenkasi district (carved out of Tirunelveli district in 2021). */
export const LOCALITIES: Locality[] = [
  { area: 'Tirunelveli Town', district: 'Tirunelveli', pincode: '627001' },
  { area: 'Palayamkottai', district: 'Tirunelveli', pincode: '627002' },
  { area: 'Pettai', district: 'Tirunelveli', pincode: '627004' },
  { area: 'Melapalayam', district: 'Tirunelveli', pincode: '627005' },
  { area: 'Ambasamudram', district: 'Tirunelveli', pincode: '627401' },
  { area: 'Cheranmahadevi', district: 'Tirunelveli', pincode: '627105' },
  { area: 'Nanguneri', district: 'Tirunelveli', pincode: '627106' },
  { area: 'Valliyur', district: 'Tirunelveli', pincode: '627117' },
  { area: 'Radhapuram', district: 'Tirunelveli', pincode: '627111' },
  { area: 'Thisayanvilai', district: 'Tirunelveli', pincode: '627657' },
  { area: 'Kadayam', district: 'Tirunelveli', pincode: '627415' },
  { area: 'Kalakkad', district: 'Tirunelveli', pincode: '627501' },
  { area: 'Sankarankovil', district: 'Tenkasi', pincode: '627756' },
  { area: 'Tenkasi', district: 'Tenkasi', pincode: '627811' },
  { area: 'Alangulam', district: 'Tenkasi', pincode: '627851' },
];
