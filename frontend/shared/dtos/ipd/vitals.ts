/** Mirrors HMS.Modules.IPD.Contracts.CreateVitalsReadingRequest. */
export interface CreateVitalsReadingRequest {
  recordedAt: string;
  temperatureF?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  bloodPressureSystolic?: number | null;
  bloodPressureDiastolic?: number | null;
  spO2Percent?: number | null;
  weightKg?: number | null;
  heightCm?: number | null;
  painScore?: number | null;
  bloodGlucoseMgDl?: number | null;
  recordedByUserId?: string | null;
  notes?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.VitalsReadingResponse. */
export interface VitalsReading {
  id: string;
  admissionId: string;
  recordedAt: string;
  temperatureF?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  bloodPressureSystolic?: number | null;
  bloodPressureDiastolic?: number | null;
  spO2Percent?: number | null;
  weightKg?: number | null;
  heightCm?: number | null;
  painScore?: number | null;
  bloodGlucoseMgDl?: number | null;
  recordedByUserId?: string | null;
  notes?: string | null;
  createdAt: string;
}
