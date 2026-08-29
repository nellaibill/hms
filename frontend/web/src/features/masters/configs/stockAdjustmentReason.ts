import { ClipboardEdit } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const stockAdjustmentReasonConfig: MasterEntityConfig = {
  key: 'stockAdjustmentReason',
  label: 'Stock Adjustment Reason',
  labelPlural: 'Stock Adjustment Reasons',
  description: 'Lookup reasons for manual stock adjustments (e.g. Damage, Expiry, Recount).',
  icon: ClipboardEdit,
  section: 'Pharmacy & Inventory',
  codeField: 'reasonCode',
  nameField: 'reasonName',
  fields: [
    { key: 'reasonCode', label: 'Reason Code', type: 'text', required: true },
    { key: 'reasonName', label: 'Reason Name', type: 'text', required: true },
    { key: 'affectsValuation', label: 'Affects Valuation', type: 'boolean', defaultValue: false },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
};
