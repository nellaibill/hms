import { ClipboardEdit } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const stockAdjustmentReasonConfig: MasterEntityConfig = {
  key: 'stockAdjustmentReason',
  label: 'Stock Adjustment Reason',
  labelPlural: 'Stock Adjustment Reasons',
  description: 'Lookup reasons for manual stock adjustments (e.g. Damage, Expiry, Recount).',
  icon: ClipboardEdit,
  section: 'Inventory Lookup',
  codeField: 'reasonCode',
  nameField: 'reasonName',
  fields: [
    { key: 'reasonCode', label: 'Reason Code', type: 'text', required: true },
    { key: 'reasonName', label: 'Reason Name', type: 'text', required: true },
    { key: 'affectsValuation', label: 'Affects Valuation', type: 'boolean', defaultValue: false },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  seed: [
    { id: 'stock-adjustment-reason-001', reasonCode: 'DAMAGE', reasonName: 'Damaged', affectsValuation: true },
    { id: 'stock-adjustment-reason-002', reasonCode: 'EXPIRY', reasonName: 'Expired', affectsValuation: true },
    { id: 'stock-adjustment-reason-003', reasonCode: 'RECOUNT', reasonName: 'Physical Recount', affectsValuation: false },
    { id: 'stock-adjustment-reason-004', reasonCode: 'THEFT', reasonName: 'Theft / Loss', affectsValuation: true },
  ],
};
