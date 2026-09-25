import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { formatCurrency } from '@/features/billing';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { consultantsApi, consultationTypesApi } from '../services/apiClient';

interface ConsultationTypeSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** The department/consultant this consultation is with. The offered types are exactly the
   * consultant's own Consultation Types (Masters → Consultant Edit) — the one rule every page
   * that picks a consultation type (New Registration, Add Visit, Billing) follows, and the same
   * rule the visit/invoice APIs enforce server-side. */
  departmentId: string | undefined;
  consultantId: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Consultation type picker backed by the real GET /api/v1/masters/consultation-types list,
 * scoped to the selected consultant — each option shows the type's name and its standard fee
 * (e.g. "Doctor's Consultation (In-house) - Regular — ₹200"), or "Amount to be filled" for
 * categories with no fixed rate (Amount left unset in the master record). Types keep the
 * master list's own order.
 *
 * Owns the dependent-field behavior too, so no page has to repeat it: nothing is pickable until
 * a consultant is chosen, and a selected type is cleared as soon as it's no longer one the
 * current consultant offers (consultant changed, cleared, or its department changed). */
export function ConsultationTypeSelect({ id, value, onValueChange, departmentId, consultantId, ariaLabel = 'Consultation type', disabled }: ConsultationTypeSelectProps) {
  const { data: consultationTypes } = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
  });
  // Same query key ConsultantSelect uses for this department, so this is a cache read.
  const { data: consultants, isSuccess: consultantsLoaded } = useQuery({
    queryKey: ['consultants', 'select-list', departmentId],
    queryFn: () => consultantsApi.getConsultants({ pageSize: 100, isActive: true, departmentId }),
    enabled: Boolean(departmentId),
  });

  const selectedConsultant = consultantId ? consultants?.items.find((c) => c.id === consultantId) : undefined;
  const allowedIds = selectedConsultant?.consultationTypeCharges.map((c) => c.consultationTypeId);

  const options = allowedIds
    ? (consultationTypes?.items ?? [])
        .filter((consultationType) => allowedIds.includes(consultationType.id))
        .map((consultationType) => ({
          value: consultationType.id,
          label: `${consultationType.name} — ${consultationType.amount != null ? formatCurrency(consultationType.amount) : 'Amount to be filled'}`,
        }))
    : [];

  // Only once the consultant list has actually loaded — a prefilled value (e.g. Billing's
  // "from registration" row) must not be wiped while its consultant is still being fetched.
  const outOfScope = Boolean(value) && (!consultantId || (consultantsLoaded && !(allowedIds ?? []).includes(value)));
  useEffect(() => {
    if (outOfScope) onValueChange('');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [outOfScope]);

  const placeholder = !consultantId
    ? 'Select a consultant first…'
    : !selectedConsultant
      ? consultantsLoaded
        ? 'Consultant not active in this department'
        : 'Loading…'
      : options.length === 0
        ? 'Consultant offers no consultation types'
        : 'Select consultation type…';

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={placeholder}
      searchPlaceholder="Search by name…"
      ariaLabel={ariaLabel}
      disabled={disabled || options.length === 0}
    />
  );
}
