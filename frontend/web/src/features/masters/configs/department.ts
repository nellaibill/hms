import { Building2 } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const departmentConfig: MasterEntityConfig = {
  key: 'department',
  label: 'Department',
  labelPlural: 'Departments',
  description: 'Hospital department directory — shared reference data for Patient registration, HR shift/roster scheduling, and Calendar events.',
  icon: Building2,
  section: 'Hospital Reference Data',
  codeField: 'code',
  nameField: 'name',
  fields: [
    { key: 'code', label: 'Department Code', type: 'text', required: true },
    { key: 'name', label: 'Department Name', type: 'text', required: true },
  ],
};
