import { Warehouse } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const warehouseConfig: MasterEntityConfig = {
  key: 'warehouse',
  label: 'Warehouse',
  labelPlural: 'Warehouses',
  description: 'Physical storage facilities used across Inventory & ERP.',
  icon: Warehouse,
  section: 'Warehouse & Location',
  codeField: 'warehouseCode',
  nameField: 'warehouseName',
  fields: [
    { key: 'warehouseCode', label: 'Warehouse Code', type: 'text', required: true },
    { key: 'warehouseName', label: 'Warehouse Name', type: 'text', required: true },
    { key: 'address', label: 'Address', type: 'textarea' },
    { key: 'country', label: 'Country', type: 'text', helpText: 'Free-text for now — no Country master exists yet.' },
    { key: 'state', label: 'State', type: 'text', helpText: 'Free-text for now — no State master exists yet.' },
  ],
};
