import { Stethoscope } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const consultationTypeConfig: MasterEntityConfig = {
  key: 'consultationType',
  label: 'Consultation Type',
  labelPlural: 'Consultation Types',
  description: "Doctor consultation categories and their standard fees (e.g. In-house Regular, Priority, Emergency) — shared reference data for Patient registration.",
  icon: Stethoscope,
  section: 'Hospital Reference Data',
  nameField: 'name',
  fields: [
    { key: 'name', label: 'Consultation Type Name', type: 'text', required: true },
    { key: 'amount', label: 'Amount (₹)', type: 'decimal', min: 0, step: 1, helpText: 'Leave blank for categories with no fixed rate (e.g. On-call) — decided per visit instead.' },
  ],
  seed: [
    { id: 'consultation-type-001', name: "Doctor's Consultation (In-house) - Regular", amount: 200 },
    { id: 'consultation-type-002', name: "Doctor's Consultation (In-house) - Priority", amount: 300 },
    { id: 'consultation-type-003', name: "Doctor's Consultation (Visiting) - Regular", amount: 250 },
    { id: 'consultation-type-004', name: "Emergency / Casualty Doctor's Consultation", amount: 500 },
    { id: 'consultation-type-005', name: "Doctor's Consultation - Others / On-call" },
  ],
};
