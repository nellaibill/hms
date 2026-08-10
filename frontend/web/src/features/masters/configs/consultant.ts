import { Stethoscope } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const consultantConfig: MasterEntityConfig = {
  key: 'consultant',
  label: 'Consultant',
  labelPlural: 'Consultants',
  description: 'Consulting doctor directory, optionally linked to a Department — shared reference data for Patient registration.',
  icon: Stethoscope,
  section: 'Hospital Reference Data',
  codeField: 'code',
  nameField: 'name',
  fields: [
    { key: 'code', label: 'Consultant Code', type: 'text', required: true },
    { key: 'name', label: 'Consultant Name', type: 'text', required: true },
    { key: 'departmentId', label: 'Department', type: 'reference', referenceEntityKey: 'department' },
    { key: 'specialization', label: 'Specialization', type: 'text' },
  ],
  seed: [
    { id: 'consultant-001', code: 'DR-001', name: 'Dr. Asha Verma', departmentId: 'department-001', specialization: 'Interventional Cardiology' },
    { id: 'consultant-002', code: 'DR-002', name: 'Dr. Rohan Mehta', departmentId: 'department-002', specialization: 'Joint Replacement' },
  ],
};
