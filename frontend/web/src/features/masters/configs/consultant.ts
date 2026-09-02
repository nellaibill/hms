import { Stethoscope } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const consultantConfig: MasterEntityConfig = {
  key: 'consultant',
  label: 'Consultant',
  labelPlural: 'Consultants',
  description: 'Consulting doctor directory, optionally linked to a Department — shared reference data for Patient registration.',
  icon: Stethoscope,
  section: 'Hospital Reference Data',
  nameField: 'name',
  fields: [
    // skipUniquenessCheck: two consultants can legitimately share a display name (e.g. two
    // "Dr. Sharma"s) — see ConsultantSelect's own comment on using Specialization instead of
    // a Code to tell them apart.
    { key: 'name', label: 'Consultant Name', type: 'text', required: true, skipUniquenessCheck: true },
    { key: 'departmentId', label: 'Department', type: 'reference', referenceEntityKey: 'department' },
    { key: 'specialization', label: 'Specialization', type: 'text' },
    {
      key: 'priority',
      label: 'Priority',
      type: 'number',
      min: 1,
      helpText: 'Controls display order in consultant pickers (Registration, Billing, etc.) — lower shows first. Leave blank for no preference.',
    },
  ],
};
