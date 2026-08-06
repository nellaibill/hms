import { DOCTORS } from '@/config/hospitalData';
import type { EntityType } from './types';

export interface EntityOption {
  id: string;
  label: string;
}

const PATIENTS: EntityOption[] = [
  { id: 'PT-1001', label: 'Meena Sundaram (PT-1001)' },
  { id: 'PT-1002', label: 'Rajesh Pandian (PT-1002)' },
  { id: 'PT-1003', label: 'Kavitha Raman (PT-1003)' },
  { id: 'PT-1004', label: 'Muthu Selvam (PT-1004)' },
  { id: 'PT-1005', label: 'Deepa Nachiyar (PT-1005)' },
  { id: 'PT-1006', label: 'Saravanan Pillai (PT-1006)' },
  { id: 'PT-1007', label: 'Anitha Krishnan (PT-1007)' },
  { id: 'PT-1008', label: 'Prabhakaran M (PT-1008)' },
];

const STAFF: EntityOption[] = [
  { id: 'ST-2001', label: 'Kavitha R — Staff Nurse (ST-2001)' },
  { id: 'ST-2002', label: 'Muthu S — Lab Technician (ST-2002)' },
  { id: 'ST-2003', label: 'Ganesan K — Ward Boy (ST-2003)' },
  { id: 'ST-2004', label: 'Revathi P — OT Nurse (ST-2004)' },
  { id: 'ST-2005', label: 'Selvi N — Receptionist (ST-2005)' },
  { id: 'ST-2006', label: 'Anand V — Pharmacist (ST-2006)' },
];

const DOCTOR_ENTITIES: EntityOption[] = DOCTORS.map((doctor, index) => ({
  id: `DR-${3001 + index}`,
  label: `${doctor.name} — ${doctor.specialty} (DR-${3001 + index})`,
}));

const APPOINTMENTS: EntityOption[] = [
  { id: 'APT-4001', label: 'APT-4001 — Dr. Karthikeyan / 06 Aug 2026' },
  { id: 'APT-4002', label: 'APT-4002 — Dr. Revathi / 07 Aug 2026' },
  { id: 'APT-4003', label: 'APT-4003 — Dr. Meenakshi / 08 Aug 2026' },
  { id: 'APT-4004', label: 'APT-4004 — Dr. Suresh Kumar / 09 Aug 2026' },
];

const ADMISSIONS: EntityOption[] = [
  { id: 'ADM-5001', label: 'ADM-5001 — Ward 3B, Bed 12' },
  { id: 'ADM-5002', label: 'ADM-5002 — ICU, Bed 4' },
  { id: 'ADM-5003', label: 'ADM-5003 — Ward 1A, Bed 7' },
];

const LAB_ORDERS: EntityOption[] = [
  { id: 'LAB-6001', label: 'LAB-6001 — Complete Blood Count' },
  { id: 'LAB-6002', label: 'LAB-6002 — Lipid Profile' },
  { id: 'LAB-6003', label: 'LAB-6003 — Liver Function Test' },
  { id: 'LAB-6004', label: 'LAB-6004 — HbA1c' },
];

const RADIOLOGY_ORDERS: EntityOption[] = [
  { id: 'RAD-7001', label: 'RAD-7001 — Chest X-Ray' },
  { id: 'RAD-7002', label: 'RAD-7002 — MRI Brain' },
  { id: 'RAD-7003', label: 'RAD-7003 — Abdominal Ultrasound' },
];

const INVOICES: EntityOption[] = [
  { id: 'INV-8001', label: 'INV-8001 — ₹4,500' },
  { id: 'INV-8002', label: 'INV-8002 — ₹12,300' },
  { id: 'INV-8003', label: 'INV-8003 — ₹2,150' },
  { id: 'INV-8004', label: 'INV-8004 — ₹8,900' },
];

const ASSETS: EntityOption[] = [
  { id: 'AST-9001', label: 'AST-9001 — MRI Machine (Radiology)' },
  { id: 'AST-9002', label: 'AST-9002 — Ventilator Unit 3 (ICU)' },
  { id: 'AST-9003', label: 'AST-9003 — Autoclave Sterilizer (OT)' },
  { id: 'AST-9004', label: 'AST-9004 — Ambulance TN-72-AZ-1180 (Emergency)' },
];

const VENDORS: EntityOption[] = [
  { id: 'VEN-1101', label: 'MedSupply Distributors Pvt Ltd (VEN-1101)' },
  { id: 'VEN-1102', label: 'ClinLab Equipments Pvt Ltd (VEN-1102)' },
  { id: 'VEN-1103', label: 'Sunrise Pharma Wholesalers (VEN-1103)' },
  { id: 'VEN-1104', label: 'Apex Medical Devices Co. (VEN-1104)' },
];

export const ENTITY_OPTIONS: Record<EntityType, EntityOption[]> = {
  Patient: PATIENTS,
  Staff: STAFF,
  Doctor: DOCTOR_ENTITIES,
  Appointment: APPOINTMENTS,
  Admission: ADMISSIONS,
  Lab: LAB_ORDERS,
  Radiology: RADIOLOGY_ORDERS,
  Billing: INVOICES,
  Asset: ASSETS,
  Vendor: VENDORS,
};

export function getEntityLabel(entityType: EntityType, entityId: string): string {
  const match = ENTITY_OPTIONS[entityType]?.find((option) => option.id === entityId);
  return match?.label ?? entityId;
}

export function entityExists(entityType: EntityType, entityId: string): boolean {
  return ENTITY_OPTIONS[entityType]?.some((option) => option.id === entityId) ?? false;
}
