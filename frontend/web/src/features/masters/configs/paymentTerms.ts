import { CalendarClock } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const paymentTermsConfig: MasterEntityConfig = {
  key: 'paymentTerms',
  label: 'Payment Term',
  labelPlural: 'Payment Terms',
  description: 'Standard payment terms offered to/by Suppliers and Customers (e.g. Net 30).',
  icon: CalendarClock,
  section: 'Finance & Payment',
  nameField: 'termName',
  fields: [
    { key: 'termName', label: 'Term Name', type: 'text', required: true, placeholder: 'e.g. Net 30' },
    { key: 'days', label: 'Days', type: 'number', required: true, min: 0, step: 1 },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  seed: [
    { id: 'payment-terms-001', termName: 'Due on Receipt', days: 0 },
    { id: 'payment-terms-002', termName: 'Net 15', days: 15 },
    { id: 'payment-terms-003', termName: 'Net 30', days: 30 },
    { id: 'payment-terms-004', termName: 'Net 45', days: 45 },
  ],
};
