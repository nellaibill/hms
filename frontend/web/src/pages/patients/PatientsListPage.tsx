import type { Patient } from '@hms/shared';
import { Loader2, Search, UserPlus2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
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

/** "Old Patient Registration" — the Reception & Registration hub's existing-patient search + list (docs/ScreenInventory.md). */
export default function PatientsListPage() {
  const [filters, setFilters] = useState<PatientSearchFilters>(emptyPatientSearchFilters);
  const [activeFilters, setActiveFilters] = useState<PatientSearchFilters | null>(null);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');
  const [patientPendingDelete, setPatientPendingDelete] = useState<Patient | null>(null);

  const hasSearched = activeFilters !== null;

  const { data, isPending, isError, error } = usePatientsQuery(
    {
      page,
      pageSize: RESULTS_PAGE_SIZE,
      sort,
      name: activeFilters?.name.trim() || undefined,
      age: activeFilters?.age.trim() ? Number(activeFilters.age) : undefined,
      uhid: activeFilters?.uhid.trim() || undefined,
      phone: activeFilters?.phone.trim() || undefined,
    },
    { enabled: hasSearched },
  );

  const deleteMutation = useDeletePatientMutation();
  const { toast } = useToast();

  function handleFilterChange(field: keyof PatientSearchFilters, value: string) {
    setFilters((prev) => ({ ...prev, [field]: value }));
  }

  function handleSearch() {
    setActiveFilters(filters);
    setPage(1);
  }

  function handleClear() {
    setFilters(emptyPatientSearchFilters);
    setActiveFilters(null);
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
      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        {data?.source === 'mock' && (
          <span className="absolute right-6 top-5 hidden rounded-full bg-page-banner-foreground/15 px-2.5 py-1 text-xs font-medium sm:inline-block">
            Demo data — API not connected
          </span>
        )}
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserPlus2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Old Patient Registration</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Find an existing patient by name, age, UHID, or phone to view or update their registration.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <PatientListToolbar filters={filters} onFilterChange={handleFilterChange} onSearch={handleSearch} onClear={handleClear} />

      {!hasSearched && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <Search className="h-6 w-6 text-muted-foreground" />
            <p className="text-sm font-medium text-foreground">Search for a patient</p>
            <p className="text-sm text-muted-foreground">
              Enter a name, age, UHID, or phone number above — use one field or combine several — then click Search.
            </p>
          </CardContent>
        </Card>
      )}

      {hasSearched && isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Searching…
        </div>
      )}

      {hasSearched && isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to search patients.'}
        </p>
      )}

      {hasSearched && !isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No patients found matching the search criteria.</p>
            <p className="text-sm text-muted-foreground">Try a different or broader combination of search fields.</p>
          </CardContent>
        </Card>
      )}

      {hasSearched && !isPending && !isError && data && data.items.length > 0 && (
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
