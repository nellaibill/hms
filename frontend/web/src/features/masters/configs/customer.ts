import { UserRoundCog } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const customerConfig: MasterEntityConfig = {
  key: 'customer',
  label: 'Customer',
  labelPlural: 'Customers',
  description: 'Institutional/bulk customer master for sales and billing (distinct from individual patients).',
  icon: UserRoundCog,
  section: 'Business Partners',
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
  seed: [
    {
      id: 'customer-001',
      customerCode: 'CUST-INS01',
      customerName: 'Sunrise Insurance TPA',
      contactPerson: 'Divya Menon',
      phone: '+91 44 1122 3344',
      email: 'claims@sunriseinsurance.example',
      country: 'India',
      paymentTermId: 'payment-terms-001',
    },
  ],
};
