import type { Patient } from '@hms/shared';
import { Loader2, UserPlus2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import {
  DeletePatientDialog,
  Pagination,
  PatientListToolbar,
  PatientTable,
  useDeletePatientMutation,
  usePatientsQuery,
} from '../../features/patients';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';

/** Registration Landing screen (docs/ScreenInventory.md) — list + search + "New Patient" entry point. */
export default function PatientsListPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');
  const [patientPendingDelete, setPatientPendingDelete] = useState<Patient | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = usePatientsQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
  });

  const deleteMutation = useDeletePatientMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
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
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div className="flex items-start gap-3 border-b border-border pb-4">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <UserPlus2 className="h-5 w-5" />
        </span>
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-foreground">Reception &amp; Registration</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Register new patients or search existing ones by name, UHID, or phone.
          </p>
        </div>
      </div>

      <PatientListToolbar search={search} onSearchChange={handleSearchChange} />

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
            <p className="text-sm font-medium text-foreground">No patients found</p>
            <p className="text-sm text-muted-foreground">
              {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Register the first patient to get started.'}
            </p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <PatientTable
            patients={data.items}
            sort={sort}
            onSortChange={handleSortChange}
            onDeleteRequested={setPatientPendingDelete}
          />
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
