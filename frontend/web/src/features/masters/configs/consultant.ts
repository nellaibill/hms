import { Calendar, ClipboardList, Stethoscope, User } from 'lucide-react';
import { formatConsultantAvailability } from '../formatConsultantAvailability';
import type { MasterEntityConfig } from '../engine/types';

export const consultantConfig: MasterEntityConfig = {
  key: 'consultant',
  label: 'Consultant',
  labelPlural: 'Consultants',
  description: 'Consulting doctor directory, optionally linked to a Department — shared reference data for Patient registration.',
  icon: Stethoscope,
  section: 'Hospital Reference Data',
  nameField: 'name',
  listFilters: ['departmentId'],
  getRowSubtitle: (record) =>
    formatConsultantAvailability({
      availableDays: (record.availableDays as string[] | undefined) ?? [],
      visitStartTime: record.visitStartTime as string | null | undefined,
      visitEndTime: record.visitEndTime as string | null | undefined,
    }),
  photo: { urlField: 'photoUrl', helpText: 'JPG, PNG (Max 2MB)' },
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
    {
      key: 'consultationTypeIds',
      label: 'Consultation Types',
      type: 'reference-checkboxes',
      referenceEntityKey: 'consultationType',
      referenceActiveOnly: true,
      required: true,
      showInTable: false,
      helpText: 'Select the consultation types applicable for this consultant. You can select multiple types.',
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
    { key: 'info', label: 'Consultant Information', icon: User, fieldKeys: ['name', 'departmentId', 'specialization', 'priority'] },
    {
      key: 'consultationTypes',
      label: 'Consultation Types',
      icon: ClipboardList,
      fieldKeys: ['consultationTypeIds'],
      infoText: 'Only active consultation types are shown. Please select all applicable types for this consultant.',
    },
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
