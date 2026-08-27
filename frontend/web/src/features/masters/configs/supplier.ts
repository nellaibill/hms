import { Truck } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const supplierConfig: MasterEntityConfig = {
  key: 'supplier',
  label: 'Supplier',
  labelPlural: 'Suppliers',
  description: 'Vendor master for purchasing goods and services.',
  icon: Truck,
  section: 'Business Partners',
  codeField: 'supplierCode',
  nameField: 'supplierName',
  fields: [
    { key: 'supplierCode', label: 'Supplier Code', type: 'text', required: true },
    { key: 'supplierName', label: 'Supplier Name', type: 'text', required: true },
    { key: 'contactPerson', label: 'Contact Person', type: 'text' },
    { key: 'phone', label: 'Phone', type: 'text' },
    { key: 'email', label: 'Email', type: 'text' },
    { key: 'taxId', label: 'Tax ID', type: 'text' },
    { key: 'country', label: 'Country', type: 'text', helpText: 'Free-text for now — no Country master exists yet.' },
    { key: 'paymentTermId', label: 'Payment Terms', type: 'reference', referenceEntityKey: 'paymentTerms' },
  ],
};
