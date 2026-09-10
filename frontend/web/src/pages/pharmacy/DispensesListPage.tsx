import { Loader2, Pill, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { useAuth } from '@/features/auth/AuthContext';
import { DispenseTable, useDispensesQuery } from '@/features/pharmacy/dispenses';

export default function DispensesListPage() {
  const [page, setPage] = useState(1);
  const { hasPermission } = useAuth();

  const { data, isPending, isError, error } = useDispensesQuery({ page, pageSize: 20 });

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Pill}
        title="Dispenses"
        subtitle="History of stock dispensed to patients."
        backTo="/pharmacy"
        backLabel="Back to Pharmacy"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          {hasPermission('pharmacy.create') && (
            <Button asChild className="ml-auto gap-1.5">
              <Link to="/pharmacy/dispenses/new">
                <Plus className="h-4 w-4" />
                Dispense
              </Link>
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading dispenses…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load dispenses.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No dispenses found</p>
              <p className="text-sm text-muted-foreground">Nothing here yet.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <DispenseTable dispenses={data.items} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
