import { Loader2, Plus, Receipt } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { useAuth } from '@/features/auth/AuthContext';
import { StockReceiptTable, useStockReceiptsQuery } from '@/features/pharmacy/stock-receipts';

export default function StockReceiptsListPage() {
  const [page, setPage] = useState(1);
  const { hasPermission } = useAuth();

  const { data, isPending, isError, error } = useStockReceiptsQuery({ page, pageSize: 20 });

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/pharmacy" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to Pharmacy
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Receipt className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Stock Receipts</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">History of stock received against products/batches.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          {hasPermission('pharmacy.create') && (
            <Button asChild className="ml-auto gap-1.5">
              <Link to="/pharmacy/stock-receipts/new">
                <Plus className="h-4 w-4" />
                Receive Stock
              </Link>
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading stock receipts…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load stock receipts.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No stock receipts found</p>
              <p className="text-sm text-muted-foreground">Nothing here yet.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <StockReceiptTable receipts={data.items} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
