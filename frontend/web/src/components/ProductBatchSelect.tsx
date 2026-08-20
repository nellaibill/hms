import { useProductBatchesQuery } from '@/features/pharmacy/product-lookup';
import { SearchableSelect } from '@/components/ui/searchable-select';

interface ProductBatchSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Scopes the batch list to this product — required, since a batch only makes sense once
   * a product has been chosen. */
  productId: string | undefined;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Batch picker for Pharmacy's Stock Receipt / Dispense forms, backed by the real
 * GET /api/v1/products/{productId}/batches list (active batches only) — mirrors
 * BedSelect's ward-scoped shape. */
export function ProductBatchSelect({ id, value, onValueChange, productId, ariaLabel = 'Batch', disabled }: ProductBatchSelectProps) {
  const { data } = useProductBatchesQuery(productId, { pageSize: 100, isActive: true, sort: 'expiryDate' });

  const options = (data?.items ?? []).map((batch) => ({
    value: batch.id,
    label: `${batch.batchNo} — expires ${new Date(batch.expiryDate).toLocaleDateString('en-IN')}`,
    keywords: batch.batchNo,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={productId ? 'Select batch…' : 'Select a product first…'}
      searchPlaceholder="Search by batch number…"
      ariaLabel={ariaLabel}
      disabled={disabled || !productId}
    />
  );
}
