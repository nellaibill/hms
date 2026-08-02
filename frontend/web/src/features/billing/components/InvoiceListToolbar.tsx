import { Plus, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { PaymentStatus } from '../types';

interface InvoiceListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  paymentStatus: PaymentStatus | undefined;
  onPaymentStatusChange: (value: PaymentStatus | undefined) => void;
}

export function InvoiceListToolbar({ search, onSearchChange, paymentStatus, onPaymentStatusChange }: InvoiceListToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative w-72">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by patient name, UHID, or invoice ID…"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          aria-label="Search invoices"
          className="pl-9"
        />
      </div>

      <Select
        value={paymentStatus ?? 'all'}
        onValueChange={(value) => onPaymentStatusChange(value === 'all' ? undefined : (value as PaymentStatus))}
      >
        <SelectTrigger className="w-48" aria-label="Filter by payment status">
          <SelectValue placeholder="All payment statuses" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All payment statuses</SelectItem>
          <SelectItem value="Paid">Paid only</SelectItem>
          <SelectItem value="Pending">Pending only</SelectItem>
        </SelectContent>
      </Select>

      <Button asChild className="ml-auto gap-1.5">
        <Link to="/finance/accounts/new">
          <Plus className="h-4 w-4" />
          New Invoice
        </Link>
      </Button>
    </div>
  );
}
