import { useQuery } from '@tanstack/react-query';
import { formatCurrency } from '@/features/billing';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { consultationTypesApi } from '../services/apiClient';

interface ConsultationTypeSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Scopes the offered types to a specific consultant's own Consultation Types selection
   * (Masters → Consultant Edit) — e.g. Consultation Billing, where only the consultation types
   * this specific doctor actually offers should be pickable. Omit to show every active type
   * (Patient Registration's Registration Details tab isn't scoped to a chosen consultant). */
  allowedIds?: string[];
  ariaLabel?: string;
  disabled?: boolean;
}

/** Consultation type picker for Patient Registration's Registration Details tab and Billing,
 * backed by the real GET /api/v1/masters/consultation-types list — each option shows the
 * type's name and its standard fee (e.g. "Doctor's Consultation (In-house) - Regular — ₹200"),
 * or "Amount to be filled" for categories with no fixed rate (Amount left unset in the master
 * record). */
export function ConsultationTypeSelect({ id, value, onValueChange, allowedIds, ariaLabel = 'Consultation type', disabled }: ConsultationTypeSelectProps) {
  const { data } = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? [])
    .filter((consultationType) => !allowedIds || allowedIds.includes(consultationType.id))
    .map((consultationType) => ({
      value: consultationType.id,
      label: `${consultationType.name} — ${consultationType.amount != null ? formatCurrency(consultationType.amount) : 'Amount to be filled'}`,
    }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={allowedIds && allowedIds.length === 0 ? 'Consultant offers no consultation types' : 'Select consultation type…'}
      searchPlaceholder="Search by name…"
      ariaLabel={ariaLabel}
      disabled={disabled || (allowedIds && allowedIds.length === 0)}
    />
  );
}
