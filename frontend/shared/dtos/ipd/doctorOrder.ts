import type { DoctorOrderStatus, DoctorOrderType } from '../../enums';

/** Mirrors HMS.Modules.IPD.Contracts.CreateDoctorOrderRequest. */
export interface CreateDoctorOrderRequest {
  orderType: DoctorOrderType;
  description: string;
  instructions?: string | null;
  orderedAt: string;
}

/** Mirrors HMS.Modules.IPD.Contracts.CancelDoctorOrderRequest. */
export interface CancelDoctorOrderRequest {
  reason?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.DoctorOrderResponse. */
export interface DoctorOrder {
  id: string;
  admissionId: string;
  orderType: DoctorOrderType;
  description: string;
  instructions?: string | null;
  orderedAt: string;
  orderedByUserId?: string | null;
  status: DoctorOrderStatus;
  completedAt?: string | null;
  cancelledAt?: string | null;
  cancellationReason?: string | null;
  createdAt: string;
}
