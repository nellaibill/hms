import type { NursingShift } from '../../enums';

/** Mirrors HMS.Modules.IPD.Contracts.CreateNursingNoteRequest. */
export interface CreateNursingNoteRequest {
  noteDateTime: string;
  shift: NursingShift;
  observation?: string | null;
  intervention?: string | null;
  patientResponse?: string | null;
  remarks?: string | null;
  recordedByUserId?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.NursingNoteResponse. */
export interface NursingNote {
  id: string;
  admissionId: string;
  noteDateTime: string;
  shift: NursingShift;
  observation?: string | null;
  intervention?: string | null;
  patientResponse?: string | null;
  remarks?: string | null;
  recordedByUserId?: string | null;
  createdAt: string;
}
