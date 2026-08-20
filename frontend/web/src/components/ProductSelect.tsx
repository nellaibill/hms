import { useProductsQuery } from '@/features/pharmacy/product-lookup';
import { SearchableSelect } from '@/components/ui/searchable-select';

interface ProductSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

/** Product picker for Pharmacy's Stock Receipt / Dispense forms, backed by the real
 * GET /api/v1/products list (active products only) — mirrors DepartmentSelect's shape. */
export function ProductSelect({ id, value, onValueChange, ariaLabel = 'Product', disabled }: ProductSelectProps) {
  const { data, isPending } = useProductsQuery({ pageSize: 100, isActive: true, sort: 'productName' });

  const options = (data?.items ?? []).map((product) => ({
    value: product.id,
    label: `${product.productName} (${product.sku})`,
    keywords: `${product.sku} ${product.productCode} ${product.genericName ?? ''}`,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder={isPending ? 'Loading products…' : 'Select product…'}
      searchPlaceholder="Search by name, SKU, or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
