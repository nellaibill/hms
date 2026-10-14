import type { Product, ReturnableBatchResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { forwardRef, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { pharmacyApi, productsApi } from '@/services/apiClient';
import { daysUntil, expiryLabel, quantity } from './format';
import { LookupInput, type LookupInputHandle } from './LookupInput';

interface StoreBatchPickerProps {
  /** The store whose shelves are being picked from. '' is the default store. */
  storeId: string;
  /** Batches already on the document, shown as taken. */
  pickedBatchIds: string[];
  onPick: (batch: ReturnableBatchResponse) => void;
  placeholder?: string;
}

async function searchProducts(term: string): Promise<Product[]> {
  return (await productsApi.getProducts({ search: term, pageSize: 20, sort: 'productName' })).items;
}

/**
 * Finds a product, then shows every batch of it the store holds — expired ones included —
 * to add to a stock document with one click. Shared by the documents that act on stock
 * already on the shelf: returns to suppliers, adjustments and transfers.
 */
export const StoreBatchPicker = forwardRef<LookupInputHandle, StoreBatchPickerProps>(function StoreBatchPicker({ storeId, pickedBatchIds, onPick, placeholder }, ref) {
  const [product, setProduct] = useState<Product | null>(null);

  const { data: batches, isFetching } = useQuery({
    queryKey: ['pharmacy', 'returnable-batches', product?.id, storeId],
    queryFn: () => pharmacyApi.getReturnableBatches(product!.id, storeId || undefined),
    enabled: Boolean(product),
  });

  return (
    <section className="rounded-lg border border-primary/30 bg-primary/5 p-3">
      <LookupInput<Product>
        ref={ref}
        queryKey={['pharmacy', 'store-batch-picker', 'products']}
        search={searchProducts}
        getKey={(candidate) => candidate.id}
        onSelect={setProduct}
        ariaLabel="Product"
        placeholder={placeholder ?? 'Find a product…'}
        displayValue={product?.productName}
        emptyText="No product found."
        renderItem={(candidate) => (
          <span>
            <span className="font-medium">{candidate.productName}</span>
            {candidate.genericName && <span className="ml-2 text-xs text-muted-foreground">{candidate.genericName}</span>}
          </span>
        )}
      />
      {product && (
        <div className="mt-2 flex flex-wrap gap-2">
          {isFetching && <span className="text-sm text-muted-foreground">Loading batches…</span>}
          {batches?.length === 0 && <span className="text-sm text-muted-foreground">No stock of {product.productName} in this store.</span>}
          {batches?.map((batch) => {
            const days = daysUntil(batch.expiryDate);
            return (
              <button
                key={batch.productBatchId}
                type="button"
                disabled={pickedBatchIds.includes(batch.productBatchId)}
                onClick={() => onPick(batch)}
                className="flex items-center gap-2 rounded-md border border-border bg-background px-2.5 py-1.5 text-sm hover:border-primary/50 disabled:opacity-50"
              >
                <span className="font-mono text-xs">{batch.batchNo}</span>
                <span>exp {expiryLabel(batch.expiryDate)}</span>
                <span className="tabular-nums text-muted-foreground">{quantity(batch.quantityOnHand)} in stock</span>
                {days < 0 ? <Badge variant="destructive">Expired</Badge> : days <= 90 ? <Badge variant="warning">{days}d left</Badge> : null}
              </button>
            );
          })}
        </div>
      )}
    </section>
  );
});
