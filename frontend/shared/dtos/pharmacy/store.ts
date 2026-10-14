import type { StoreType } from '../../enums';

/** Mirrors HMS.Modules.Pharmacy.Contracts.StoreResponse — a place the pharmacy holds stock. */
export interface StoreResponse {
  id: string;
  code: string;
  name: string;
  storeType: StoreType;
  /** The store used when a stock request names none. Exactly one store is the default. */
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.CreateStoreRequest. */
export interface CreateStoreRequest {
  code: string;
  name: string;
  storeType: StoreType;
  isDefault: boolean;
}

/** Mirrors HMS.Modules.Pharmacy.Contracts.UpdateStoreRequest — code is immutable. `isDefault: true`
 * moves the default here; `false` leaves the default where it is. */
export interface UpdateStoreRequest {
  name: string;
  storeType: StoreType;
  isActive: boolean;
  isDefault: boolean;
}
