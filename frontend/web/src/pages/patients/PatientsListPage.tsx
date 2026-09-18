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

/** "Old Patient Registration" — the Reception & Registration hub's existing-patient search + list (docs/ScreenInventory.md).
 * Loads the most recently registered patients by default (same "show data immediately, narrow
 * it from there" pattern as the OPD Patient List) rather than gating every row behind an
 * explicit Search click first — Search/Clear still narrow or reset that same list in place. */
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
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');
  const [patientPendingDelete, setPatientPendingDelete] = useState<Patient | null>(null);

  const { data, isPending, isError, error } = usePatientsQuery({
    page,
    pageSize: RESULTS_PAGE_SIZE,
    sort,
    name: appliedFilters.name.trim() || undefined,
    age: appliedFilters.age.trim() ? Number(appliedFilters.age) : undefined,
    uhid: appliedFilters.uhid.trim() || undefined,
    phone: appliedFilters.phone.trim() || undefined,
    requiresDataVerification: appliedFilters.needsVerification || undefined,
  });

  const isFiltered =
    appliedFilters.needsVerification || [appliedFilters.name, appliedFilters.age, appliedFilters.uhid, appliedFilters.phone].some((v) => v.trim() !== '');

  const deleteMutation = useDeletePatientMutation();
  const { toast } = useToast();

  function handleFilterChange(field: keyof PatientSearchFilters, value: string | boolean) {
    setFilters((prev) => ({ ...prev, [field]: value }));
  }

  function handleSearch() {
    setAppliedFilters(filters);
    setPage(1);
  }

  function handleClear() {
    setFilters(emptyPatientSearchFilters);
    setAppliedFilters(emptyPatientSearchFilters);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
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

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading patients…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load patients.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">
              {isFiltered ? 'No patients found matching the search criteria.' : 'No patients registered yet.'}
            </p>
            {isFiltered && <p className="text-sm text-muted-foreground">Try a different or broader combination of search fields.</p>}
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <div className="max-h-[65vh] overflow-y-auto rounded-lg">
            <PatientTable patients={data.items} sort={sort} onSortChange={handleSortChange} onDeleteRequested={setPatientPendingDelete} />
          </div>
          <Pagination meta={data.meta} onPageChange={setPage} />
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
