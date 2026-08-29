import { UserRoundCog } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const customerConfig: MasterEntityConfig = {
  key: 'customer',
  label: 'Customer',
  labelPlural: 'Customers',
  description: 'Institutional/bulk customer master for sales and billing (distinct from individual patients).',
  icon: UserRoundCog,
  section: 'Finance',
  codeField: 'customerCode',
  nameField: 'customerName',
  fields: [
    { key: 'customerCode', label: 'Customer Code', type: 'text', required: true },
    { key: 'customerName', label: 'Customer Name', type: 'text', required: true },
    { key: 'contactPerson', label: 'Contact Person', type: 'text' },
    { key: 'phone', label: 'Phone', type: 'text' },
    { key: 'email', label: 'Email', type: 'text' },
    { key: 'country', label: 'Country', type: 'text', helpText: 'Free-text for now — no Country master exists yet.' },
    { key: 'paymentTermId', label: 'Payment Terms', type: 'reference', referenceEntityKey: 'paymentTerms' },
  ],
};
