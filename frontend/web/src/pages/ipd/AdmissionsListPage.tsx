import type { AdmissionStatus } from '@hms/shared';
import { ClipboardList, Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Pagination } from '@/components/Pagination';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { AdmissionTable, useAdmissionsQuery } from '../../features/ipd/admissions';

export default function AdmissionsListPage() {
  const [status, setStatus] = useState<AdmissionStatus>('Admitted');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useAdmissionsQuery({
    page,
    pageSize: 20,
    search: debouncedSearch || undefined,
    status,
  });

  function handleStatusChange(value: string) {
    setStatus(value as AdmissionStatus);
    setPage(1);
  }

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to IPD
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <ClipboardList className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Admissions</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Admitted and discharged inpatients.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Tabs value={status} onValueChange={handleStatusChange}>
          <TabsList>
            <TabsTrigger value="Admitted">Admitted Patients</TabsTrigger>
            <TabsTrigger value="Discharged">Discharged Patients</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="flex flex-wrap items-center gap-3">
          <Input
            type="search"
            placeholder="Search by admission number…"
            value={search}
            onChange={(event) => handleSearchChange(event.target.value)}
            aria-label="Search admissions"
            className="w-64"
          />
          <Button asChild className="ml-auto gap-1.5">
            <Link to="/clinical/ipd/admissions/new">
              <Plus className="h-4 w-4" />
              New Admission
            </Link>
          </Button>
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading admissions…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load admissions.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">
                No {status === 'Admitted' ? 'admitted' : 'discharged'} patients found
              </p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Nothing here yet.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <AdmissionTable admissions={data.items} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
