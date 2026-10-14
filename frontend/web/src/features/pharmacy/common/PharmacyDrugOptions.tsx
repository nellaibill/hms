import { useQuery } from '@tanstack/react-query';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { pharmacyApi } from '@/services/apiClient';

interface PharmacyDrugOptionsProps {
  /** The id a text input's `list` attribute points at. */
  id: string;
  /** What has been typed so far. */
  search: string;
}

/**
 * Suggestions for a drug-name box, drawn from what the pharmacy stocks. Prescribing stays
 * free text — a doctor can write anything — but a name picked from here is one the pharmacy
 * counter will find at once, and each suggestion says whether it is in stock. Renders a
 * `<datalist>`, so it needs no layout and falls away silently for a user who may not read
 * the pharmacy catalog.
 */
export function PharmacyDrugOptions({ id, search }: PharmacyDrugOptionsProps) {
  const term = useDebouncedValue(search.trim(), 300);
  const { data } = useQuery({
    queryKey: ['pharmacy', 'catalog', 'drug-options', term],
    queryFn: () => pharmacyApi.searchCatalog(term, undefined, 10),
    enabled: term.length >= 3,
    staleTime: 60_000,
    retry: false,
  });

  return (
    <datalist id={id}>
      {data?.map((item) => (
        <option key={item.productId} value={item.productName}>
          {[item.genericName, item.availableQuantity > 0 ? 'in stock' : 'not in stock'].filter(Boolean).join(' · ')}
        </option>
      ))}
    </datalist>
  );
}
