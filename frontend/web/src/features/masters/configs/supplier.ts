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
  seed: [
    {
      id: 'supplier-001',
      supplierCode: 'SUP-CIPLA',
      supplierName: 'Cipla Distribution Pvt. Ltd.',
      contactPerson: 'Anita Rao',
      phone: '+91 44 2345 6789',
      email: 'sales@cipladist.example',
      taxId: '33AAAAA0000A1Z5',
      country: 'India',
      paymentTermId: 'payment-terms-002',
    },
    {
      id: 'supplier-002',
      supplierCode: 'SUP-BD',
      supplierName: 'BD Medical Supplies',
      contactPerson: 'Karthik Iyer',
      phone: '+91 44 9876 5432',
      email: 'orders@bdmedical.example',
      taxId: '33BBBBB0000B1Z6',
      country: 'India',
      paymentTermId: 'payment-terms-003',
    },
  ],
};
