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
    {
      key: 'amount',
      label: 'Hospital Charge (₹)',
      type: 'decimal',
      min: 0,
      step: 1,
      // Not shown as a list column anymore — the per-consultant Consultant Charge/Hospital
      // Charge/Hospital Income breakdown now lives on the Consultant Edit page (see
      // configs/consultant.ts's 'reference-checkboxes-amount' field), which made this list's
      // own Amount/Running Cost/Margin columns redundant. Still editable on this entity's own
      // Add/Edit form — that's what the Consultant page's read-only Hospital Charge reads from.
      showInTable: false,
      helpText: 'What the patient is billed for this consultation type. Leave blank for categories with no fixed rate (e.g. On-call) — decided per visit instead.',
    },
  ],
};
