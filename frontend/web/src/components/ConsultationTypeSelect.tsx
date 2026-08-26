import { useQuery } from '@tanstack/react-query';
import { formatCurrency } from '@/features/billing';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { consultationTypesApi } from '../services/apiClient';

interface ConsultationTypeSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Consultation type picker for Patient Registration's Registration Details tab, backed by
 * the real GET /api/v1/masters/consultation-types list — each option shows the type's name
 * and its standard fee (e.g. "Doctor's Consultation (In-house) - Regular — ₹200"), or "Amount
 * to be filled" for categories with no fixed rate (Amount left unset in the master record). */
export function ConsultationTypeSelect({ id, value, onValueChange, ariaLabel = 'Consultation type', disabled }: ConsultationTypeSelectProps) {
  const { data } = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? []).map((consultationType) => ({
    value: consultationType.id,
    label: `${consultationType.name} — ${consultationType.amount != null ? formatCurrency(consultationType.amount) : 'Amount to be filled'}`,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select consultation type…"
      searchPlaceholder="Search by name…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
