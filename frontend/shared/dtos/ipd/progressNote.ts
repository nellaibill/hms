/** Mirrors HMS.Modules.IPD.Contracts.CreateProgressNoteRequest. */
export interface CreateProgressNoteRequest {
  noteDateTime: string;
  clinicalCondition?: string | null;
  progress?: string | null;
  diagnosis?: string | null;
  assessment?: string | null;
  plan?: string | null;
  instructions?: string | null;
  authorUserId?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.ProgressNoteResponse. */
export interface ProgressNote {
  id: string;
  admissionId: string;
  noteDateTime: string;
  clinicalCondition?: string | null;
  progress?: string | null;
  diagnosis?: string | null;
  assessment?: string | null;
  plan?: string | null;
  instructions?: string | null;
  authorUserId?: string | null;
  createdAt: string;
}
