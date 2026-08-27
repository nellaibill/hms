import { IdCard } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const designationConfig: MasterEntityConfig = {
  key: 'designation',
  label: 'Designation',
  labelPlural: 'Designations',
  description: 'Employee designation/job-title directory — reference data for HR Employee records.',
  icon: IdCard,
  section: 'Hospital Reference Data',
  codeField: 'code',
  nameField: 'name',
  fields: [
    { key: 'code', label: 'Designation Code', type: 'text', required: true },
    { key: 'name', label: 'Designation Name', type: 'text', required: true },
  ],
};
