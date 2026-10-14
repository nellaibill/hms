import { forwardRef } from 'react';
import { mastersApi } from '@/services/apiClient';
import { LookupInput, type LookupInputHandle } from './LookupInput';

export interface SupplierOption {
  id: string;
  name: string;
  code: string;
  phone?: string;
  taxId?: string;
}

interface SupplierLookupProps {
  value: SupplierOption | null;
  onChange: (supplier: SupplierOption) => void;
  autoFocus?: boolean;
  disabled?: boolean;
}

async function searchSuppliers(term: string): Promise<SupplierOption[]> {
  const page = await mastersApi.list('supplier', { search: term || undefined, pageSize: 20, isActive: true, sort: 'supplierName' });
  return page.items.map((record) => ({
    id: String(record.id),
    name: String(record.supplierName ?? ''),
    code: String(record.supplierCode ?? ''),
    phone: record.phone ? String(record.phone) : undefined,
    taxId: record.taxId ? String(record.taxId) : undefined,
  }));
}

/** Supplier picker for purchase screens, searched on the server from the Supplier master.
 * Lists every active supplier on focus — a pharmacy buys from a couple of dozen at most. */
export const SupplierLookup = forwardRef<LookupInputHandle, SupplierLookupProps>(function SupplierLookup({ value, onChange, autoFocus, disabled }, ref) {
  return (
    <LookupInput<SupplierOption>
      ref={ref}
      queryKey={['pharmacy', 'suppliers', 'lookup']}
      search={searchSuppliers}
      getKey={(supplier) => supplier.id}
      onSelect={onChange}
      ariaLabel="Supplier"
      placeholder="Search supplier…"
      minChars={0}
      emptyText="No supplier found. Add suppliers from Pharmacy → Setup."
      displayValue={value?.name}
      autoFocus={autoFocus}
      disabled={disabled}
      renderItem={(supplier) => (
        <div className="flex items-baseline justify-between gap-3">
          <span className="font-medium">{supplier.name}</span>
          <span className="text-xs text-muted-foreground">
            {supplier.code}
            {supplier.taxId ? ` · GSTIN ${supplier.taxId}` : ''}
          </span>
        </div>
      )}
    />
  );
});
