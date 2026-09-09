import type { MedicationOrderStatus } from '../../enums';

/** Mirrors HMS.Modules.IPD.Contracts.CreateMedicationOrderRequest. */
export interface CreateMedicationOrderRequest {
  drugName: string;
  dose: string;
  route: string;
  frequency: string;
  startDate: string;
  endDate?: string | null;
  instructions?: string | null;
  orderedAt: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.DiscontinueMedicationOrderRequest. */
export interface DiscontinueMedicationOrderRequest {
  reason?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.MedicationOrderResponse. */
export interface MedicationOrder {
  id: string;
  admissionId: string;
  drugName: string;
  dose: string;
  route: string;
  frequency: string;
  startDate: string;
  endDate?: string | null;
  instructions?: string | null;
  orderedAt: string;
  orderedByUserId?: string | null;
  status: MedicationOrderStatus;
  discontinuedAt?: string | null;
  discontinuedReason?: string | null;
  createdAt: string;
}
