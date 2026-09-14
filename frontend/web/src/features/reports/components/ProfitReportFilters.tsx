import { Search } from 'lucide-react';
import { BILLING_TYPES, type BillingItem } from '@/features/billing';
import { getDisplayLabel, getMasterConfig, useMasterOptionsQuery } from '@/features/masters';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { ProfitReportFilterState } from '../profitReport';

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

interface ProfitReportFiltersProps {
  filters: ProfitReportFilterState;
  onChange: (filters: ProfitReportFilterState) => void;
}

/** Hospital Profit Report's filter bar — Billing Type/Department/Consultant dropdowns plus a
 * free-text search, mirroring LabWorklistFilters' exact toolbar shape. Department and Consultant
 * are deliberately independent filters here (not chained the way a data-entry form's Department
 * → Consultant selects are) — a consultant billed outside their usual department shouldn't
 * silently disappear just because both filters happen to be set. */
export function ProfitReportFilters({ filters, onChange }: ProfitReportFiltersProps) {
  const { data: departments } = useMasterOptionsQuery('department');
  const { data: consultants } = useMasterOptionsQuery('consultant');
  const departmentConfig = getMasterConfig('department');
  const consultantConfig = getMasterConfig('consultant');

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-search">Search</Label>
        <div className="relative w-64">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            id="profit-report-search"
            type="search"
            placeholder="Service, package, patient, or invoice…"
            value={filters.search ?? ''}
            onChange={(event) => onChange({ ...filters, search: event.target.value || undefined })}
            className="pl-9"
          />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="profit-report-billing-type">Billing type</Label>
        <Select
          value={filters.billingType ?? ALL_VALUE}
          onValueChange={(value) => onChange({ ...filters, billingType: value === ALL_VALUE ? undefined : (value as BillingItem['billingType']) })}
        >
          <SelectTrigger id="profit-report-billing-type" className="w-44" aria-label="Filter by billing type">
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
        <Label htmlFor="profit-report-department">Department</Label>
        <Select
          value={filters.departmentId ?? ALL_VALUE}
          onValueChange={(value) => onChange({ ...filters, departmentId: value === ALL_VALUE ? undefined : value })}
        >
          <SelectTrigger id="profit-report-department" className="w-48" aria-label="Filter by department">
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
        <Label htmlFor="profit-report-consultant">Consultant</Label>
        <Select
          value={filters.consultantId ?? ALL_VALUE}
          onValueChange={(value) => onChange({ ...filters, consultantId: value === ALL_VALUE ? undefined : value })}
        >
          <SelectTrigger id="profit-report-consultant" className="w-48" aria-label="Filter by consultant">
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
    </div>
  );
}
