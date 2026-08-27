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
};
