/** Mirrors HMS.Modules.Radiology.Contracts.XrayAiAnalysisResponse — an AI-generated, unreviewed
 * read of one stored X-ray image. Never a diagnosis: always render `disclaimer` beside `analysis`. */
export interface XrayAiAnalysisResponse {
  documentId: string;
  /** The model's reply in the fixed section layout (ANATOMICAL REGION: … CLINICAL REVIEW:). */
  analysis: string;
  model: string;
  generatedAtUtc: string;
  disclaimer: string;
}
