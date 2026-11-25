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
      visitStartTime2: record.visitStartTime2 as string | null | undefined,
      visitEndTime2: record.visitEndTime2 as string | null | undefined,
    }),
  photo: { urlField: 'photoUrl', helpText: 'JPG, PNG (Max 2MB)' },
  consultantLogin: true,
  // A schedule is all or nothing (ADR-148): days with both visiting times, or none at all for a
  // doctor with no fixed days, so such a doctor can still be saved (and given a login).
  validateForm: (values) => {
    const days = Array.isArray(values.availableDays) ? values.availableDays.length : 0;
    const start = Boolean(values.visitStartTime);
    const end = Boolean(values.visitEndTime);
    if (days === 0 && !start && !end) return undefined;
    if (days === 0) return 'Select the available days for these visiting hours, or clear the hours for a doctor with no fixed schedule.';
    if (!start || !end) return 'Enter both the visiting start and end time for the selected days.';
    return undefined;
  },
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
      key: 'consultationTypeCharges',
      label: 'Consultation Types',
      type: 'reference-checkboxes-amount',
      referenceEntityKey: 'consultationType',
      referenceActiveOnly: true,
      arrayItemKeys: { id: 'consultationTypeId', amount: 'consultantCharge' },
      referenceAmountField: 'amount',
      // ADR-132: every consultant takes review and emergency patients, so a new consultant
      // starts with these two ticked. Existing consultants got them from the Masters migration
      // BackfillConsultantDefaultConsultationTypes, which matches the same names.
      defaultCheckedOptionNames: ['Review Consultation', "Emergency / Casualty Doctor's Consultation"],
      required: true,
      showInTable: false,
      helpText:
        'Select the consultation types this consultant offers and set the consultant charge for each — the hospital charge and resulting margin are shown alongside it.',
    },
    {
      key: 'availableDays',
      label: 'Available Days',
      type: 'day-checkboxes',
      showInTable: false,
      helpText: 'Select the days when the doctor is available at the hospital. Leave the days and visiting hours blank for an on-call or visiting doctor with no fixed schedule.',
    },
    {
      key: 'visitStartTime',
      label: 'Start Time',
      type: 'time',
      showInTable: false,
      rangeLabel: 'Visiting Hours',
      helpText: "Doctor's availability/visit timing at the hospital.",
    },
    { key: 'visitEndTime', label: 'End Time', type: 'time', showInTable: false },
    {
      key: 'visitStartTime2',
      label: 'Second Session Start Time',
      type: 'time',
      showInTable: false,
      rangeLabel: 'Second Visiting Hours',
      helpText: 'Optional — e.g. an evening clinic on the same days. Leave both blank if there is no second session.',
    },
    { key: 'visitEndTime2', label: 'Second Session End Time', type: 'time', showInTable: false },
  ],
  fieldGroups: [
    { key: 'info', label: 'Consultant Information', icon: User, fieldKeys: ['name', 'departmentId', 'specialization', 'priority'] },
    {
      key: 'consultationTypes',
      label: 'Consultation Types',
      icon: ClipboardList,
      fieldKeys: ['consultationTypeCharges'],
      infoText: 'Only active consultation types are shown. Please select all applicable types for this consultant.',
    },
    {
      key: 'availability',
      label: 'Doctor Availability',
      icon: Calendar,
      fieldKeys: ['availableDays', 'visitStartTime', 'visitEndTime', 'visitStartTime2', 'visitEndTime2'],
      dividedColumns: true,
      infoText: 'This schedule will be shown to receptionists when selecting a consultant (for appointment registration, OPD, etc.).',
    },
  ],
};
