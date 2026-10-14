import type { Product } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useProductsQuery } from '@/features/pharmacy/product-lookup';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { productsApi } from '@/services/apiClient';

interface ProductSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  /** Hands back the whole product alongside its id, for callers that need more than the id
   * (e.g. the dispense cart's name and selling price). */
  onProductChange?: (product: Product) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** How many matches one search shows. A pharmacy catalog runs to thousands of products, so
 * the list is never loaded whole — the pharmacist narrows it by typing instead. */
const RESULT_LIMIT = 25;

function toOption(product: Product) {
  return {
    value: product.id,
    label: `${product.productName} (${product.sku})`,
    // Mirrors what the backend search matches on, so SearchableSelect's own client-side
    // filter never hides a row the server returned for a generic-name or code match.
    keywords: `${product.sku} ${product.productCode} ${product.genericName ?? ''}`,
  };
}

/**
 * Product picker for Pharmacy's Stock Receipt / Dispense forms and the Stock Ledger filter,
 * backed by GET /api/v1/products (active products only) and searched on the server by
 * name, generic name, SKU or code.
 */
export function ProductSelect({ id, value, onValueChange, onProductChange, ariaLabel = 'Product', disabled }: ProductSelectProps) {
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search.trim(), 250);

  const { data, isPending, isFetching } = useProductsQuery({
    pageSize: RESULT_LIMIT,
    isActive: true,
    sort: 'productName',
    search: debouncedSearch || undefined,
  });
  const results = data?.items ?? [];

  // The selected product is usually not among the current search's results (the list resets
  // to the first page every time the dropdown reopens), so it's fetched on its own to keep
  // the trigger showing its name rather than falling back to the placeholder.
  const selectedInResults = results.find((product) => product.id === value);
  const { data: selectedById } = useQuery({
    queryKey: ['pharmacy', 'products', 'byId', value],
    queryFn: () => productsApi.getProductById(value),
    enabled: Boolean(value) && !selectedInResults,
    staleTime: 60_000,
  });
  const selected = selectedInResults ?? (selectedById?.id === value ? selectedById : undefined);

  const options = results.map(toOption);
  if (selected && !selectedInResults) {
    options.unshift(toOption(selected));
  }

  function handleValueChange(productId: string) {
    onValueChange(productId);
    const product = results.find((p) => p.id === productId) ?? (selected?.id === productId ? selected : undefined);
    if (product) {
      onProductChange?.(product);
    }
  }

  const moreAvailable = (data?.meta.totalCount ?? 0) > results.length;

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={handleValueChange}
      options={options}
      placeholder={isPending ? 'Loading products…' : 'Select product…'}
      searchPlaceholder={moreAvailable ? 'Type to search all products…' : 'Search by name, generic, SKU, or code…'}
      ariaLabel={ariaLabel}
      disabled={disabled}
      onSearchChange={setSearch}
      // Also "loading" for the debounce gap between a keystroke and its request starting —
      // otherwise the list flashes "No matches" for a search that simply hasn't run yet.
      isLoading={isFetching || search.trim() !== debouncedSearch}
    />
  );
}
