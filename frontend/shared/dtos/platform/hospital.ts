/** Mirrors HMS.Modules.Platform.Contracts.CreateHospitalRequest. */
export interface CreateHospitalRequest {
  hospitalName: string;
  hospitalCode: string;
  mobileNumber: string;
  address: string;
  city: string;
  state: string;
  pincode: string;
  superAdminUsername: string;
  superAdminFirstName: string;
  superAdminLastName: string;
  superAdminEmail: string;
  superAdminPhoneNumber: string;
  superAdminPassword: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.CreateHospitalResponse. */
export interface CreateHospitalResponse {
  id: string;
  hospitalName: string;
  hospitalCode: string;
  status: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.TenantListItemResponse. */
export interface TenantListItemResponse {
  id: string;
  hospitalName: string;
  hospitalCode: string;
  status: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.TenantListQuery. */
export interface TenantListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.UpdateTenantStatusRequest. */
export interface UpdateTenantStatusRequest {
  status: string;
}

/** Mirrors HMS.Modules.Platform.Contracts.TenantDashboardStatsResponse. */
export interface TenantDashboardStatsResponse {
  total: number;
  active: number;
  inactive: number;
  /** A tenant-provisioning failure whose rollback also failed, so an orphaned database may
   * still exist. Zero in the normal case; any nonzero value needs investigation. */
  provisioningAlertCount: number;
}
