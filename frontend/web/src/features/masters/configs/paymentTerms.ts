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
};
