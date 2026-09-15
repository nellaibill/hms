import { Stethoscope } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const diagnosisConfig: MasterEntityConfig = {
  key: 'diagnosis',
  label: 'Diagnosis',
  labelPlural: 'Diagnoses',
  description: 'Hospital-maintained diagnosis catalog (e.g. "Osteoarthritis of knee") for the OPD Consultation form — grows as staff use it, not a full ICD-10 import.',
  icon: Stethoscope,
  section: 'Hospital Reference Data',
  nameField: 'name',
  fields: [
    { key: 'name', label: 'Diagnosis Name', type: 'text', required: true },
    { key: 'icdCode', label: 'ICD Code', type: 'text', placeholder: 'Optional', helpText: 'Leave blank if this diagnosis hasn’t been coded yet.' },
  ],
};
