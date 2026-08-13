import type { ChargeType } from '../../enums';

/** Mirrors HMS.Modules.IPD.Contracts.CreateAdmissionChargeRequest. */
export interface CreateAdmissionChargeRequest {
  chargeType: ChargeType;
  amount: number;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.AdmissionChargeResponse. */
export interface AdmissionCharge {
  id: string;
  admissionId: string;
  chargeType: ChargeType;
  amount: number;
  remarks?: string | null;
  createdAt: string;
}
