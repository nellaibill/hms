import { useQuery } from '@tanstack/react-query';
import { consultantsApi } from '../services/apiClient';

interface ConsultantNameProps {
  consultantId: string;
}

/** Resolves a ConsultantId to a display name via the same cached ['consultants','select-list']
 * query ConsultantSelect populates, so list/detail views don't show raw GUIDs. Falls back to a
 * truncated id while loading or if the id isn't a known consultant. */
// A handful of pre-existing patient registrations predate Consultant becoming a real
// Masters reference and were backfilled to this sentinel — see the AddDepartmentConsultantId
// ToPatientRegistration migration. No real consultant will ever have this id.
const UNSET_ID = '00000000-0000-0000-0000-000000000000';

export function ConsultantName({ consultantId }: ConsultantNameProps) {
  const { data } = useQuery({
    queryKey: ['consultants', 'select-list'],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true }),
  });

  if (consultantId === UNSET_ID) {
    return <span className="text-muted-foreground">—</span>;
  }

  const consultant = data?.items.find((item) => item.id === consultantId);
  if (consultant) {
    return <>{consultant.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{consultantId.slice(0, 8)}…</span>;
}
