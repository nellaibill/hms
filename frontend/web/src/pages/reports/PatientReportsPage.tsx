import type { BloodGroup, Gender, PatientListQuery, PatientReportRow } from '@hms/shared';
import { BLOOD_GROUPS, GENDERS } from '@hms/shared';
import { FileSpreadsheet, Loader2, Printer, RefreshCw, Users } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PageBanner } from '@/components/PageBanner';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/components/ui/toast-context';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { downloadBlob } from '@/features/patientImport';
import { bloodGroupLabel } from '@/features/patients/bloodGroupLabel';
import { Pagination } from '@/features/patients';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { departmentsApi, patientsApi } from '@/services/apiClient';
import { PatientNameLink } from '@/components/PatientNameLink';

const PAGE_SIZES = [25, 50, 100] as const;

function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10);
}

// Default range mirrors this app's other reports (Finance, the Patient Analytics charts page
// this replaces) — a rolling 90-day window, "To" defaulting to today.
function defaultFrom(): string {
  const d = new Date();
  d.setDate(d.getDate() - 90);
  return toDateInputValue(d);
}

interface Filters {
  from: string;
  to: string;
  search: string;
  gender: Gender | '';
  bloodGroup: BloodGroup | '';
  departmentId: string;
}

function defaultFilters(): Filters {
  return { from: defaultFrom(), to: toDateInputValue(new Date()), search: '', gender: '', bloodGroup: '', departmentId: '' };
}

function toQuery(filters: Filters, page: number, pageSize: number, sort: string): PatientListQuery {
  return {
    from: filters.from,
    to: filters.to,
    search: filters.search || undefined,
    gender: filters.gender || undefined,
    bloodGroup: filters.bloodGroup || undefined,
    departmentId: filters.departmentId || undefined,
    page,
    pageSize,
    sort,
  };
}

/** Patient Reports MVP — search/filter/table/export, replacing the earlier chart-based version
 * at this same route (kept in the repo, unrouted, as PatientAnalyticsChartsPage.tsx, per the
 * user's own "charts are a future phase" framing). Every number and row comes from real API
 * calls — no mock data, no client-side pagination (server does filtering/sorting/paging; see
 * PatientsController's report/report-summary/report-export actions and
 * PatientRepository.BuildFilteredQuery). */
