import { useQuery } from '@tanstack/react-query';
import { departmentsApi } from '@/services/apiClient';

/** Shares the exact ['departments','select-list'] cache DepartmentSelect/DepartmentName use
 * elsewhere in HR, so this feature never issues a redundant department fetch of its own. */
function useDepartmentList() {
  return useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });
}

/** Department display names, for the sidebar/filter-panel department dropdowns. Deduped —
 * seed/test data can contain departments that share a display name, which would otherwise
 * produce duplicate React keys in the dropdown. */
export function useDepartmentNames(): string[] {
  const { data } = useDepartmentList();
  return Array.from(new Set((data?.items ?? []).map((department) => department.name)));
}

/** DepartmentId -> display name, for resolving events' DepartmentId onto a readable label. */
export function useDepartmentNameById(): Map<string, string> {
  const { data } = useDepartmentList();
  return new Map((data?.items ?? []).map((department) => [department.id, department.name]));
}
