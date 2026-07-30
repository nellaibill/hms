import { CreditCard } from 'lucide-react';
import type { MasterEntityConfig } from '../engine/types';

export const paymentMethodConfig: MasterEntityConfig = {
  key: 'paymentMethod',
  label: 'Payment Method',
  labelPlural: 'Payment Methods',
  description: 'Methods available for recording payments (e.g. Cash, Card, Bank Transfer).',
  icon: CreditCard,
  section: 'Finance & Payment',
  codeField: 'methodCode',
  nameField: 'methodName',
  fields: [
    { key: 'methodCode', label: 'Method Code', type: 'text', required: true },
    { key: 'methodName', label: 'Method Name', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  seed: [
    { id: 'payment-method-001', methodCode: 'CASH', methodName: 'Cash' },
    { id: 'payment-method-002', methodCode: 'CARD', methodName: 'Card' },
    { id: 'payment-method-003', methodCode: 'BANK', methodName: 'Bank Transfer' },
    { id: 'payment-method-004', methodCode: 'CHEQUE', methodName: 'Cheque' },
  ],
};
