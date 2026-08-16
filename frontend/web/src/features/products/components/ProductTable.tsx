import type { Product } from '@hms/shared';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { useAuth } from '@/features/auth/AuthContext';
import { StatusBadge } from './StatusBadge';

interface ProductTableProps {
  products: Product[];
  sort: string;
  onSortChange: (sort: string) => void;
}

const columns: Array<{ field: string; label: string }> = [
  { field: 'productName', label: 'Product' },
  { field: 'sku', label: 'SKU' },
];

export function ProductTable({ products, sort, onSortChange }: ProductTableProps) {
  // Primes the Masters reference cache for the two entities this table's columns resolve
  // labels from (see engine/registry.ts's resolveRecordLabel) — same technique Masters'
  // own MasterTable uses for reference-type columns.
  useMasterOptionsQuery('productCategory');
  useMasterOptionsQuery('brand');

  const currentField = sort.startsWith('-') ? sort.slice(1) : sort;
  const isDescending = sort.startsWith('-');
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('support-services.edit');

  function toggleSort(field: string) {
    if (currentField !== field) {
      onSortChange(field);
      return;
    }
    onSortChange(isDescending ? field : `-${field}`);
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            {columns.map((column) => (
              <th key={column.field} className="whitespace-nowrap px-4 py-2.5">
                <button type="button" onClick={() => toggleSort(column.field)} className="inline-flex items-center gap-1 hover:text-foreground">
                  {column.label}
                  {currentField === column.field &&
                    (isDescending ? <ArrowDown className="h-3.5 w-3.5" /> : <ArrowUp className="h-3.5 w-3.5" />)}
                </button>
              </th>
            ))}
            <th className="whitespace-nowrap px-4 py-2.5">Category</th>
            <th className="whitespace-nowrap px-4 py-2.5">Brand</th>
            <th className="whitespace-nowrap px-4 py-2.5">Selling Price</th>
            <th className="whitespace-nowrap px-4 py-2.5">Status</th>
            <th className="whitespace-nowrap px-4 py-2.5 text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {products.map((product) => (
            <tr key={product.id} className="hover:bg-muted/30">
              <td className="whitespace-nowrap px-4 py-3">
                <Link to={`/support/inventory/${product.id}`} className="font-medium text-foreground hover:text-primary hover:underline">
                  {product.productName}
                </Link>
                <div className="text-xs text-muted-foreground">{product.productCode}</div>
              </td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{product.sku}</td>
              <td className="max-w-xs truncate px-4 py-3 text-muted-foreground">{resolveRecordLabel('productCategory', product.categoryId)}</td>
              <td className="max-w-xs truncate px-4 py-3 text-muted-foreground">{resolveRecordLabel('brand', product.brandId)}</td>
              <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{product.sellingPrice.toLocaleString('en-IN')}</td>
              <td className="px-4 py-3">
                <StatusBadge isActive={product.isActive} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1.5">
                  <Button asChild variant="ghost" size="sm">
                    <Link to={`/support/inventory/${product.id}`}>View</Link>
                  </Button>
                  {canEdit && (
                    <Button asChild variant="ghost" size="sm">
                      <Link to={`/support/inventory/${product.id}/edit`}>Edit</Link>
                    </Button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
