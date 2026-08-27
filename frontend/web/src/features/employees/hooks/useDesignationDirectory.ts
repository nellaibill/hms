import { useQuery } from '@tanstack/react-query';
import { mastersApi } from '@/services/apiClient';

/** Shares the exact ['masters','designation','select-list'] cache DesignationSelect uses
 * elsewhere in HR, so this feature never issues a redundant designation fetch of its own. */
function useDesignationList() {
  return useQuery({
    queryKey: ['masters', 'designation', 'select-list'],
    queryFn: () => mastersApi.list('designation', { pageSize: 100, isActive: true }),
  });
}

/** DesignationId -> display name, for resolving the Employees list table's Designation
 * column — EmployeeResponse.designationName is only populated on the single-record GET,
 * never on the paged list (see EmployeeResponse's own doc comment), so the list view
 * resolves names client-side from this small, cached lookup instead. */
export function useDesignationNameById(): Map<string, string> {
  const { data } = useDesignationList();
  return new Map((data?.items ?? []).map((designation) => [String(designation.id), String(designation.name)]));
}
