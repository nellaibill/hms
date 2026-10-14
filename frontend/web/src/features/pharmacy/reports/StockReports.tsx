import type { NonMovingStockRow, StockValuationRow } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, Boxes, Hourglass, IndianRupee, Tag } from 'lucide-react';
import { useMemo, useState } from 'react';
import { dateLabel, expiryLabel, rupees, todayIso } from '@/features/pharmacy/common/format';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';
import { sum, toSection, type ReportColumn } from './columns';
import { ReportFrame } from './ReportFrame';
import { ReportTable } from './ReportTable';
import { StatTile } from './StatTile';

export interface StockReportProps {
  storeId: string;
}

// ---- Stock valuation -------------------------------------------------------------------

/** What the stock on hand is worth today, at cost and at MRP, product by product. */
export function StockValuationReport({ storeId }: StockReportProps) {
  const [search, setSearch] = useState('');
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'valuation', storeId],
    queryFn: () => pharmacyApi.getStockValuation(storeId || undefined),
  });

  const withCost = data?.totalCostValue != null;
  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data?.rows ?? []).filter((row) => !term || row.productName.toLowerCase().includes(term) || (row.genericName ?? '').toLowerCase().includes(term));
  }, [data, search]);

  const columns: ReportColumn<StockValuationRow>[] = [
    { header: 'Product', value: (row) => row.productName, render: (row) => (
      <>
        <span className="font-medium text-foreground">{row.productName}</span>
        {row.genericName && <span className="block text-xs text-muted-foreground">{row.genericName}</span>}
      </>
    ) },
    { header: 'Rack', value: (row) => row.rackLocation ?? '' },
    { header: 'Batches', value: (row) => row.batches, kind: 'count', total: true },
    { header: 'Quantity', value: (row) => row.quantity, kind: 'quantity', total: true },
    ...(withCost ? [{ header: 'At cost', value: (row: StockValuationRow) => row.costValue, kind: 'money' as const, total: true }] : []),
    { header: 'At MRP', value: (row) => row.mrpValue, kind: 'money', total: true },
    { header: 'Expired qty', value: (row) => row.expiredQuantity, kind: 'quantity', total: true },
    ...(withCost ? [{ header: 'Expired at cost', value: (row: StockValuationRow) => row.expiredCostValue, kind: 'money' as const, total: true }] : []),
  ];

  return (
    <ReportFrame
      title="Stock valuation"
      caption={`As on ${dateLabel(todayIso())}`}
      sections={[toSection('Stock valuation', columns, rows)]}
      filename={`pharmacy-stock-valuation-${todayIso()}.xlsx`}
      isPending={isPending}
      error={error}
      controls={<input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Filter products…" aria-label="Filter products" className="h-8 w-44 rounded-md border border-input bg-background px-2 text-sm" />}
      tiles={
        data && (
          <>
            <StatTile compact label="Products in stock" value={data.rows.length} icon={Boxes} />
            {withCost && <StatTile compact label="Stock at cost" value={rupees(data.totalCostValue)} tone="success" icon={IndianRupee} />}
            <StatTile compact label="Stock at MRP" value={rupees(data.totalMrpValue)} icon={Tag} />
            {withCost && <StatTile compact label="Expired, at cost" value={rupees(data.expiredCostValue)} tone={(data.expiredCostValue ?? 0) > 0 ? 'destructive' : 'default'} to="/pharmacy/expiry" icon={AlertTriangle} />}
          </>
        )
      }
    >
      <ReportTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.productId}
        emptyText={search ? 'No product matches the filter.' : 'There is no stock on hand.'}
        rowClassName={(row) => (row.expiredQuantity > 0 ? 'bg-destructive/5' : undefined)}
      />
    </ReportFrame>
  );
}

// ---- Non-moving stock ------------------------------------------------------------------

const IDLE_WINDOWS = [30, 60, 90, 180, 365];

/** Stock that has sat on the shelf without a sale — money tied up, and the first candidates
 * to return to the supplier before they expire. */
export function NonMovingStockReport({ storeId }: StockReportProps) {
  const [days, setDays] = useState(90);
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'non-moving', storeId, days],
    queryFn: () => pharmacyApi.getNonMovingStock(storeId || undefined, days),
  });

  const rows = data ?? [];
  const withCost = rows.some((row) => row.costValue != null);

  const columns: ReportColumn<NonMovingStockRow>[] = [
    { header: 'Product', value: (row) => row.productName },
    { header: 'Rack', value: (row) => row.rackLocation ?? '' },
    { header: 'Quantity', value: (row) => row.quantity, kind: 'quantity', total: true },
    ...(withCost ? [{ header: 'At cost', value: (row: NonMovingStockRow) => row.costValue, kind: 'money' as const, total: true }] : []),
    { header: 'Last sold', value: (row) => (row.lastSoldOn ? dateLabel(row.lastSoldOn) : 'Never'), nowrap: true },
    { header: 'Stocked since', value: (row) => (row.stockedOn ? dateLabel(row.stockedOn) : ''), nowrap: true },
    { header: 'Idle days', value: (row) => row.idleDays, kind: 'count' },
    { header: 'Nearest expiry', value: (row) => (row.nearestExpiry ? expiryLabel(row.nearestExpiry) : 'Expired'), nowrap: true, render: (row) => <span className={cn(!row.nearestExpiry && 'text-destructive')}>{row.nearestExpiry ? expiryLabel(row.nearestExpiry) : 'Expired'}</span> },
  ];

  return (
    <ReportFrame
      title="Non-moving stock"
      caption={`Not sold for ${days} days or more · as on ${dateLabel(todayIso())}`}
      sections={[toSection('Non-moving stock', columns, rows)]}
      filename={`pharmacy-non-moving-${days}-days-${todayIso()}.xlsx`}
      isPending={isPending}
      error={error}
      controls={
        <div className="flex overflow-hidden rounded-md border border-border text-xs font-medium">
          {IDLE_WINDOWS.map((window) => (
            <button key={window} type="button" onClick={() => setDays(window)} className={cn('px-2.5 py-1.5', days === window ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
              {window} days
            </button>
          ))}
        </div>
      }
      tiles={
        <>
          <StatTile compact label="Products" value={rows.length} icon={Hourglass} />
          <StatTile compact label="Never sold" value={rows.filter((row) => !row.lastSoldOn).length} tone="warning" icon={AlertTriangle} />
          {withCost && <StatTile compact label="Tied up, at cost" value={rupees(sum(rows, (row) => row.costValue))} tone="warning" icon={IndianRupee} />}
        </>
      }
    >
      <ReportTable columns={columns} rows={rows} rowKey={(row) => row.productId} emptyText={`Nothing on the shelf has gone ${days} days or more without a sale.`} />
    </ReportFrame>
  );
}
