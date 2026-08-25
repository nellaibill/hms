import { useQuery } from '@tanstack/react-query';
import { statesApi } from '../services/apiClient';

interface DistrictNameProps {
  stateId: string;
  districtId: string;
}

/** Resolves a DistrictId to a display name via the same cached ['districts','select-list',stateId]
 * query DistrictSelect populates, so detail views don't show a raw GUID. Mirrors DepartmentName. */
export function DistrictName({ stateId, districtId }: DistrictNameProps) {
  const { data } = useQuery({
    queryKey: ['districts', 'select-list', stateId],
    queryFn: () => statesApi.getDistricts(stateId),
    enabled: Boolean(stateId),
  });

  const district = data?.find((item) => item.id === districtId);
  if (district) {
    return <>{district.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{districtId.slice(0, 8)}…</span>;
}
