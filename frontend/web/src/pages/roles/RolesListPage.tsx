import { Loader2, ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { Pagination, RoleListToolbar, RoleTable, useRolesQuery, type RoleStatus } from '../../features/roles';

export default function RolesListPage() {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<RoleStatus | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('name');

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useRolesQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    status,
  });

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleStatusChange(value: RoleStatus | undefined) {
    setStatus(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <div className="flex items-start gap-3 border-b border-border pb-3">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <ShieldCheck className="h-5 w-5" />
        </span>
        <div className="flex flex-1 items-start justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">Roles Management</h1>
            <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
              Define roles and their module-level permissions across the HMS.
            </p>
          </div>
          <span className="mt-0.5 shrink-0 rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning">
            Demo data — no backend yet
          </span>
        </div>
      </div>

      <RoleListToolbar search={search} onSearchChange={handleSearchChange} status={status} onStatusChange={handleStatusChange} />

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading roles…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load roles.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No roles found</p>
            <p className="text-sm text-muted-foreground">
              {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Add the first role to get started.'}
            </p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <RoleTable roles={data.items} sort={sort} onSortChange={handleSortChange} />
          <Pagination meta={data.meta} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
