import { RotateCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { EVENT_TYPE_META } from '../constants';
import { useDepartmentNames } from '../hooks/useDepartmentDirectory';
import { createEmptyFilters, EVENT_TYPES, type CalendarEventFilters, type EventType } from '../types';

const QUICK_FILTERS: { label: string; types: EventType[] }[] = [
  { label: 'Holidays Only', types: ['Holiday'] },
  { label: 'Doctor Leave Only', types: ['DoctorLeave'] },
  { label: 'Meetings', types: ['Meeting'] },
  { label: 'Training', types: ['Training'] },
];

interface FilterPanelProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  filters: CalendarEventFilters;
  onFiltersChange: (filters: CalendarEventFilters) => void;
  anchor: React.ReactNode;
}

export function FilterPanel({ open, onOpenChange, filters, onFiltersChange, anchor }: FilterPanelProps) {
  const departmentNames = useDepartmentNames();

  function toggleType(type: EventType) {
    onFiltersChange({
      ...filters,
      types: filters.types.includes(type) ? filters.types.filter((t) => t !== type) : [...filters.types, type],
    });
  }

  function applyQuickFilter(types: EventType[]) {
    const isActive = types.every((t) => filters.types.includes(t)) && filters.types.length === types.length;
    onFiltersChange({ ...filters, types: isActive ? [] : types });
  }

  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      <PopoverTrigger asChild>{anchor}</PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-foreground">Filters</h3>
          <Button variant="ghost" size="sm" className="h-7 px-2 text-xs text-muted-foreground" onClick={() => onFiltersChange(createEmptyFilters())}>
            <RotateCcw className="h-3 w-3" />
            Reset Filters
          </Button>
        </div>

        <div className="mt-3 flex flex-col gap-4">
          <div>
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Quick Filters</p>
            <div className="flex flex-wrap gap-1.5">
              {QUICK_FILTERS.map((qf) => {
                const active = qf.types.every((t) => filters.types.includes(t)) && filters.types.length === qf.types.length;
                return (
                  <button
                    key={qf.label}
                    type="button"
                    onClick={() => applyQuickFilter(qf.types)}
                    className={cn(
                      'rounded-full border px-2.5 py-1 text-xs font-medium transition-colors',
                      active ? 'border-primary bg-primary/10 text-primary' : 'border-border text-muted-foreground hover:bg-accent',
                    )}
                  >
                    {qf.label}
                  </button>
                );
              })}
            </div>
          </div>

          <div>
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Event Type</p>
            <div className="grid grid-cols-2 gap-1">
              {EVENT_TYPES.map((type) => {
                const meta = EVENT_TYPE_META[type];
                const checked = filters.types.includes(type);
                return (
                  <label key={type} className="flex cursor-pointer items-center gap-1.5 rounded px-1.5 py-1 text-xs hover:bg-accent">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggleType(type)}
                      className="h-3.5 w-3.5 rounded border-input text-primary"
                    />
                    <span className={cn('h-2 w-2 rounded-full', meta.dotClass)} aria-hidden="true" />
                    {meta.label}
                  </label>
                );
              })}
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="filter-department" className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Department
            </Label>
            <Select
              value={filters.department ?? 'all'}
              onValueChange={(value) => onFiltersChange({ ...filters, department: value === 'all' ? undefined : value })}
            >
              <SelectTrigger id="filter-department" className="h-9 text-sm">
                <SelectValue placeholder="All departments" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All departments</SelectItem>
                {departmentNames.map((dept) => (
                  <SelectItem key={dept} value={dept}>
                    {dept}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div>
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Date Range</p>
            <div className="grid grid-cols-2 gap-2">
              <Input
                type="date"
                aria-label="From date"
                value={filters.dateFrom ?? ''}
                onChange={(e) => onFiltersChange({ ...filters, dateFrom: e.target.value || undefined })}
                className="h-9 text-sm"
              />
              <Input
                type="date"
                aria-label="To date"
                value={filters.dateTo ?? ''}
                onChange={(e) => onFiltersChange({ ...filters, dateTo: e.target.value || undefined })}
                className="h-9 text-sm"
              />
            </div>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
