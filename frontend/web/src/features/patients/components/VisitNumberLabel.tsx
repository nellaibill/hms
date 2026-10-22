import type { PatientVisit } from '@hms/shared';
import { Badge } from '@/components/ui/badge';

/** The visit's OP No/IP No, or a "Fee pending"/"Advance pending" badge — nothing for an old
 * visit recorded before these numbers existed. */
export function VisitNumberLabel({ visit }: { visit: PatientVisit }) {
  const isOp = visit.visitType === 'OP';
  if (visit.visitNumber) {
    return (
      <span className="font-semibold text-foreground">
        {isOp ? 'OP No' : 'IP No'} <span className="font-mono">{visit.visitNumber}</span>
      </span>
    );
  }
  if (visit.visitNumberPending) {
    return (
      <Badge variant="warning" className="text-[10px]">
        {isOp ? 'Fee pending' : 'Advance pending'}
      </Badge>
    );
  }
  return null;
}
