import { Building2, CalendarRange, Layers, RotateCcw, Search, User } from 'lucide-react';
import { BILLING_TYPES, type BillingItem } from '@/features/billing';
import { getDisplayLabel, getMasterConfig, useMasterOptionsQuery } from '@/features/masters';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { ProfitReportFilterState } from '../profitReport';
import type { ReportDateRange } from '../types';

const ALL_VALUE = '__all__';

/** The two BillingItem.billingType values with no dedicated billing-wizard card (see
 * BillingItem's own doc comment) — generated server-side only, still real line items a report
 * should be able to isolate. */
const SERVER_ONLY_BILLING_TYPES = ['Pharmacy', 'InpatientCharge'] as const;

const BILLING_TYPE_LABELS: Record<BillingItem['billingType'], string> = {
  Consultation: 'Consultation',
  Radiology: 'Radiology',
  Laboratory: 'Laboratory',
  Procedure: 'Procedure',
  Injection: 'Injection',
  File: 'File',
  Pharmacy: 'Pharmacy',
  InpatientCharge: 'Inpatient Charge',
};

interface ProfitReportFilterBarProps {
  range: ReportDateRange;
  filters: ProfitReportFilterState;
  onRangeChange: (range: ReportDateRange) => void;
  onFiltersChange: (filters: ProfitReportFilterState) => void;
  onSearch: () => void;
  onReset: () => void;
}

/**
 * Hospital Profit Report's one-card filter bar — date range, free-text search, and Billing
 * Type/Department/Consultant dropdowns, applied only once "Search" is clicked rather than live
 * on every keystroke/selection (see ProfitReportPage's own doc comment on why: this report can
 * mix a large date range with several filters at once, so committing them together as one
 * explicit action reads more predictably than the page's numbers shifting under a half-typed
 * search term). "Reset" clears every field back to the page's defaults and returns to the
 * pre-search empty state, not just clearing the inputs.
 */
export function ProfitReportFilterBar({ range, filters, onRangeChange, onFiltersChange, onSearch, onReset }: ProfitReportFilterBarProps) {
  const { data: departments } = useMasterOptionsQuery('department');
  const { data: consultants } = useMasterOptionsQuery('consultant');
  const departmentConfig = getMasterConfig('department');
  const consultantConfig = getMasterConfig('consultant');

  return (
    <div className="flex flex-wrap items-end gap-4 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-col gap-1">
        <Label className="flex items-center gap-1.5">
          <CalendarRange className="h-4 w-4 text-muted-foreground" />
          Date Range
        </Label>
        <div className="flex items-end gap-2">
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted-foreground">From</span>
            <Input
              type="date"
              aria-label="From"
              value={range.from}
              max={range.to}
              onChange={(e) => onRangeChange({ ...range, from: e.target.value })}
              className="w-40"
            />
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted-foreground">To</span>
            <Input
              type="date"
              aria-label="To"
              value={range.to}
              min={range.from}
              onChange={(e) => onRangeChange({ ...range, to: e.target.value })}
              className="w-40"
            />
          </div>
        </div>
      </div>

      <div className="hidden h-14 w-px bg-border sm:block" />

      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-search" className="flex items-center gap-1.5">
          <Search className="h-4 w-4 text-muted-foreground" />
          Search
        </Label>
        <Input
          id="profit-report-search"
          type="search"
          placeholder="Search by service, package, patient or invoice…"
          value={filters.search ?? ''}
          onChange={(event) => onFiltersChange({ ...filters, search: event.target.value || undefined })}
          className="w-64"
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-billing-type" className="flex items-center gap-1.5">
          <Layers className="h-4 w-4 text-muted-foreground" />
          Billing Type
        </Label>
        <Select
          value={filters.billingType ?? ALL_VALUE}
          onValueChange={(value) => onFiltersChange({ ...filters, billingType: value === ALL_VALUE ? undefined : (value as BillingItem['billingType']) })}
        >
          <SelectTrigger id="profit-report-billing-type" className="w-40" aria-label="Filter by billing type">
            <SelectValue placeholder="All types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_VALUE}>All types</SelectItem>
            {[...BILLING_TYPES, ...SERVER_ONLY_BILLING_TYPES].map((type) => (
              <SelectItem key={type} value={type}>
                {BILLING_TYPE_LABELS[type]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-department" className="flex items-center gap-1.5">
          <Building2 className="h-4 w-4 text-muted-foreground" />
          Department
        </Label>
        <Select
          value={filters.departmentId ?? ALL_VALUE}
          onValueChange={(value) => onFiltersChange({ ...filters, departmentId: value === ALL_VALUE ? undefined : value })}
        >
          <SelectTrigger id="profit-report-department" className="w-44" aria-label="Filter by department">
            <SelectValue placeholder="All departments" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_VALUE}>All departments</SelectItem>
            {(departments ?? []).map((department) => (
              <SelectItem key={department.id} value={department.id}>
                {departmentConfig ? getDisplayLabel(departmentConfig, department) : department.id}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-consultant" className="flex items-center gap-1.5">
          <User className="h-4 w-4 text-muted-foreground" />
          Consultant
        </Label>
        <Select
          value={filters.consultantId ?? ALL_VALUE}
          onValueChange={(value) => onFiltersChange({ ...filters, consultantId: value === ALL_VALUE ? undefined : value })}
        >
          <SelectTrigger id="profit-report-consultant" className="w-44" aria-label="Filter by consultant">
            <SelectValue placeholder="All consultants" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_VALUE}>All consultants</SelectItem>
            {(consultants ?? []).map((consultant) => (
              <SelectItem key={consultant.id} value={consultant.id}>
                {consultantConfig ? getDisplayLabel(consultantConfig, consultant) : consultant.id}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex gap-2">
        <Button type="button" className="gap-1.5" onClick={onSearch}>
          <Search className="h-4 w-4" />
          Search
        </Button>
        <Button type="button" variant="outline" className="gap-1.5" onClick={onReset}>
          <RotateCcw className="h-4 w-4" />
          Reset
        </Button>
      </div>
    </div>
  );
}
