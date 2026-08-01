import { Factory } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const manufacturerConfig: MasterEntityConfig = {
  key: 'manufacturer',
  label: 'Manufacturer',
  labelPlural: 'Manufacturers',
  description: 'Manufacturer master with contact details, used across Inventory & ERP.',
  icon: Factory,
  section: 'Brand & Manufacturer',
  codeField: 'manufacturerCode',
  nameField: 'manufacturerName',
  fields: [
    { key: 'manufacturerCode', label: 'Manufacturer Code', type: 'text', required: true },
    { key: 'manufacturerName', label: 'Manufacturer Name', type: 'text', required: true },
    { key: 'contactPerson', label: 'Contact Person', type: 'text' },
    { key: 'phone', label: 'Phone', type: 'text' },
    { key: 'email', label: 'Email', type: 'text' },
    { key: 'country', label: 'Country', type: 'text', helpText: 'Free-text for now — no Country master exists yet.' },
  ],
  seed: [
    { id: 'manufacturer-001', manufacturerCode: 'CIPLA-MFG', manufacturerName: 'Cipla Ltd.', contactPerson: 'Rohan Mehta', phone: '+91 22 2482 6000', email: 'contact@cipla.com', country: 'India' },
    { id: 'manufacturer-002', manufacturerCode: 'BD-MFG', manufacturerName: 'Becton Dickinson & Co.', contactPerson: 'Sarah Lin', phone: '+1 201 847 6800', email: 'contact@bd.com', country: 'United States' },
  ],
};
