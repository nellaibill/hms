import type { Patient } from '@hms/shared';
import { Loader2, UserPlus2 } from 'lucide-react';
import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { useToast } from '@/components/ui/toast-context';
import {
  DeletePatientDialog,
  emptyPatientSearchFilters,
  Pagination,
  PatientListToolbar,
  PatientTable,
  useDeletePatientMutation,
  usePatientsQuery,
  type PatientSearchFilters,
} from '../../features/patients';
import { RequirePermission } from '../../features/auth/RequirePermission';

const RESULTS_PAGE_SIZE = 100;
const RECENT_VISITS_PAGE_SIZE = 20;
const RECENT_VISITS_LIMIT = 100;
const RECENT_VISITS_MAX_PAGE = RECENT_VISITS_LIMIT / RECENT_VISITS_PAGE_SIZE;

/** "Old Patient Registration" — the Reception & Registration hub's existing-patient search + list (docs/ScreenInventory.md).
 * Loads the same default "Last 100 visits" list OPD Billing Entry's PatientPicker shows before
 * any search — the 100 most recently active patients by last visit (not registration date),
 * 20 per page — rather than gating every row behind an explicit Search click first. Search/
 * Clear still narrow to (or reset from) the broader, precisely-filtered query below it. */
export default function PatientsListPage() {
  const navigate = useNavigate();
  // The header's Patient Search box lands here with a typed-but-unpicked query via router state
  // (see HeaderSearchBox) — seed both the visible field and the applied filters so results show
  // immediately instead of requiring a second, redundant click on Search.
  const location = useLocation();
  const initialName = (location.state as { name?: string } | null)?.name?.trim();
  const initialFilters: PatientSearchFilters = initialName
    ? { ...emptyPatientSearchFilters, name: initialName }
    : emptyPatientSearchFilters;
  const [filters, setFilters] = useState<PatientSearchFilters>(initialFilters);
  const [appliedFilters, setAppliedFilters] = useState<PatientSearchFilters>(initialFilters);
  const [resultsPage, setResultsPage] = useState(1);
  const [recentPage, setRecentPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');
  const [patientPendingDelete, setPatientPendingDelete] = useState<Patient | null>(null);

  const isFiltered =
    appliedFilters.needsVerification || [appliedFilters.name, appliedFilters.age, appliedFilters.uhid, appliedFilters.phone].some((v) => v.trim() !== '');

  const { data, isPending, isError, error } = usePatientsQuery(
    {
      page: resultsPage,
      pageSize: RESULTS_PAGE_SIZE,
      sort,
      name: appliedFilters.name.trim() || undefined,
      age: appliedFilters.age.trim() ? Number(appliedFilters.age) : undefined,
      uhid: appliedFilters.uhid.trim() || undefined,
      phone: appliedFilters.phone.trim() || undefined,
      requiresDataVerification: appliedFilters.needsVerification || undefined,
      // Populates lastVisit*/Consultant/Department/AppointmentTime — same OPD Billing Entry's
      // PatientPicker already turns on, so this list can show the same at-a-glance visit context.
      includeLastVisit: true,
    },
    { enabled: isFiltered },
  );

  // Default (no filters applied) — same query PatientPicker's own "Last 100 visits" list uses:
  // sorted by most recent visit activity, not registration date, capped at 100 total even
  // though the server's own count reflects the whole patient list.
  const {
    data: recentData,
    isPending: isRecentPending,
    isError: isRecentError,
    error: recentError,
  } = usePatientsQuery(
    { page: recentPage, pageSize: RECENT_VISITS_PAGE_SIZE, sort: '-lastVisitAt', includeLastVisit: true },
    { enabled: !isFiltered },
  );
  const recentMeta = recentData
    ? {
        ...recentData.meta,
        totalCount: Math.min(recentData.meta.totalCount, RECENT_VISITS_LIMIT),
        totalPages: Math.min(recentData.meta.totalPages, RECENT_VISITS_MAX_PAGE),
      }
    : undefined;

  const deleteMutation = useDeletePatientMutation();
  const { toast } = useToast();

  function handleFilterChange(field: keyof PatientSearchFilters, value: string | boolean) {
    setFilters((prev) => ({ ...prev, [field]: value }));
  }

  function handleSearch() {
    setAppliedFilters(filters);
    setResultsPage(1);
  }

  function handleClear() {
    setFilters(emptyPatientSearchFilters);
    setAppliedFilters(emptyPatientSearchFilters);
    setRecentPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setResultsPage(1);
  }

  function handleConfirmDelete() {
    if (!patientPendingDelete) {
      return;
    }
    const { firstName, lastName, uhid } = patientPendingDelete;
    deleteMutation.mutate(patientPendingDelete.id, {
      onSuccess: () => setPatientPendingDelete(null),
      // Deletion previously had no failure feedback at all — the dialog just sat there
      // with no explanation. Keep it open (rather than dismissing as if it worked) so the
      // user can see the reason and retry or cancel.
      onError: (err) =>
        toast({
          title: 'Delete failed',
          description: `Could not delete ${firstName} ${lastName} (UHID ${uhid}): ${err.message}`,
          variant: 'error',
        }),
    });
  }

  return (
    <RequirePermission permission="patient-management.view">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={UserPlus2}
        title="Old Patient Registration"
        subtitle="Find an existing patient by name, age, UHID, or phone to view or update their registration."
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <PatientListToolbar
        filters={filters}
        onFilterChange={handleFilterChange}
        onSearch={handleSearch}
        onClear={handleClear}
        onSuggestionSelect={(patient) => navigate(`/patients/registration/${patient.id}`)}
      />

      {!isFiltered && (
        <div className="flex flex-col gap-3">
          <p className="text-sm font-medium text-foreground">Last 100 visits</p>

          {isRecentPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading patients…
            </div>
          )}

          {!isRecentPending && isRecentError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {recentError instanceof Error ? recentError.message : 'Failed to load patients.'}
            </p>
          )}

          {!isRecentPending && !isRecentError && recentData && recentData.items.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                <p className="text-sm font-medium text-foreground">No patients registered yet.</p>
              </CardContent>
            </Card>
          )}

          {!isRecentPending && !isRecentError && recentData && recentData.items.length > 0 && (
            <>
              <div className="max-h-[65vh] overflow-y-auto rounded-lg">
                <PatientTable
                  patients={recentData.items}
                  sort={sort}
                  onSortChange={handleSortChange}
                  onDeleteRequested={setPatientPendingDelete}
                  sortable={false}
                />
              </div>
              {recentMeta && <Pagination meta={recentMeta} onPageChange={setRecentPage} />}
            </>
          )}
        </div>
      )}

      {isFiltered && isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading patients…
        </div>
      )}

      {isFiltered && isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load patients.'}
        </p>
      )}

      {isFiltered && !isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No patients found matching the search criteria.</p>
            <p className="text-sm text-muted-foreground">Try a different or broader combination of search fields.</p>
          </CardContent>
        </Card>
      )}

      {isFiltered && !isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <div className="max-h-[65vh] overflow-y-auto rounded-lg">
            <PatientTable patients={data.items} sort={sort} onSortChange={handleSortChange} onDeleteRequested={setPatientPendingDelete} />
          </div>
          <Pagination meta={data.meta} onPageChange={setResultsPage} />
        </div>
      )}

      {patientPendingDelete && (
        <DeletePatientDialog
          patient={patientPendingDelete}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setPatientPendingDelete(null)}
        />
      )}
      </div>
    </div>
    </RequirePermission>
  );
}
