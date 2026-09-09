/** Mirrors HMS.Modules.IPD.Contracts.CreateMedicationAdministrationRequest. */
export interface CreateMedicationAdministrationRequest {
  scheduledTime: string;
  wasGiven: boolean;
  administeredAt?: string | null;
  reason?: string | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.MedicationAdministrationResponse. */
export interface MedicationAdministration {
  id: string;
  medicationOrderId: string;
  scheduledTime: string;
  wasGiven: boolean;
  administeredAt?: string | null;
  reason?: string | null;
  remarks?: string | null;
  recordedByUserId?: string | null;
  createdAt: string;
}
