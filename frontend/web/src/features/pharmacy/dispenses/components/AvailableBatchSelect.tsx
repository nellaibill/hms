import type { AvailableBatchResponse } from '@hms/shared';
import { useEffect } from 'react';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { useAvailableBatchesQuery } from '@/features/pharmacy/stock-balances';

interface AvailableBatchSelectProps {
  id: string;
  productId: string | undefined;
  /** The store being dispensed from; omitted, the default store. */
  storeId?: string;
  value: string;
  onBatchChange: (batch: AvailableBatchResponse | null) => void;
  /** Batches of this product already in the cart — a product/batch can only be one cart
   * line, so offering them again would just lead to a rejected Add. */
  excludeBatchIds?: string[];
}

/**
 * Batch picker for dispensing: lists only what can actually be dispensed (active,
 * unexpired, in stock) with each batch's on-hand quantity, earliest expiry first — and
 * picks that first batch by default, so stock leaves in expiry order unless the pharmacist
 * deliberately chooses otherwise. Stock Receipt keeps the plain ProductBatchSelect, since
 * stock can be received into any batch, including an empty one.
 */
export function AvailableBatchSelect({ id, productId, storeId, value, onBatchChange, excludeBatchIds = [] }: AvailableBatchSelectProps) {
  const { data, isPending } = useAvailableBatchesQuery(productId, storeId);
  const batches = (data ?? []).filter((batch) => !excludeBatchIds.includes(batch.productBatchId));
  const firstBatchId = batches[0]?.productBatchId;

  useEffect(() => {
    if (!value && firstBatchId) {
      onBatchChange(batches[0]);
    }
    // Keyed on the ids only: `batches` is a fresh array every render, and onBatchChange an
    // inline callback — depending on either would re-run this on every render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value, firstBatchId]);

  const options = batches.map((batch) => ({
    value: batch.productBatchId,
    label: `${batch.batchNo} — exp ${new Date(batch.expiryDate).toLocaleDateString('en-IN')} — ${batch.quantityOnHand} on hand${batch.isExpiringSoon ? ' (expiring soon)' : ''}`,
    keywords: batch.batchNo,
  }));

  const loaded = Boolean(productId) && !isPending;
  const placeholder = !productId
    ? 'Select a product first…'
    : !loaded
      ? 'Loading batches…'
      : batches.length === 0
        ? 'No stock available'
        : 'Select batch…';

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={(batchId) => onBatchChange(batches.find((batch) => batch.productBatchId === batchId) ?? null)}
      options={options}
      placeholder={placeholder}
      searchPlaceholder="Search by batch number…"
      ariaLabel="Batch"
      disabled={!loaded || batches.length === 0}
    />
  );
}
