import type { PaymentMethod } from '../../enums/billing';

/** Mirrors HMS.Modules.IPD.Contracts.CreateAdmissionAdvanceRequest. */
export interface CreateAdmissionAdvanceRequest {
  amount: number;
  method: PaymentMethod;
  referenceNumber?: string | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.AdmissionAdvanceResponse. */
export interface AdmissionAdvance {
  id: string;
  admissionId: string;
  amount: number;
  method: PaymentMethod;
  referenceNumber?: string | null;
  remarks?: string | null;
  createdAt: string;
}
