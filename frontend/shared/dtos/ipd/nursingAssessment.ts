/** Mirrors HMS.Modules.IPD.Contracts.CreateNursingAssessmentRequest. */
export interface CreateNursingAssessmentRequest {
  assessedAt: string;
  generalCondition?: string | null;
  consciousnessLevel?: string | null;
  mobility?: string | null;
  nutritionStatus?: string | null;
  fallRisk?: string | null;
  pressureSoreRisk?: string | null;
  skinCondition?: string | null;
  painScore?: number | null;
  notes?: string | null;
  assessedByUserId?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.NursingAssessmentResponse. */
export interface NursingAssessment {
  id: string;
  admissionId: string;
  assessedAt: string;
  generalCondition?: string | null;
  consciousnessLevel?: string | null;
  mobility?: string | null;
  nutritionStatus?: string | null;
  fallRisk?: string | null;
  pressureSoreRisk?: string | null;
  skinCondition?: string | null;
  painScore?: number | null;
  notes?: string | null;
  assessedByUserId?: string | null;
  createdAt: string;
}
