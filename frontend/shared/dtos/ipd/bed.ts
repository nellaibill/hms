import type { BedStatus } from '../../enums';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.IPD.Contracts.BedResponse. */
export interface Bed {
  id: string;
  wardId: string;
  bedNumber: string;
  bedType: string;
  status: BedStatus;
  isActive: boolean;
  dailyCharge: number;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.CreateBedRequest. */
export interface CreateBedRequest {
  wardId: string;
  bedNumber: string;
  bedType: string;
  status: BedStatus;
  isActive: boolean;
  dailyCharge: number;
}

/** Mirrors HMS.Modules.IPD.Contracts.UpdateBedRequest — no WardId/BedNumber, matching the
 * backend (natural-key fields, protected from change after creation). */
export interface UpdateBedRequest {
  bedType: string;
  status: BedStatus;
  isActive: boolean;
  dailyCharge: number;
}

/** Mirrors HMS.Modules.IPD.Contracts.BedListQuery. */
export interface BedListQuery extends PagedQuery {
  wardId?: string;
  status?: BedStatus;
  isActive?: boolean;
}
