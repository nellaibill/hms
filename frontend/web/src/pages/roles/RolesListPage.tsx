import { Loader2, ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Card, CardContent } from '@/components/ui/card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { Pagination, RoleListToolbar, RoleTable, useRolesQuery, type RoleStatus } from '../../features/roles';
import { RequirePermission } from '../../features/auth/RequirePermission';

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
    <RequirePermission permission="identity-administration.view">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ShieldCheck}
        title="Roles Management"
        subtitle="Define roles and their module-level permissions across the HMS."
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
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
    </div>
    </RequirePermission>
  );
}
