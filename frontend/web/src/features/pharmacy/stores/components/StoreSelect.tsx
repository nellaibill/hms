import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useStoresQuery } from '../hooks/useStores';

interface StoreSelectProps {
  id: string;
  /** A store id, or '' — which means "all stores" when allowAll is set and "the default
   * store" otherwise, matching what the API does with an omitted storeId. */
  value: string;
  onValueChange: (storeId: string) => void;
  /** For filters: adds an "All stores" choice. Without it the control always names one store. */
  allowAll?: boolean;
  label?: string;
}

const ALL = '__all__';

/**
 * Store picker for Pharmacy's forms and filters. Renders nothing while the hospital has a
 * single store — there is nothing to choose, and most hospitals start that way — so the
 * screens stay exactly as simple as they were until a second store is added.
 */
export function StoreSelect({ id, value, onValueChange, allowAll = false, label = 'Store' }: StoreSelectProps) {
  const { data } = useStoresQuery();
  const stores = (data ?? []).filter((store) => store.isActive);

  if (stores.length <= 1) {
    return null;
  }

  const defaultStoreId = stores.find((store) => store.isDefault)?.id ?? stores[0].id;
  const selected = value || (allowAll ? ALL : defaultStoreId);

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Select value={selected} onValueChange={(next) => onValueChange(next === ALL ? '' : next)}>
        <SelectTrigger id={id} aria-label={label} className="min-w-[180px]">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {allowAll && <SelectItem value={ALL}>All stores</SelectItem>}
          {stores.map((store) => (
            <SelectItem key={store.id} value={store.id}>
              {store.name}
              {store.isDefault && !allowAll ? ' (default)' : ''}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
