/** Mirrors HMS.Modules.Radiology.Contracts.XrayAiAnalysisResponse — a saved, AI-generated read of
 * one stored X-ray image. Never a diagnosis: always render `disclaimer` beside `analysis`, and show
 * `isReviewed` so a clinician can tell an unreviewed draft from a confirmed one. */
export interface XrayAiAnalysisResponse {
  id: string;
  patientId: string;
  documentId: string;
  /** The model's reply in the fixed section layout (ANATOMICAL REGION: … CLINICAL REVIEW:). */
  analysis: string;
  model: string;
  generatedAtUtc: string;
  generatedByUserId?: string | null;
  isReviewed: boolean;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
  disclaimer: string;
}
