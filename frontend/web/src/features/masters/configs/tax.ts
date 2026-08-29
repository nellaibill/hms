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
  section: 'Finance',
  codeField: 'taxCode',
  nameField: 'taxName',
  fields: [
    { key: 'taxCode', label: 'Tax Code', type: 'text', required: true },
    { key: 'taxName', label: 'Tax Name', type: 'text', required: true },
    { key: 'taxType', label: 'Tax Type', type: 'select', options: TAX_TYPE_OPTIONS, defaultValue: 'GST' },
    { key: 'ratePercent', label: 'Rate (%)', type: 'decimal', required: true, min: 0.01, step: 0.01, helpText: 'Rate must be greater than 0.' },
    { key: 'isInclusive', label: 'Inclusive of Price', type: 'boolean', defaultValue: false },
  ],
};
