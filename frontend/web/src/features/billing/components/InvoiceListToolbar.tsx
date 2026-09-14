import { CircleDollarSign, RotateCcw, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { PaymentStatus } from '../types';

interface InvoiceListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  paymentStatus: PaymentStatus | undefined;
  onPaymentStatusChange: (value: PaymentStatus | undefined) => void;
}

/**
 * Search/filter only — Reports and New Invoice moved up to InvoiceLedgerPage's persistent
 * header (see that file's own comment) so they're always visible without scrolling past
 * whichever tab's table is currently showing.
 *
 * Same bordered/shadowed card look every Finance report's own filter bar uses
 * (features/reports/components/ReportFilterBar.tsx) for visual consistency across the whole
 * Accounts & Finance section — but deliberately NOT that shared component or its Search/Reset-
 * commits-a-draft pattern: this toolbar's search is already server-side and debounced
 * (AllInvoicesPage's own `useDebouncedValue`), so it stays genuinely live rather than gaining an
 * extra click that would only slow it down. "Reset" here just clears these two fields back to
 * their defaults, immediately.
 */
export function InvoiceListToolbar({ search, onSearchChange, paymentStatus, onPaymentStatusChange }: InvoiceListToolbarProps) {
  function handleReset() {
    onSearchChange('');
    onPaymentStatusChange(undefined);
  }

  return (
    <div className="flex flex-wrap items-end gap-4 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-col gap-1">
        <Label htmlFor="invoice-list-search" className="flex items-center gap-1.5">
          <Search className="h-4 w-4 text-muted-foreground" />
          Search
        </Label>
        <div className="relative w-72">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            id="invoice-list-search"
            type="search"
            placeholder="Search by patient name, UHID, or invoice ID…"
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="invoice-list-status" className="flex items-center gap-1.5">
          <CircleDollarSign className="h-4 w-4 text-muted-foreground" />
          Payment Status
        </Label>
        <Select
          value={paymentStatus ?? 'all'}
          onValueChange={(value) => onPaymentStatusChange(value === 'all' ? undefined : (value as PaymentStatus))}
        >
          <SelectTrigger id="invoice-list-status" className="w-48" aria-label="Filter by payment status">
            <SelectValue placeholder="All payment statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All payment statuses</SelectItem>
            <SelectItem value="Paid">Paid only</SelectItem>
            <SelectItem value="Pending">Pending only</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Button type="button" variant="outline" className="gap-1.5" onClick={handleReset}>
        <RotateCcw className="h-4 w-4" />
        Reset
      </Button>
    </div>
  );
}
