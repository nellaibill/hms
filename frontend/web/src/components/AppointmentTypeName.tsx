import { useQuery } from '@tanstack/react-query';
import { appointmentTypesApi } from '../services/apiClient';

interface AppointmentTypeNameProps {
  appointmentTypeId: string | null | undefined;
}

/** Resolves an AppointmentTypeId to a display name via the same cached
 * ['appointmentTypes','select-list'] query AppointmentTypeSelect populates, so list/detail
 * views don't show raw GUIDs. Unlike Department/ConsultantName, AppointmentTypeId is
 * genuinely optional (IP/Emergency/Day-care encounters never have one) — renders "—" for
 * a missing id rather than a sentinel/truncated-id fallback. */
export function AppointmentTypeName({ appointmentTypeId }: AppointmentTypeNameProps) {
  const { data } = useQuery({
    queryKey: ['appointmentTypes', 'select-list'],
    queryFn: () => appointmentTypesApi.getAppointmentTypes({ pageSize: 100, isActive: true }),
    enabled: Boolean(appointmentTypeId),
  });

  if (!appointmentTypeId) {
    return <span className="text-muted-foreground">—</span>;
  }

  const appointmentType = data?.items.find((item) => item.id === appointmentTypeId);
  if (appointmentType) {
    return <>{appointmentType.name}</>;
  }

  return <span className="font-mono text-xs text-muted-foreground">{appointmentTypeId.slice(0, 8)}…</span>;
}
