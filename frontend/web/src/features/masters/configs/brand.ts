import { Tag } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const brandConfig: MasterEntityConfig = {
  key: 'brand',
  label: 'Brand',
  labelPlural: 'Brands',
  description: 'Product brand master used across Inventory & ERP.',
  icon: Tag,
  section: 'Brand & Manufacturer',
  codeField: 'brandCode',
  nameField: 'brandName',
  fields: [
    { key: 'brandCode', label: 'Brand Code', type: 'text', required: true },
    { key: 'brandName', label: 'Brand Name', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
};
