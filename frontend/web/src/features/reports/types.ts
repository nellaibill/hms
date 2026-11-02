import type { PaymentStatus } from '@/features/billing';

export interface IncomeReportRow {
  id: string;
  invoiceNumber?: string;
  date: string;
  patientId: string;
  patientName: string;
  patientUhid: string;
  billingTypes: string;
  /** The bill's approved discount (ADR-109/111) — `amount` is already net of it. */
  discount: number;
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
