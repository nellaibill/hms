import type { TransactionType } from '@hms/shared';
import { ListFilter } from 'lucide-react';
import { ProductSelect } from '@/components/ProductSelect';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

export interface StockLedgerFilters {
  productId: string;
  transactionType: TransactionType | undefined;
  fromDate: string;
  toDate: string;
}

interface StockLedgerToolbarProps {
  filters: StockLedgerFilters;
  onChange: (filters: StockLedgerFilters) => void;
}

export function StockLedgerToolbar({ filters, onChange }: StockLedgerToolbarProps) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <ListFilter className="mb-2 h-4 w-4 text-muted-foreground" />

      <div className="flex flex-col gap-1">
        <Label htmlFor="ledger-product">Product</Label>
        <div className="w-56">
          <ProductSelect id="ledger-product" value={filters.productId} onValueChange={(value) => onChange({ ...filters, productId: value })} />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="ledger-type">Transaction type</Label>
        <Select
          value={filters.transactionType ?? 'all'}
          onValueChange={(value) => onChange({ ...filters, transactionType: value === 'all' ? undefined : (value as TransactionType) })}
        >
          <SelectTrigger id="ledger-type" className="w-44" aria-label="Filter by transaction type">
            <SelectValue placeholder="All types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All types</SelectItem>
            <SelectItem value="Receipt">Receipt only</SelectItem>
            <SelectItem value="Dispense">Dispense only</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="ledger-from">From</Label>
        <Input
          id="ledger-from"
          type="date"
          value={filters.fromDate}
          max={filters.toDate || undefined}
          onChange={(e) => onChange({ ...filters, fromDate: e.target.value })}
          className="w-44"
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="ledger-to">To</Label>
        <Input
          id="ledger-to"
          type="date"
          value={filters.toDate}
          min={filters.fromDate || undefined}
          onChange={(e) => onChange({ ...filters, toDate: e.target.value })}
          className="w-44"
        />
      </div>
    </div>
  );
}
