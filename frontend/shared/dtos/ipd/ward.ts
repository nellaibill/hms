import type { WardType } from '../../enums';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.IPD.Contracts.WardResponse. */
export interface Ward {
  id: string;
  code: string;
  name: string;
  departmentId: string;
  wardType: WardType;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.CreateWardRequest. */
export interface CreateWardRequest {
  code: string;
  name: string;
  departmentId: string;
  wardType: WardType;
  isActive: boolean;
}

/** Mirrors HMS.Modules.IPD.Contracts.UpdateWardRequest — no Code, matching the backend
 * (Code is Ward's natural key, set only at creation). */
export interface UpdateWardRequest {
  name: string;
  departmentId: string;
  wardType: WardType;
  isActive: boolean;
}

/** Mirrors HMS.Modules.IPD.Contracts.WardListQuery. */
export interface WardListQuery extends PagedQuery {
  isActive?: boolean;
  departmentId?: string;
}
