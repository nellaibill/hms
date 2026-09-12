import { Building2, Calendar, Stethoscope, User } from 'lucide-react';
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
    {
      key: 'consultantType',
      label: 'Consultant Type',
      type: 'radio-card',
      required: true,
      options: [
        { value: 'InHouse', label: 'In-house Doctor', description: 'Regular doctor employed by the hospital.', icon: Building2 },
        { value: 'Visiting', label: 'Visiting Doctor', description: 'External consultant who visits the hospital on specific days and time.', icon: User },
      ],
      infoBox: {
        title: 'Billing Information',
        text: 'Consultant type determines the applicable consultation billing category (In-house or Visiting). This setting will be used during patient billing.',
      },
    },
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
    {
      key: 'availableDays',
      label: 'Available Days',
      type: 'day-checkboxes',
      required: true,
      showInTable: false,
      helpText: 'Select the days when the doctor is available at the hospital.',
    },
    {
      key: 'visitStartTime',
      label: 'Start Time',
      type: 'time',
      required: true,
      showInTable: false,
      rangeLabel: 'Visiting Hours',
      helpText: "Doctor's availability/visit timing at the hospital.",
    },
    { key: 'visitEndTime', label: 'End Time', type: 'time', required: true, showInTable: false },
  ],
  fieldGroups: [
    { key: 'consultantType', label: 'Consultant Type *', icon: User, fieldKeys: ['consultantType'] },
    { key: 'info', label: 'Consultant Information', icon: User, fieldKeys: ['name', 'departmentId', 'specialization', 'priority'] },
    {
      key: 'availability',
      label: 'Doctor Availability',
      icon: Calendar,
      fieldKeys: ['availableDays', 'visitStartTime', 'visitEndTime'],
      dividedColumns: true,
      infoText: 'This schedule will be shown to receptionists when selecting a consultant (for appointment registration, OPD, etc.).',
    },
  ],
};
