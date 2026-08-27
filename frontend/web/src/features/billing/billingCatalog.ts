import { DOCTORS } from '@/config/hospitalData';

/**
 * Mock billing reference data (departments/consultants/services with prices) — no billing
 * master-data API exists yet, so this stands in for it. Consultants are drawn from the same
 * DOCTORS roster used across auth/patients/dashboard so names stay consistent app-wide.
 */

export interface BillingDepartment {
  id: string;
  name: string;
  /** Standard OP consultation fee for this department, before consultant/type adjustments. */
  baseConsultationFee: number;
}

export interface BillingConsultant {
  id: string;
  name: string;
  departmentId: string;
  /** Applied on top of the department's base fee — senior/specialist consultants charge more. */
  feeMultiplier: number;
}

export interface ConsultationTypeOption {
  id: string;
  name: string;
  /** Applied on top of the department base fee × consultant multiplier. */
  multiplier: number;
}

export interface BillingService {
  id: string;
  name: string;
  price: number;
}

export interface ServiceConsultant {
  id: string;
  name: string;
}

export const CONSULTATION_DEPARTMENTS: BillingDepartment[] = [
  { id: 'general-medicine', name: 'General Medicine', baseConsultationFee: 300 },
  { id: 'cardiology', name: 'Cardiology', baseConsultationFee: 600 },
  { id: 'orthopedics', name: 'Orthopedics', baseConsultationFee: 500 },
  { id: 'pediatrics', name: 'Pediatrics', baseConsultationFee: 350 },
  { id: 'gynecology', name: 'Gynecology', baseConsultationFee: 450 },
  { id: 'neurology', name: 'Neurology', baseConsultationFee: 700 },
  { id: 'dermatology', name: 'Dermatology', baseConsultationFee: 400 },
  { id: 'ent', name: 'ENT', baseConsultationFee: 350 },
  { id: 'general-surgery', name: 'General Surgery', baseConsultationFee: 550 },
];

const CONSULTANT_FEE_MULTIPLIERS: Record<string, number> = {
  'Dr. Revathi': 1.2,
  'Dr. Balasubramanian': 1.1,
  'Dr. Priya': 1.3,
  'Dr. Arun Kumar': 1.2,
};

function departmentIdFor(specialty: string): string {
  const department = CONSULTATION_DEPARTMENTS.find((d) => d.name === specialty);
  return department?.id ?? '';
}

export const CONSULTATION_CONSULTANTS: BillingConsultant[] = DOCTORS.filter((doctor) => departmentIdFor(doctor.specialty))
  .map((doctor) => ({
    id: doctor.name.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
    name: doctor.name,
    departmentId: departmentIdFor(doctor.specialty),
    feeMultiplier: CONSULTANT_FEE_MULTIPLIERS[doctor.name] ?? 1,
  }));

export const CONSULTATION_TYPES: ConsultationTypeOption[] = [
  { id: 'new', name: 'New Patient', multiplier: 1 },
  { id: 'follow-up', name: 'Follow-up', multiplier: 0.5 },
  { id: 'emergency', name: 'Emergency', multiplier: 1.5 },
  { id: 'second-opinion', name: 'Second Opinion', multiplier: 1.2 },
];

export function getConsultantsForDepartment(departmentId: string): BillingConsultant[] {
  return CONSULTATION_CONSULTANTS.filter((c) => c.departmentId === departmentId);
}

/** Department base fee × consultant multiplier × consultation-type multiplier, rounded to the nearest ₹5. */
export function getConsultationCharge(departmentId: string, consultantId: string, consultationTypeId: string): number {
  const department = CONSULTATION_DEPARTMENTS.find((d) => d.id === departmentId);
  const consultant = CONSULTATION_CONSULTANTS.find((c) => c.id === consultantId);
  const type = CONSULTATION_TYPES.find((t) => t.id === consultationTypeId);
  if (!department || !consultant || !type) return 0;
  const raw = department.baseConsultationFee * consultant.feeMultiplier * type.multiplier;
  return Math.round(raw / 5) * 5;
}

export const RADIOLOGY_SERVICES: BillingService[] = [
  { id: 'xray-chest', name: 'X-Ray — Chest', price: 300 },
  { id: 'xray-limb', name: 'X-Ray — Limb', price: 350 },
  { id: 'usg-abdomen', name: 'Ultrasound — Abdomen', price: 800 },
  { id: 'ct-head', name: 'CT Scan — Head', price: 2500 },
  { id: 'ct-chest', name: 'CT Scan — Chest', price: 3000 },
  { id: 'mri-brain', name: 'MRI — Brain', price: 5500 },
  { id: 'mri-spine', name: 'MRI — Spine', price: 6000 },
  { id: 'mammography', name: 'Mammography', price: 1200 },
];

export const RADIOLOGY_CONSULTANTS: ServiceConsultant[] = [
  { id: 'dr-nirmala', name: 'Dr. Nirmala (Radiologist)' },
  { id: 'dr-ashok-kumar', name: 'Dr. Ashok Kumar (Radiologist)' },
  { id: 'ms-divya', name: 'Ms. Divya (Radiology Technician)' },
];

export const LABORATORY_SERVICES: BillingService[] = [
  { id: 'cbc', name: 'Complete Blood Count (CBC)', price: 250 },
  { id: 'blood-sugar-fasting', name: 'Blood Sugar — Fasting', price: 100 },
  { id: 'lipid-profile', name: 'Lipid Profile', price: 600 },
  { id: 'lft', name: 'Liver Function Test (LFT)', price: 550 },
  { id: 'kft', name: 'Kidney Function Test (KFT)', price: 500 },
  { id: 'urine-routine', name: 'Urine — Routine', price: 150 },
  { id: 'thyroid-profile', name: 'Thyroid Profile', price: 700 },
  { id: 'covid-rtpcr', name: 'COVID-19 RT-PCR', price: 900 },
];

export const LABORATORY_CONSULTANTS: ServiceConsultant[] = [
  { id: 'dr-geetha', name: 'Dr. Geetha (Pathologist)' },
  { id: 'mr-vignesh', name: 'Mr. Vignesh (Lab Technician)' },
  { id: 'ms-anitha', name: 'Ms. Anitha (Lab Technician)' },
];

export const PROCEDURE_SERVICES: BillingService[] = [
  { id: 'wound-dressing', name: 'Wound Dressing', price: 200 },
  { id: 'suturing', name: 'Suturing', price: 500 },
  { id: 'nebulization', name: 'Nebulization', price: 150 },
  { id: 'ecg', name: 'ECG', price: 250 },
  { id: 'catheterization', name: 'Catheterization', price: 600 },
  { id: 'minor-surgery', name: 'Minor Surgery', price: 3500 },
  { id: 'plaster-cast', name: 'Plaster Cast', price: 800 },
  { id: 'injection', name: 'Injection Administration', price: 100 },
];

export const PROCEDURE_CONSULTANTS: ServiceConsultant[] = [
  { id: 'dr-arun-kumar', name: 'Dr. Arun Kumar (General Surgery)' },
  { id: 'dr-suresh-kumar', name: 'Dr. Suresh Kumar (Orthopedics)' },
  { id: 'staff-nurse-kalaivani', name: 'Staff Nurse Kalaivani' },
];

export function getServicePrice(services: BillingService[], serviceId: string): number {
  return services.find((s) => s.id === serviceId)?.price ?? 0;
}
