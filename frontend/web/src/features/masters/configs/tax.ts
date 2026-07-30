import { Percent } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const TAX_TYPE_OPTIONS = [
  { value: 'GST', label: 'GST' },
  { value: 'VAT', label: 'VAT' },
  { value: 'Sales Tax', label: 'Sales Tax' },
  { value: 'Excise', label: 'Excise' },
  { value: 'Other', label: 'Other' },
];

export const taxConfig: MasterEntityConfig = {
  key: 'tax',
  label: 'Tax',
  labelPlural: 'Taxes',
  description: 'Tax rates applied to purchases and sales (e.g. GST, VAT).',
  icon: Percent,
  section: 'Units & Tax',
  codeField: 'taxCode',
  nameField: 'taxName',
  fields: [
    { key: 'taxCode', label: 'Tax Code', type: 'text', required: true },
    { key: 'taxName', label: 'Tax Name', type: 'text', required: true },
    { key: 'taxType', label: 'Tax Type', type: 'select', options: TAX_TYPE_OPTIONS, defaultValue: 'GST' },
    { key: 'ratePercent', label: 'Rate (%)', type: 'decimal', required: true, min: 0.01, step: 0.01, helpText: 'Rate must be greater than 0.' },
    { key: 'isInclusive', label: 'Inclusive of Price', type: 'boolean', defaultValue: false },
  ],
  seed: [
    { id: 'tax-001', taxCode: 'GST5', taxName: 'GST 5%', taxType: 'GST', ratePercent: 5, isInclusive: false },
    { id: 'tax-002', taxCode: 'GST12', taxName: 'GST 12%', taxType: 'GST', ratePercent: 12, isInclusive: false },
    { id: 'tax-003', taxCode: 'GST18', taxName: 'GST 18%', taxType: 'GST', ratePercent: 18, isInclusive: false },
  ],
};
