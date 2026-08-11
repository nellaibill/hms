import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { appointmentTypesApi } from '../services/apiClient';

interface AppointmentTypeSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Appointment type picker for Patient Registration's OP encounters, backed by the real
 * GET /api/v1/masters/appointment-types list. */
export function AppointmentTypeSelect({ id, value, onValueChange, ariaLabel = 'Appointment type', disabled }: AppointmentTypeSelectProps) {
  const { data } = useQuery({
    queryKey: ['appointmentTypes', 'select-list'],
    queryFn: () => appointmentTypesApi.getAppointmentTypes({ pageSize: 100, isActive: true }),
  });

  // Always suffix the code — see DepartmentSelect's identical comment. Code is the only
  // guaranteed-unique field.
  const options = (data?.items ?? []).map((appointmentType) => ({
    value: appointmentType.id,
    label: `${appointmentType.name} (${appointmentType.code})`,
    keywords: appointmentType.code,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select appointment type…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
