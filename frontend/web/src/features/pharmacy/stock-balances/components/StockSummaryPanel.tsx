import type { StockStatus, StockSummaryFilter, StockSummaryResponse } from '@hms/shared';
import { ChevronDown, ChevronRight, Loader2, Search } from 'lucide-react';
import { Fragment, useState } from 'react';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { useStockBalancesQuery } from '../hooks/useStockBalancesQuery';
import { useStockSummaryQuery } from '../hooks/useStockSummaryQuery';
import { StockBalanceTable } from './StockBalanceTable';

const ALL = 'all';
type FilterTab = typeof ALL | StockSummaryFilter;

const FILTER_TABS: { value: FilterTab; label: string }[] = [
  { value: ALL, label: 'All products' },
  { value: 'NeedsReorder', label: 'Needs reorder' },
  { value: 'ExpiringSoon', label: 'Expiring soon' },
  { value: 'Expired', label: 'Expired' },
];

const EMPTY_MESSAGES: Record<FilterTab, string> = {
  all: 'No products found.',
  NeedsReorder: 'Nothing is low or out of stock.',
  ExpiringSoon: 'No stock is close to its expiry date.',
  Expired: 'No expired stock on hand.',
};

const STATUS_BADGES: Record<StockStatus, { label: string; variant: 'success' | 'warning' | 'destructive' | 'secondary' }> = {
  InStock: { label: 'In stock', variant: 'success' },
  LowStock: { label: 'Low stock', variant: 'warning' },
  OutOfStock: { label: 'Out of stock', variant: 'destructive' },
  NotStocked: { label: 'Not stocked', variant: 'secondary' },
};

const PAGE_SIZE = 20;

/**
 * Pharmacy's stock list: one row per product with all its batches rolled up, searched,
 * filtered and paged on the server — so it stays usable at a real catalog's size, and "low
 * stock" is judged on the product's total rather than batch by batch. A row expands to the
 * batches behind it.
 */
export function StockSummaryPanel() {
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<FilterTab>(ALL);
  const [page, setPage] = useState(1);
  const [expandedProductId, setExpandedProductId] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const { data, isPending, isError, error, isFetching } = useStockSummaryQuery({
    page,
    pageSize: PAGE_SIZE,
    search: debouncedSearch || undefined,
    filter: filter === ALL ? undefined : filter,
  });

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleFilterChange(value: string) {
    setFilter(value as FilterTab);
    setPage(1);
    setExpandedProductId(null);
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Tabs value={filter} onValueChange={handleFilterChange}>
          <TabsList>
            {FILTER_TABS.map((tab) => (
              <TabsTrigger key={tab.value} value={tab.value}>
                {tab.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        <div className="relative w-full sm:w-72">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
            placeholder="Search name, generic, SKU, or code…"
            aria-label="Search products"
            className="pl-9"
          />
          {isFetching && !isPending && <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />}
        </div>
      </div>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading stock…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load stock.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">{debouncedSearch ? `No products match "${debouncedSearch}".` : EMPTY_MESSAGES[filter]}</p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="w-8 px-2 py-2.5" />
                  <th className="px-4 py-2.5">Product</th>
                  <th className="px-4 py-2.5 text-right">Available</th>
                  <th className="px-4 py-2.5 text-right">Reorder level</th>
                  <th className="px-4 py-2.5">Nearest expiry</th>
                  <th className="px-4 py-2.5">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.items.map((summary) => {
                  const isExpanded = expandedProductId === summary.productId;
                  return (
                    <Fragment key={summary.productId}>
                      <StockSummaryRow
                        summary={summary}
                        isExpanded={isExpanded}
                        onToggle={() => setExpandedProductId(isExpanded ? null : summary.productId)}
                      />
                      {isExpanded && (
                        <tr>
                          <td colSpan={6} className="bg-muted/20 px-4 py-3">
                            <ProductBatches productId={summary.productId} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination meta={data.meta} onPageChange={setPage} />
        </>
      )}
    </div>
  );
}

interface StockSummaryRowProps {
  summary: StockSummaryResponse;
  isExpanded: boolean;
  onToggle: () => void;
}

function StockSummaryRow({ summary, isExpanded, onToggle }: StockSummaryRowProps) {
  const status = STATUS_BADGES[summary.status];
  const canExpand = summary.batchCount > 0;
  const rowClassName =
    summary.status === 'OutOfStock' || summary.expiredQuantity > 0
      ? 'bg-destructive/5 hover:bg-destructive/10'
      : summary.status === 'LowStock' || summary.expiringSoonQuantity > 0
        ? 'bg-warning/5 hover:bg-warning/10'
        : 'hover:bg-muted/30';

  return (
    <tr className={rowClassName}>
      <td className="px-2 py-3 text-center">
        {canExpand && (
          <button
            type="button"
            onClick={onToggle}
            aria-expanded={isExpanded}
            aria-label={`${isExpanded ? 'Hide' : 'Show'} batches of ${summary.productName}`}
            className="rounded p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
          </button>
        )}
      </td>
      <td className="px-4 py-3">
        <div className="flex flex-col">
          <span className="font-medium text-foreground">{summary.productName}</span>
          <span className="text-xs text-muted-foreground">
            {summary.genericName ? `${summary.genericName} · ` : ''}
            <span className="font-mono">{summary.sku}</span>
            {summary.batchCount > 0 && ` · ${summary.batchCount} ${summary.batchCount === 1 ? 'batch' : 'batches'}`}
          </span>
        </div>
      </td>
      <td className="px-4 py-3 text-right font-medium tabular-nums text-foreground">{summary.availableQuantity}</td>
      <td className="px-4 py-3 text-right tabular-nums text-muted-foreground">{summary.reorderLevel > 0 ? summary.reorderLevel : '—'}</td>
      <td className="px-4 py-3 text-sm text-foreground">
        {summary.nearestExpiry ? new Date(summary.nearestExpiry).toLocaleDateString('en-IN') : '—'}
      </td>
      <td className="px-4 py-3">
        <div className="flex flex-wrap items-center gap-1.5">
          <Badge variant={status.variant}>{status.label}</Badge>
          {summary.expiringSoonQuantity > 0 && <Badge variant="warning">{summary.expiringSoonQuantity} expiring soon</Badge>}
          {summary.expiredQuantity > 0 && <Badge variant="destructive">{summary.expiredQuantity} expired</Badge>}
          {summary.onHoldQuantity > 0 && <Badge variant="secondary">{summary.onHoldQuantity} on hold</Badge>}
        </div>
      </td>
    </tr>
  );
}

/** The expanded row's batch breakdown — fetched only when a product is opened. */
function ProductBatches({ productId }: { productId: string }) {
  const { data, isPending, isError } = useStockBalancesQuery({ productId, inStockOnly: true, pageSize: 100 });

  if (isPending) {
    return (
      <div className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading batches…
      </div>
    );
  }

  if (isError || !data) {
    return <p className="py-2 text-sm text-destructive">Failed to load batches.</p>;
  }

  const batches = [...data.items].sort((a, b) => a.expiryDate.localeCompare(b.expiryDate));
  return <StockBalanceTable balances={batches} />;
}
