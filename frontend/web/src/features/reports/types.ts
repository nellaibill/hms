import type { PaymentStatus } from '@/features/billing';

export interface IncomeReportRow {
  id: string;
  invoiceNumber?: string;
  date: string;
  patientName: string;
  patientUhid: string;
  billingTypes: string;
  amount: number;
  paymentStatus: PaymentStatus;
}

export interface ExpenseReportRow {
  id: string;
  date: string;
  category: string;
  description: string;
  amount: number;
}

export interface ReportDateRange {
  from: string;
  to: string;
}
