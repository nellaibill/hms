import { ChevronLeft, ChevronRight, Filter, RefreshCw, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { FilterPanel } from './FilterPanel';
import type { CalendarEventFilters } from '../types';
import { MONTH_LABELS } from '../utils/date';

const VIEWS = [
  { key: 'month', label: 'Month', enabled: true },
  { key: 'week', label: 'Week', enabled: false },
  { key: 'day', label: 'Day', enabled: false },
] as const;

interface CalendarToolbarProps {
  year: number;
  month: number;
  onPrevMonth: () => void;
  onNextMonth: () => void;
  onToday: () => void;
  search: string;
  onSearchChange: (value: string) => void;
  filters: CalendarEventFilters;
  onFiltersChange: (filters: CalendarEventFilters) => void;
  filterPanelOpen: boolean;
  onFilterPanelOpenChange: (open: boolean) => void;
  onRefresh: () => void;
  isRefreshing?: boolean;
}

export function CalendarToolbar({
  year,
  month,
  onPrevMonth,
  onNextMonth,
  onToday,
  search,
  onSearchChange,
  filters,
  onFiltersChange,
  filterPanelOpen,
  onFilterPanelOpenChange,
  onRefresh,
  isRefreshing,
}: CalendarToolbarProps) {
  const activeFilterCount = filters.types.length + (filters.department ? 1 : 0) + (filters.dateFrom || filters.dateTo ? 1 : 0);
  return (
    <div className="sticky top-0 z-20 flex flex-wrap items-center gap-3 border-b border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80 sm:px-6">
      <div className="flex items-center gap-1.5">
        <Button variant="outline" size="sm" onClick={onToday}>
          Today
        </Button>
        <Button variant="ghost" size="icon" aria-label="Previous month" onClick={onPrevMonth}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="icon" aria-label="Next month" onClick={onNextMonth}>
          <ChevronRight className="h-4 w-4" />
        </Button>
        <h2 className="ml-1 text-lg font-semibold tracking-tight text-foreground">
          {MONTH_LABELS[month - 1]} {year}
        </h2>
      </div>

      <div className="flex items-center gap-0.5 rounded-md border border-border bg-muted/40 p-0.5">
        {VIEWS.map((view) => (
          <Tooltip key={view.key}>
            <TooltipTrigger asChild>
              <button
                type="button"
                disabled={!view.enabled}
                aria-current={view.enabled && view.key === 'month' ? 'true' : undefined}
                className={cn(
                  'rounded px-3 py-1.5 text-sm font-medium transition-colors',
                  view.key === 'month'
                    ? 'bg-background text-foreground shadow-soft'
                    : 'cursor-not-allowed text-muted-foreground/60',
                )}
              >
                {view.label}
              </button>
            </TooltipTrigger>
            {!view.enabled && <TooltipContent>Coming soon</TooltipContent>}
          </Tooltip>
        ))}
      </div>

      <div className="relative ml-auto w-full min-w-[160px] max-w-xs flex-1 sm:w-64 sm:flex-none">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search events…"
          aria-label="Search events"
          className="pl-8"
        />
      </div>

      <FilterPanel
        open={filterPanelOpen}
        onOpenChange={onFilterPanelOpenChange}
        filters={filters}
        onFiltersChange={onFiltersChange}
        anchor={
          <Button variant="outline" size="sm" className="relative">
            <Filter className="mr-1.5 h-4 w-4" />
            Filters
            {activeFilterCount > 0 && (
              <span className="ml-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold text-primary-foreground">
                {activeFilterCount}
              </span>
            )}
          </Button>
        }
      />

      <Button
        variant="ghost"
        size="icon"
        aria-label="Refresh"
        onClick={onRefresh}
        disabled={isRefreshing}
      >
        <RefreshCw className={cn('h-4 w-4', isRefreshing && 'animate-spin')} />
      </Button>
    </div>
  );
}
