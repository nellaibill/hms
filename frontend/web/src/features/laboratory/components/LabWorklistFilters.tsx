import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { humanize } from '@/features/patients/humanize';
import { LAB_ORDER_PRIORITIES, LAB_ORDER_STATUSES, type LabOrderPriority, type LabOrderStatus } from '../types';

export interface LabWorklistFilterValues {
  search: string;
  status: LabOrderStatus | undefined;
  priority: LabOrderPriority | undefined;
  dateFrom: string;
  dateTo: string;
}

interface LabWorklistFiltersProps {
  filters: LabWorklistFilterValues;
  onChange: (filters: LabWorklistFilterValues) => void;
}

/** The lab worklist's own filter toolbar — status/priority/date-range dropdowns plus a
 * (page-level debounced) search input, mirroring StockLedgerToolbar's exact layout shape. */
export function LabWorklistFilters({ filters, onChange }: LabWorklistFiltersProps) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-col gap-1">
        <Label htmlFor="worklist-search">Search</Label>
        <div className="relative w-64">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            id="worklist-search"
            type="search"
            placeholder="Order no., patient name, or UHID…"
            value={filters.search}
            onChange={(event) => onChange({ ...filters, search: event.target.value })}
            className="pl-9"
          />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="worklist-status">Status</Label>
        <Select
          value={filters.status ?? 'all'}
          onValueChange={(value) => onChange({ ...filters, status: value === 'all' ? undefined : (value as LabOrderStatus) })}
        >
          <SelectTrigger id="worklist-status" className="w-48" aria-label="Filter by status">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {LAB_ORDER_STATUSES.map((status) => (
              <SelectItem key={status} value={status}>
                {humanize(status)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="worklist-priority">Priority</Label>
        <Select
          value={filters.priority ?? 'all'}
          onValueChange={(value) => onChange({ ...filters, priority: value === 'all' ? undefined : (value as LabOrderPriority) })}
        >
          <SelectTrigger id="worklist-priority" className="w-36" aria-label="Filter by priority">
            <SelectValue placeholder="All priorities" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All priorities</SelectItem>
            {LAB_ORDER_PRIORITIES.map((priority) => (
              <SelectItem key={priority} value={priority}>
                {priority}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="worklist-from">From</Label>
        <Input
          id="worklist-from"
          type="date"
          value={filters.dateFrom}
          max={filters.dateTo || undefined}
          onChange={(event) => onChange({ ...filters, dateFrom: event.target.value })}
          className="w-40"
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="worklist-to">To</Label>
        <Input
          id="worklist-to"
          type="date"
          value={filters.dateTo}
          min={filters.dateFrom || undefined}
          onChange={(event) => onChange({ ...filters, dateTo: event.target.value })}
          className="w-40"
        />
      </div>
    </div>
  );
}
