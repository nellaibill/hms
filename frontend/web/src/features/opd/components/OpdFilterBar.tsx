import { ADMISSION_STATUSES, INVOICE_PAYMENT_STATUSES, LAB_ORDER_ITEM_STATUSES, OPD_CONSULTATION_STATUSES } from '@hms/shared';
import { Download, RefreshCw, Search } from 'lucide-react';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { humanize } from '@/features/patients/humanize';
import type { OpdFilterValues, OpdTab } from '../types';

/** Per-tab option list for the shared Status dropdown — each tab's list endpoint filters on
 * a different underlying status field (OpdConsultationStatus / LabOrderItemStatus /
 * InvoicePaymentStatus / AdmissionStatus), per the OPD module's real endpoint shapes. The
 * Consultation List tab's summary endpoint has no status filter at all (it returns
 * per-consultant counts across every status at once), so it reuses the OPD status list only
 * for label consistency — the value is never actually sent for that tab. */
function statusOptionsForTab(tab: OpdTab): readonly string[] {
  switch (tab) {
    case 'investigations':
      return LAB_ORDER_ITEM_STATUSES;
    case 'procedures':
      return INVOICE_PAYMENT_STATUSES;
    case 'admissions':
      return ADMISSION_STATUSES;
    default:
      return OPD_CONSULTATION_STATUSES;
  }
}

interface OpdFilterBarProps {
  tab: OpdTab;
  filters: OpdFilterValues;
  onChange: (filters: OpdFilterValues) => void;
  onRefresh: () => void;
  /** True while the active tab's data is being (re)fetched — spins the Refresh icon. */
  isRefreshing?: boolean;
  onExport: () => void;
  /** True when the signed-in user is a Consultant/Doctor linked to their own consultant
   * record (AuthContextValue.scopedConsultantId) — locks Department/Consultant to their own
   * so they can't browse another consultant's queue. UI convenience only: the backend forces
   * this same restriction regardless of what these fields are set to. */
  lockDepartmentAndConsultant?: boolean;
}

/** Shared filter toolbar for every OPD tab — mirrors LabWorklistFilters' exact layout shape
 * (rounded card, flex-wrap row of labeled fields), plus the Refresh/Export actions the OPD
 * list page's screenshot shows pinned to the row's right edge. */
export function OpdFilterBar({ tab, filters, onChange, onRefresh, isRefreshing, onExport, lockDepartmentAndConsultant }: OpdFilterBarProps) {
  const statusOptions = statusOptionsForTab(tab);

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border border-border bg-card p-4 shadow-soft-md">
      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-from">From Date</Label>
        <Input
          id="opd-from"
          type="date"
          value={filters.from}
          max={filters.to || undefined}
          onChange={(event) => onChange({ ...filters, from: event.target.value })}
          className="w-40"
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-to">To Date</Label>
        <Input
          id="opd-to"
          type="date"
          value={filters.to}
          min={filters.from || undefined}
          onChange={(event) => onChange({ ...filters, to: event.target.value })}
          className="w-40"
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-department">Department</Label>
        <DepartmentSelect
          id="opd-department"
          value={filters.departmentId ?? ''}
          onValueChange={(value) => onChange({ ...filters, departmentId: value || undefined, consultantId: undefined })}
          ariaLabel="Filter by department"
          disabled={lockDepartmentAndConsultant}
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-consultant">Consultant</Label>
        <ConsultantSelect
          id="opd-consultant"
          value={filters.consultantId ?? ''}
          onValueChange={(value) => onChange({ ...filters, consultantId: value || undefined })}
          departmentId={filters.departmentId}
          ariaLabel="Filter by consultant"
          disabled={lockDepartmentAndConsultant}
        />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-status">Status</Label>
        <Select
          value={filters.status ?? 'all'}
          onValueChange={(value) => onChange({ ...filters, status: value === 'all' ? undefined : value })}
        >
          <SelectTrigger id="opd-status" className="w-44" aria-label="Filter by status">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {statusOptions.map((status) => (
              <SelectItem key={status} value={status}>
                {humanize(status)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="opd-search">Search</Label>
        <div className="relative w-64">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            id="opd-search"
            type="search"
            placeholder="Patient Name / UHID / Phone"
            value={filters.search}
            onChange={(event) => onChange({ ...filters, search: event.target.value })}
            className="pl-9"
          />
        </div>
      </div>

      <div className="ml-auto flex items-center gap-2">
        <Button type="button" variant="outline" onClick={onRefresh} disabled={isRefreshing} className="gap-1.5">
          <RefreshCw className={isRefreshing ? 'h-4 w-4 animate-spin' : 'h-4 w-4'} />
          Refresh
        </Button>
        <Button type="button" onClick={onExport} className="gap-1.5">
          <Download className="h-4 w-4" />
          Export
        </Button>
      </div>
    </div>
  );
}
