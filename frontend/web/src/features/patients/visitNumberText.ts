import type { PatientVisit } from '@hms/shared';

/** What a visit's number reads as: its OP No/IP No, what it's still waiting for (ADR-103 /
 * ADR-104), or null for an old visit recorded before these numbers existed. */
export function visitNumberText(visit: PatientVisit): string | null {
  const isOp = visit.visitType === 'OP';
  if (visit.visitNumber) return `${isOp ? 'OP No' : 'IP No'} ${visit.visitNumber}`;
  if (visit.visitNumberPending) return isOp ? 'Fee pending' : 'Advance pending';
  return null;
}
