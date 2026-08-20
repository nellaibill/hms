import { ArrowRight, Loader2, PackagePlus, Pill, Receipt } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { useProductsQuery } from '@/features/pharmacy/product-lookup';
import { StockBalanceTable, useStockBalancesQuery } from '@/features/pharmacy/stock-balances';

interface HubCard {
  key: string;
  label: string;
  description: string;
  icon: typeof Pill;
  path: string;
}

/**
 * Pharmacy's landing page ('/pharmacy') — current stock balances (flagging low/zero
 * stock), plus links into Receive Stock, Dispense, and the combined Stock Ledger. Mirrors
 * IpdDashboardPage's hub shape.
 */
export default function PharmacyHubPage() {
  const { hasPermission } = useAuth();
  const balancesQuery = useStockBalancesQuery({ page: 1, pageSize: 50, sort: 'productName' });

  // Loaded once for the whole list (not per-row) to resolve ProductId -> reorderLevel for
  // the low-stock flag, the same "resolve via a separate 100-row lookup query" pattern
  // BedsListPage uses for ward labels.
  const productsForReorderLevels = useProductsQuery({ pageSize: 100, isActive: true });
  const reorderLevelsByProductId = Object.fromEntries(
    (productsForReorderLevels.data?.items ?? []).map((product) => [product.id, product.reorderLevel]),
  );

  const cards: HubCard[] = [
    { key: 'receive', label: 'Receive Stock', description: 'Record newly received stock against a product/batch.', icon: PackagePlus, path: '/pharmacy/stock-receipts/new' },
    { key: 'dispense', label: 'Dispense', description: 'Issue stock directly to a patient.', icon: Pill, path: '/pharmacy/dispenses/new' },
    { key: 'receipts', label: 'Stock Receipts', description: 'History of stock received.', icon: Receipt, path: '/pharmacy/stock-receipts' },
    { key: 'dispenses', label: 'Dispenses', description: 'History of stock dispensed to patients.', icon: Pill, path: '/pharmacy/dispenses' },
    { key: 'ledger', label: 'Stock Ledger', description: 'Combined, filterable receipt + dispense history.', icon: Receipt, path: '/pharmacy/stock-ledger' },
  ];

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Pill className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Pharmacy</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Stock/batch/expiry tracking and direct-dispense to patients.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
        {hasPermission('pharmacy.create') && (
          <section className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Actions</h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              {cards.slice(0, 2).map((card) => {
                const Icon = card.icon;
                return (
                  <Link key={card.key} to={card.path} className="block">
                    <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                      <CardHeader>
                        <div className="flex items-center justify-between">
                          <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                            <Icon className="h-4.5 w-4.5" />
                          </span>
                          <ArrowRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                        <CardTitle className="text-base">{card.label}</CardTitle>
                        <CardDescription>{card.description}</CardDescription>
                      </CardHeader>
                    </Card>
                  </Link>
                );
              })}
            </div>
          </section>
        )}

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Browse</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {cards.slice(2).map((card) => {
              const Icon = card.icon;
              return (
                <Link key={card.key} to={card.path} className="block">
                  <Card className="h-full transition-all hover:border-primary/40 hover:bg-accent/40 hover:shadow-soft-lg">
                    <CardHeader>
                      <div className="flex items-center justify-between">
                        <span className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                          <Icon className="h-4.5 w-4.5" />
                        </span>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <CardTitle className="text-base">{card.label}</CardTitle>
                      <CardDescription>{card.description}</CardDescription>
                    </CardHeader>
                  </Card>
                </Link>
              );
            })}
          </div>
        </section>

        <section className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Current Stock Balances</h2>

          {balancesQuery.isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading stock balances…
            </div>
          )}

          {balancesQuery.isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {balancesQuery.error instanceof Error ? balancesQuery.error.message : 'Failed to load stock balances.'}
            </p>
          )}

          {!balancesQuery.isPending && !balancesQuery.isError && balancesQuery.data && balancesQuery.data.items.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                <p className="text-sm font-medium text-foreground">No stock balances yet</p>
                <p className="text-sm text-muted-foreground">Record a stock receipt to get started.</p>
              </CardContent>
            </Card>
          )}

          {!balancesQuery.isPending && !balancesQuery.isError && balancesQuery.data && balancesQuery.data.items.length > 0 && (
            <StockBalanceTable balances={balancesQuery.data.items} reorderLevelsByProductId={reorderLevelsByProductId} />
          )}
        </section>
      </div>
    </div>
  );
}
