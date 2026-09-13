import { Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Card, CardContent } from '@/components/ui/card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { getMasterConfig, MasterListToolbar, MasterTable, Pagination, useMastersQuery } from '@/features/masters';

export default function MasterListPage() {
  const { entityKey } = useParams<{ entityKey: string }>();
  const config = getMasterConfig(entityKey);

  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [filters, setFilters] = useState<Record<string, string | undefined>>({});
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState(config?.nameField ?? config?.codeField ?? 'updatedAt');

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useMastersQuery(entityKey ?? '', {
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    isActive,
    filters,
  });

  if (!config) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Unknown Masters entity "{entityKey}".
        </p>
        <Link to="/admin/masters" className="mt-3 inline-block text-sm text-primary hover:underline">
          Back to Masters
        </Link>
      </div>
    );
  }

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleIsActiveChange(value: boolean | undefined) {
    setIsActive(value);
    setPage(1);
  }

  function handleFilterChange(key: string, value: string | undefined) {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  const Icon = config.icon;

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Icon}
        title={config.labelPlural}
        subtitle={config.description}
        backTo="/admin/masters"
        backLabel="Back to Masters"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <MasterListToolbar
          entityKey={config.key}
          entityLabel={config.label}
          search={search}
          onSearchChange={handleSearchChange}
          isActive={isActive}
          onIsActiveChange={handleIsActiveChange}
          config={config}
          filters={filters}
          onFilterChange={handleFilterChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading {config.labelPlural.toLowerCase()}…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : `Failed to load ${config.labelPlural.toLowerCase()}.`}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No {config.labelPlural.toLowerCase()} found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : `Add the first ${config.label.toLowerCase()} to get started.`}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <MasterTable config={config} records={data.items} sort={sort} onSortChange={handleSortChange} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
