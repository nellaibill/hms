import { Factory } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const manufacturerConfig: MasterEntityConfig = {
  key: 'manufacturer',
  label: 'Manufacturer',
  labelPlural: 'Manufacturers',
  description: 'Manufacturer master with contact details, used across Inventory & ERP.',
  icon: Factory,
  section: 'Pharmacy & Inventory',
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
};