export default function PatientReportsPage() {
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;

  const [filters, setFilters] = useState<Filters>(defaultFilters);
  const [appliedFilters, setAppliedFilters] = useState<Filters>(defaultFilters);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(50);
  const [sort, setSort] = useState('-createdAt');
  const [isExporting, setIsExporting] = useState(false);

  const query = toQuery(appliedFilters, page, pageSize, sort);
  const queryKey = ['patients', 'report', query] as const;

  const {
    data: reportData,
    isPending,
    isError,
    refetch,
    isFetching,
  } = useQuery({
    queryKey,
    queryFn: () => patientsApi.getPatientsReport(query),
  });

  const { data: summary, isPending: isSummaryPending } = useQuery({
    queryKey: ['patients', 'report-summary', appliedFilters],
    queryFn: () => patientsApi.getPatientsReportSummary(toQuery(appliedFilters, 1, 1, sort)),
  });

  // Primes the department reference cache so resolveRecordLabel (used for the table's
  // Department/Last Visit-department cells below) resolves real names on first render — same
  // reasoning as ProfitReportPage's own priming hooks.
  useMasterOptionsQuery('department');
  const { data: departments } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  function handleApplyFilters() {
    setAppliedFilters(filters);
    setPage(1);
  }

  function handleReset() {
    const reset = defaultFilters();
    setFilters(reset);
    setAppliedFilters(reset);
    setPage(1);
  }

  function handlePageSizeChange(value: string) {
    setPageSize(Number(value));
    setPage(1);
  }

  function toggleSort(field: string) {
    setSort((current) => (current === field ? `-${field}` : field));
    setPage(1);
  }

  function sortIndicator(field: string): string {
    if (sort === field) return ' ▲';
    if (sort === `-${field}`) return ' ▼';
    return '';
  }

  async function handleExport() {
    setIsExporting(true);
    try {
      const blob = await patientsApi.exportPatientsReport(toQuery(appliedFilters, 1, 1, sort));
      downloadBlob(blob, `patient-report-${appliedFilters.from}-to-${appliedFilters.to}.xlsx`);
    } catch {
      toast({ title: 'Export failed', description: 'Could not export the patient report. Please try again.', variant: 'error' });
    } finally {
      setIsExporting(false);
    }
  }

  function handleRefresh() {
    void refetch();
    void queryClient.invalidateQueries({ queryKey: ['patients', 'report-summary'] });
  }

  function handlePrint() {
    window.print();
  }

  const rows = reportData?.items ?? [];
  const meta = reportData?.meta;

  const appliedFilterSummary = [
    appliedFilters.search ? `Search: ${appliedFilters.search}` : null,
    appliedFilters.gender ? `Gender: ${appliedFilters.gender}` : null,
    appliedFilters.bloodGroup ? `Blood Group: ${bloodGroupLabel(appliedFilters.bloodGroup)}` : null,
    appliedFilters.departmentId ? `Department: ${resolveRecordLabel('department', appliedFilters.departmentId)}` : null,
  ].filter((part): part is string => part !== null);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Users}
        title="Patient Reports"
        subtitle="Patient registration and patient activity reports"
        className="print:hidden"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8 print:hidden">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-semibold text-foreground">Patient Reports</h2>
          <div className="flex items-center gap-2">
            <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleRefresh} disabled={isFetching}>
              <RefreshCw className={isFetching ? 'h-4 w-4 animate-spin' : 'h-4 w-4'} />
              Refresh
            </Button>
            <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleExport} disabled={isExporting}>
              <FileSpreadsheet className="h-4 w-4" />
              {isExporting ? 'Exporting…' : 'Export Excel'}
            </Button>
            <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handlePrint}>
              <Printer className="h-4 w-4" />
              Print
            </Button>
          </div>
        </div>

        <Card>
          <CardContent className="flex flex-col gap-3 py-4">
            <div className="flex flex-wrap items-end gap-3">
              <div className="flex flex-col gap-1">
                <Label htmlFor="report-from">From Date</Label>
                <Input
                  id="report-from"
                  type="date"
                  value={filters.from}
                  max={filters.to}
                  onChange={(e) => setFilters({ ...filters, from: e.target.value })}
                  className="w-44"
                />
              </div>
              <div className="flex flex-col gap-1">
                <Label htmlFor="report-to">To Date</Label>
                <Input
                  id="report-to"
                  type="date"
                  value={filters.to}
                  min={filters.from}
                  onChange={(e) => setFilters({ ...filters, to: e.target.value })}
                  className="w-44"
                />
              </div>
              <div className="flex min-w-[240px] flex-1 flex-col gap-1">
                <Label htmlFor="report-search">Search Patient</Label>
                <Input
                  id="report-search"
                  placeholder="Patient Name / UHID / Phone Number"
                  value={filters.search}
                  onChange={(e) => setFilters({ ...filters, search: e.target.value })}
                />
              </div>
            </div>

            <div className="flex flex-wrap items-end gap-3">
              <div className="flex flex-col gap-1">
                <Label htmlFor="report-gender">Gender</Label>
                <Select value={filters.gender || 'all'} onValueChange={(v) => setFilters({ ...filters, gender: v === 'all' ? '' : (v as Gender) })}>
                  <SelectTrigger id="report-gender" className="w-40">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Genders</SelectItem>
                    {GENDERS.map((g) => (
                      <SelectItem key={g} value={g}>
                        {g === 'NA' ? 'N/A' : g}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-1">
                <Label htmlFor="report-blood-group">Blood Group</Label>
                <Select
                  value={filters.bloodGroup || 'all'}
                  onValueChange={(v) => setFilters({ ...filters, bloodGroup: v === 'all' ? '' : (v as BloodGroup) })}
                >
                  <SelectTrigger id="report-blood-group" className="w-40">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Blood Groups</SelectItem>
                    {BLOOD_GROUPS.map((bg) => (
                      <SelectItem key={bg} value={bg}>
                        {bloodGroupLabel(bg)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-1">
                <Label htmlFor="report-department">Department</Label>
                <Select value={filters.departmentId || 'all'} onValueChange={(v) => setFilters({ ...filters, departmentId: v === 'all' ? '' : v })}>
                  <SelectTrigger id="report-department" className="w-56">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Departments</SelectItem>
                    {(departments?.items ?? []).map((d) => (
                      <SelectItem key={d.id} value={d.id}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="ml-auto flex gap-2">
                <Button type="button" variant="outline" onClick={handleReset}>
                  Reset
                </Button>
                <Button type="button" onClick={handleApplyFilters}>
                  Apply Filters
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <Card>
            <CardContent className="flex flex-col gap-1 py-4">
              <span className="text-xs text-muted-foreground">Total Patients</span>
              <span className="text-lg font-semibold text-foreground">{isSummaryPending ? '—' : summary?.totalPatients ?? 0}</span>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="flex flex-col gap-1 py-4">
              <span className="text-xs text-muted-foreground">New Patients</span>
              <span className="text-lg font-semibold text-foreground">{isSummaryPending ? '—' : summary?.newPatients ?? 0}</span>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="flex flex-col gap-1 py-4">
              <span className="text-xs text-muted-foreground">Returning Patients</span>
              <span className="text-lg font-semibold text-foreground">{isSummaryPending ? '—' : summary?.returningPatients ?? 0}</span>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="flex flex-col gap-1 py-4">
              <span className="text-xs text-muted-foreground">Total Visits</span>
              <span className="text-lg font-semibold text-foreground">{isSummaryPending ? '—' : summary?.totalVisits ?? 0}</span>
            </CardContent>
          </Card>
        </div>

        <div className="flex flex-col gap-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              Patient Records <span className="font-normal text-muted-foreground">({meta?.totalCount ?? 0})</span>
            </h2>
            <div className="flex items-center gap-2">
              <Label htmlFor="report-page-size" className="text-xs text-muted-foreground">
                Rows per page
              </Label>
              <Select value={String(pageSize)} onValueChange={handlePageSizeChange}>
                <SelectTrigger id="report-page-size" className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZES.map((size) => (
                    <SelectItem key={size} value={String(size)}>
                      {size}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading patient report…
            </div>
          )}

          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Unable to load patient report. Please try again.
            </p>
          )}

          {!isPending && !isError && rows.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                <p className="text-sm font-medium text-foreground">No patients found</p>
              </CardContent>
            </Card>
          )}

          {!isPending && !isError && rows.length > 0 && (
            <>
              <div className="overflow-x-auto rounded-lg border border-border">
                <table className="w-full min-w-[960px] text-sm">
                  <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="cursor-pointer select-none px-4 py-2.5" onClick={() => toggleSort('uhid')}>
                        UHID{sortIndicator('uhid')}
                      </th>
                      <th className="cursor-pointer select-none px-4 py-2.5" onClick={() => toggleSort('firstName')}>
                        Patient{sortIndicator('firstName')}
                      </th>
                      <th className="px-4 py-2.5">Age</th>
                      <th className="px-4 py-2.5">Gender</th>
                      <th className="px-4 py-2.5">Phone</th>
                      <th className="cursor-pointer select-none px-4 py-2.5" onClick={() => toggleSort('createdAt')}>
                        Registration Date{sortIndicator('createdAt')}
                      </th>
                      <th className="px-4 py-2.5">Department</th>
                      <th className="px-4 py-2.5">Last Visit</th>
                      <th className="px-4 py-2.5">Status</th>
                      <th className="px-4 py-2.5">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {rows.map((row: PatientReportRow) => (
                      <tr key={row.patient.id} className="hover:bg-muted/30">
                        <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{row.patient.uhid}</td>
                        <td className="px-4 py-3">
                          <PatientNameLink patientId={row.patient.id}>
                            {row.patient.firstName} {row.patient.lastName}
                          </PatientNameLink>
                        </td>
                        <td className="px-4 py-3 text-muted-foreground">{row.patient.age}</td>
                        <td className="px-4 py-3 text-muted-foreground">{row.patient.gender === 'NA' ? 'N/A' : row.patient.gender}</td>
                        <td className="px-4 py-3 text-muted-foreground">{row.patient.primaryPhone}</td>
                        <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                          {new Date(row.patient.createdAt).toLocaleDateString('en-IN')}
                        </td>
                        <td className="px-4 py-3 text-muted-foreground">
                          {row.lastVisitDepartmentId ? resolveRecordLabel('department', row.lastVisitDepartmentId) : '—'}
                        </td>
                        <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                          {row.lastVisitAt ? new Date(row.lastVisitAt).toLocaleDateString('en-IN') : '—'}
                        </td>
                        <td className="px-4 py-3">
                          {row.patient.requiresDataVerification ? (
                            <Badge variant="warning">Needs Verification</Badge>
                          ) : (
                            <Badge variant="success">Verified</Badge>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <Button asChild variant="ghost" size="sm">
                            <Link to={`/patients/registration/${row.patient.id}`}>View</Link>
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {meta && <Pagination meta={meta} onPageChange={setPage} />}
            </>
          )}
        </div>
      </div>

      {/* Printed view — hidden on screen at all times, shown (with everything else hidden)
       * only once printing starts. See index.css's `.print-target` rule and
       * InvoicePrintTemplate for the same convention elsewhere in this app. */}
      <div className="print-target hidden bg-white p-8 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
        <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-4 text-center">
          <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
          <span className="text-xs text-gray-600">Patient Report</span>
        </div>

        <div className="mt-4 flex items-start justify-between gap-6 text-sm">
          <span>
            {appliedFilters.from} to {appliedFilters.to}
            {appliedFilterSummary.length > 0 ? ` · ${appliedFilterSummary.join(' · ')}` : ''}
          </span>
          <span className="whitespace-nowrap text-gray-600">Printed {new Date().toLocaleString('en-IN')}</span>
        </div>

        <table className="mt-4 w-full border-collapse text-xs">
          <thead>
            <tr className="border-y-2 border-black">
              <th className="py-1.5 pr-2 text-left font-semibold">UHID</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Patient</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Age</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Gender</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Phone</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Registration Date</th>
              <th className="py-1.5 pr-2 text-left font-semibold">Department</th>
              <th className="py-1.5 text-left font-semibold">Last Visit</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.patient.id} className="border-b border-gray-300">
                <td className="py-1.5 pr-2 align-top">{row.patient.uhid}</td>
                <td className="py-1.5 pr-2 align-top">
                  {row.patient.firstName} {row.patient.lastName}
                </td>
                <td className="py-1.5 pr-2 align-top">{row.patient.age}</td>
                <td className="py-1.5 pr-2 align-top">{row.patient.gender === 'NA' ? 'N/A' : row.patient.gender}</td>
                <td className="py-1.5 pr-2 align-top">{row.patient.primaryPhone}</td>
                <td className="py-1.5 pr-2 align-top">{new Date(row.patient.createdAt).toLocaleDateString('en-IN')}</td>
                <td className="py-1.5 pr-2 align-top">
                  {row.lastVisitDepartmentId ? resolveRecordLabel('department', row.lastVisitDepartmentId) : '—'}
                </td>
                <td className="py-1.5 align-top">{row.lastVisitAt ? new Date(row.lastVisitAt).toLocaleDateString('en-IN') : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="mt-4 flex justify-end border-t-2 border-black pt-2 text-sm font-bold">
          <span>Total: {meta?.totalCount ?? rows.length}</span>
        </div>
      </div>
    </div>
  );
}
