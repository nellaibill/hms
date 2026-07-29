import type { Patient } from '@hms/shared';
import { Loader2, Search, UserPlus2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
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
    deleteMutation.mutate(patientPendingDelete.id, {
      onSuccess: () => setPatientPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <div className="flex items-start gap-3 border-b border-border pb-3">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <UserPlus2 className="h-5 w-5" />
        </span>
        <div className="flex flex-1 items-start justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-primary">Old Patient Registration</h1>
            <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
              Find an existing patient by name, age, UHID, or phone to view or update their registration.
            </p>
          </div>
          {data?.source === 'mock' && (
            <span className="mt-0.5 shrink-0 rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning">
              Demo data — API not connected
            </span>
          )}
        </div>
      </div>

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
  );
}
