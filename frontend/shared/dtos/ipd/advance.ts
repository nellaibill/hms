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
  /** The admission's linked visit's IP No — set only on the record-advance response once it has
   * one, which a paid advance is what issues (ADR-104). */
  ipNumber?: string | null;
}
