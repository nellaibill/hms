import { useQuery } from '@tanstack/react-query';
import { departmentsApi } from '../services/apiClient';

interface DepartmentNameProps {
  departmentId: string;
}

/** Resolves a DepartmentId to a display name via the same cached ['departments','select-list']
 * query DepartmentSelect populates, so list/detail views don't show raw GUIDs. Falls back to a
 * truncated id while loading or if the id isn't a known department. */
export function DepartmentName({ departmentId }: DepartmentNameProps) {
  const { data } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  const department = data?.items.find((item) => item.id === departmentId);
  if (department) {
    return <>{department.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{departmentId.slice(0, 8)}…</span>;
}
