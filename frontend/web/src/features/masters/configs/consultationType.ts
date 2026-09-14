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
      helpText: 'What the patient is billed for this consultation type. Leave blank for categories with no fixed rate (e.g. On-call) — decided per visit instead.',
    },
    {
      key: 'costPrice',
      label: 'Running Cost (₹)',
      type: 'decimal',
      min: 0,
      step: 1,
      defaultValue: 0,
      helpText: 'What it costs the hospital to deliver this consultation once (allocated staff/facility time) — used to calculate profit margin. Leave 0 if not yet costed.',
    },
  ],
  extraColumns: [
    {
      key: 'margin',
      label: 'Margin',
      // costPrice of 0 means "not yet costed", not "free to run"; amount can legitimately be
      // unset (no fixed rate) — both render as non-numbers rather than a misleading figure.
      render: (record) => {
        const amount = record.amount;
        const costPrice = Number(record.costPrice ?? 0);
        if (amount === null || amount === undefined || amount === '') return '—';
        const price = Number(amount);
        if (costPrice <= 0) return 'Not costed';
        const margin = price - costPrice;
        const marginPercent = price > 0 ? (margin / price) * 100 : 0;
        const sign = margin < 0 ? '-' : '';
        return `${sign}₹${Math.abs(margin).toLocaleString('en-IN')} (${marginPercent.toFixed(0)}%)`;
      },
    },
  ],
};
