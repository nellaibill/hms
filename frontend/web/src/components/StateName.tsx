import { useQuery } from '@tanstack/react-query';
import { statesApi } from '../services/apiClient';

interface StateNameProps {
  stateId: string;
}

/** Resolves a StateId to a display name via the same cached ['states','select-list'] query
 * StateSelect populates, so detail views don't show a raw GUID. Mirrors DepartmentName. */
export function StateName({ stateId }: StateNameProps) {
  const { data } = useQuery({
    queryKey: ['states', 'select-list'],
    queryFn: () => statesApi.getStates(),
  });

  const state = data?.find((item) => item.id === stateId);
  if (state) {
    return <>{state.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{stateId.slice(0, 8)}…</span>;
}
