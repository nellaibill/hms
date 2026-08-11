import { useQuery } from '@tanstack/react-query';
import { departmentsApi } from '../services/apiClient';

interface DepartmentNameProps {
  departmentId: string;
}

/** Resolves a DepartmentId to a display name via the same cached ['departments','select-list']
 * query DepartmentSelect populates, so list/detail views don't show raw GUIDs. Falls back to a
 * truncated id while loading or if the id isn't a known department. */
// A handful of pre-existing patient registrations predate Department becoming a real
// Masters reference and were backfilled to this sentinel — see the AddDepartmentConsultantId
// ToPatientRegistration migration. No real department will ever have this id.
const UNSET_ID = '00000000-0000-0000-0000-000000000000';

export function DepartmentName({ departmentId }: DepartmentNameProps) {
  const { data } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  if (departmentId === UNSET_ID) {
    return <span className="text-muted-foreground">—</span>;
  }

  const department = data?.items.find((item) => item.id === departmentId);
  if (department) {
    return <>{department.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{departmentId.slice(0, 8)}…</span>;
}
