import { Coins } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const currencyConfig: MasterEntityConfig = {
  key: 'currency',
  label: 'Currency',
  labelPlural: 'Currencies',
  description: 'Currencies used for pricing and payments.',
  icon: Coins,
  section: 'Finance & Payment',
  codeField: 'currencyCode',
  nameField: 'currencyName',
  fields: [
    { key: 'currencyCode', label: 'Currency Code', type: 'text', required: true, placeholder: 'e.g. INR' },
    { key: 'currencyName', label: 'Currency Name', type: 'text', required: true },
    { key: 'symbol', label: 'Symbol', type: 'text', required: true },
    { key: 'decimalPlaces', label: 'Decimal Places', type: 'number', defaultValue: 2, min: 0, max: 6, step: 1 },
  ],
  seed: [
    { id: 'currency-001', currencyCode: 'INR', currencyName: 'Indian Rupee', symbol: '₹', decimalPlaces: 2 },
    { id: 'currency-002', currencyCode: 'USD', currencyName: 'US Dollar', symbol: '$', decimalPlaces: 2 },
  ],
};
