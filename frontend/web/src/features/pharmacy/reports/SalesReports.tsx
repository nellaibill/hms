import { SALE_PAYMENT_METHODS, type CollectionRow, type DailySalesRow, type ProductSalesRow, type SalePaymentMethod, type SalesRegisterRow, type SalesReturnRegisterRow } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Banknote, IndianRupee, Percent, ReceiptText, ShoppingBag, Undo2, Wallet } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Badge } from '@/components/ui/badge';
import { dateLabel, dateTimeLabel, money, rupees } from '@/features/pharmacy/common/format';
import { PAYMENT_METHOD_LABELS } from '@/features/pharmacy/common/labels';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';
import { sum, toSection, type ReportColumn } from './columns';
import { periodLabel, type ReportPeriod } from './period';
import { ReportFrame } from './ReportFrame';
import { ReportTable } from './ReportTable';
import { StatTile } from './StatTile';

export interface PeriodReportProps {
  period: ReportPeriod;
  storeId: string;
}

const compactRupees = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 });
const tooltipStyle = { background: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: 8, fontSize: 12, color: 'hsl(var(--popover-foreground))' };

function query(period: ReportPeriod, storeId: string) {
  return { ...period, storeId: storeId || undefined };
}

function fileName(name: string, period: ReportPeriod) {
  return `pharmacy-${name}-${period.fromDate}-to-${period.toDate}.xlsx`;
}

// ---- Day-wise sales --------------------------------------------------------------------

/** Each day's bills, returns, collections and margin — the day book of the counter. */
export function DailySalesReport({ period, storeId }: PeriodReportProps) {
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'daily', period, storeId],
    queryFn: () => pharmacyApi.getDailySalesReport(query(period, storeId)),
  });

  const rows = useMemo(() => (data ?? []).filter((day) => day.bills > 0 || day.returns > 0 || day.collected !== 0), [data]);
  const withCost = rows.some((day) => day.margin != null);

  const columns: ReportColumn<DailySalesRow>[] = [
    { header: 'Date', value: (day) => dateLabel(day.date), nowrap: true },
    { header: 'Bills', value: (day) => day.bills, kind: 'count', total: true },
    { header: 'Gross', value: (day) => day.grossAmount, kind: 'money', total: true },
    { header: 'Discount', value: (day) => day.discountAmount, kind: 'money', total: true },
    { header: 'Taxable', value: (day) => day.taxableAmount, kind: 'money', total: true },
    { header: 'GST', value: (day) => day.taxAmount, kind: 'money', total: true },
    { header: 'Sales', value: (day) => day.salesAmount, kind: 'money', total: true },
    { header: 'Returns', value: (day) => day.returnAmount, kind: 'money', total: true },
    { header: 'Net sales', value: (day) => day.netSales, kind: 'money', total: true },
    { header: 'Collected', value: (day) => day.collected, kind: 'money', total: true },
    ...(withCost
      ? ([
          { header: 'Cost', value: (day) => day.cost, kind: 'money', total: true },
          { header: 'Margin', value: (day) => day.margin, kind: 'money', total: true },
        ] satisfies ReportColumn<DailySalesRow>[])
      : []),
  ];

  const netSales = sum(rows, (day) => day.netSales);
  const margin = sum(rows, (day) => day.margin);
  const chart = (data ?? []).map((day) => ({ day: dateLabel(day.date).slice(0, 6), net: day.netSales }));

  return (
    <ReportFrame
      title="Day-wise sales"
      caption={periodLabel(period)}
      sections={[toSection('Day-wise sales', columns, rows)]}
      filename={fileName('day-wise-sales', period)}
      isPending={isPending}
      error={error}
      tiles={
        <>
          <StatTile compact label="Bills" value={sum(rows, (day) => day.bills)} icon={ReceiptText} />
          <StatTile compact label="Sales" value={rupees(sum(rows, (day) => day.salesAmount))} icon={ShoppingBag} />
          <StatTile compact label="Returns" value={rupees(sum(rows, (day) => day.returnAmount))} tone="warning" icon={Undo2} />
          <StatTile compact label="Net sales" value={rupees(netSales)} tone="success" icon={IndianRupee} />
          <StatTile compact label="Collected" value={rupees(sum(rows, (day) => day.collected))} hint={netSales !== sum(rows, (day) => day.collected) ? 'Credit and IP bills are collected later' : undefined} icon={Wallet} />
          {withCost && <StatTile compact label="Margin" value={rupees(margin)} hint="Net of returns" tone="success" icon={Percent} />}
        </>
      }
    >
      {chart.length > 1 && rows.length > 0 && (
        <div className="h-48 rounded-lg border border-border bg-background p-2 print:hidden">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chart} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
              <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
              <XAxis dataKey="day" tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 11 }} interval="preserveStartEnd" />
              <YAxis tickLine={false} axisLine={false} tick={{ fill: 'hsl(var(--muted-foreground))', fontSize: 11 }} tickFormatter={(value: number) => compactRupees.format(value)} width={44} />
              <Tooltip cursor={{ fill: 'hsl(var(--accent))' }} contentStyle={tooltipStyle} formatter={(value) => rupees(Number(value ?? 0))} />
              <Bar dataKey="net" name="Net sales" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]} maxBarSize={28} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
      <ReportTable columns={columns} rows={rows} rowKey={(day) => day.date} emptyText="No bills, returns or payments in this period." />
    </ReportFrame>
  );
}

// ---- Sales register --------------------------------------------------------------------

const KIND_VARIANT: Record<string, 'secondary' | 'warning' | 'success'> = { 'Walk-in': 'secondary', OP: 'success', IP: 'warning' };

const BILL_COLUMNS: ReportColumn<SalesRegisterRow>[] = [
  { header: 'Date', value: (bill) => dateTimeLabel(bill.saleDate), nowrap: true },
  {
    header: 'Bill no.',
    value: (bill) => bill.billNo,
    mono: true,
    nowrap: true,
    render: (bill) => (
      <Link to={`/pharmacy/sales/${bill.saleId}`} className="font-medium text-primary hover:underline print:text-black print:no-underline">
        {bill.billNo}
      </Link>
    ),
  },
  { header: 'Customer', value: (bill) => [bill.customerName, bill.patientUhid].filter(Boolean).join(' · ') },
  { header: 'Type', value: (bill) => bill.kind, render: (bill) => <Badge variant={KIND_VARIANT[bill.kind] ?? 'secondary'}>{bill.kind}</Badge> },
  { header: 'Doctor', value: (bill) => bill.doctorName ?? '' },
  { header: 'Gross', value: (bill) => bill.grossAmount, kind: 'money', total: true },
  { header: 'Discount', value: (bill) => bill.discountAmount, kind: 'money', total: true },
  { header: 'Taxable', value: (bill) => bill.taxableAmount, kind: 'money', total: true },
  { header: 'GST', value: (bill) => bill.taxAmount, kind: 'money', total: true },
  { header: 'Net', value: (bill) => bill.netAmount, kind: 'money', total: true },
  { header: 'Paid', value: (bill) => bill.paidAmount, kind: 'money', total: true },
  { header: 'Returned', value: (bill) => bill.returnedAmount, kind: 'money', total: true },
  { header: 'Due', value: (bill) => bill.balanceDue, kind: 'money', total: true, render: (bill) => <span className={cn(bill.balanceDue > 0 && 'font-medium text-warning')}>{money(bill.balanceDue)}{bill.chargedToAdmission && bill.balanceDue > 0 ? ' (IP)' : ''}</span> },
];

const RETURN_COLUMNS: ReportColumn<SalesReturnRegisterRow>[] = [
  { header: 'Date', value: (row) => dateTimeLabel(row.returnDate), nowrap: true },
  { header: 'Return no.', value: (row) => row.returnNo, mono: true, nowrap: true },
  {
    header: 'Against bill',
    value: (row) => row.billNo,
    mono: true,
    nowrap: true,
    render: (row) => (
      <Link to={`/pharmacy/sales/${row.saleId}`} className="text-primary hover:underline print:text-black print:no-underline">
        {row.billNo}
      </Link>
    ),
  },
  { header: 'Customer', value: (row) => row.customerName },
  { header: 'Taxable', value: (row) => row.taxableAmount, kind: 'money', total: true },
  { header: 'GST', value: (row) => row.taxAmount, kind: 'money', total: true },
  { header: 'Return value', value: (row) => row.returnAmount, kind: 'money', total: true },
  { header: 'Refunded', value: (row) => row.refundAmount, kind: 'money', total: true },
  { header: 'Mode', value: (row) => (row.refundAmount > 0 ? PAYMENT_METHOD_LABELS[row.refundMethod] : '—') },
];

/** Every bill and every sale return in the period, one line each — up to three months at a time. */
export function SalesRegisterReport({ period, storeId }: PeriodReportProps) {
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'sales-register', period, storeId],
    queryFn: () => pharmacyApi.getSalesRegister(query(period, storeId)),
  });

  const bills = data?.bills ?? [];
  const returns = data?.returns ?? [];

  return (
    <ReportFrame
      title="Sales register"
      caption={`${periodLabel(period)} · ${bills.length} bills · ${returns.length} returns`}
      sections={[toSection('Bills', BILL_COLUMNS, bills), toSection('Sale returns', RETURN_COLUMNS, returns)]}
      filename={fileName('sales-register', period)}
      isPending={isPending}
      error={error}
      tiles={
        <>
          <StatTile compact label="Bills" value={bills.length} icon={ReceiptText} />
          <StatTile compact label="Billed" value={rupees(sum(bills, (bill) => bill.netAmount))} icon={ShoppingBag} />
          <StatTile compact label="Returns" value={rupees(sum(returns, (row) => row.returnAmount))} tone="warning" icon={Undo2} />
          <StatTile compact label="Still due" value={rupees(sum(bills, (bill) => bill.balanceDue))} hint="Includes inpatient credit" tone="warning" icon={Wallet} />
        </>
      }
    >
      <ReportTable columns={BILL_COLUMNS} rows={bills} rowKey={(bill) => bill.saleId} emptyText="No bills in this period." />
      {returns.length > 0 && (
        <>
          <h3 className="pt-2 text-sm font-semibold text-foreground">Sale returns</h3>
          <ReportTable columns={RETURN_COLUMNS} rows={returns} rowKey={(row) => row.returnId} emptyText="" />
        </>
      )}
    </ReportFrame>
  );
}

// ---- Item-wise sales -------------------------------------------------------------------

type ProductOrder = 'value' | 'quantity' | 'margin';

/** What each product sold, net of returns: the fast movers, and where the margin comes from. */
export function ProductSalesReport({ period, storeId }: PeriodReportProps) {
  const [order, setOrder] = useState<ProductOrder>('value');
  const [search, setSearch] = useState('');
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'products', period, storeId],
    queryFn: () => pharmacyApi.getProductSalesReport(query(period, storeId)),
  });

  const withCost = (data ?? []).some((row) => row.margin != null);
  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    const filtered = (data ?? []).filter((row) => !term || row.productName.toLowerCase().includes(term));
    const key: Record<ProductOrder, (row: ProductSalesRow) => number> = { value: (row) => row.amount, quantity: (row) => row.netQuantity, margin: (row) => row.margin ?? 0 };
    return [...filtered].sort((a, b) => key[order](b) - key[order](a));
  }, [data, order, search]);

  const columns: ReportColumn<ProductSalesRow>[] = [
    { header: '#', value: (row) => rows.indexOf(row) + 1, kind: 'count' },
    { header: 'Product', value: (row) => row.productName },
    { header: 'Bills', value: (row) => row.bills, kind: 'count' },
    { header: 'Sold', value: (row) => row.quantitySold, kind: 'quantity', total: true },
    { header: 'Returned', value: (row) => row.quantityReturned, kind: 'quantity', total: true },
    { header: 'Net qty', value: (row) => row.netQuantity, kind: 'quantity', total: true },
    { header: 'Taxable', value: (row) => row.taxableAmount, kind: 'money', total: true },
    { header: 'GST', value: (row) => row.taxAmount, kind: 'money', total: true },
    { header: 'Amount', value: (row) => row.amount, kind: 'money', total: true },
    ...(withCost
      ? ([
          { header: 'Cost', value: (row) => row.cost, kind: 'money', total: true },
          { header: 'Margin', value: (row) => row.margin, kind: 'money', total: true },
          { header: 'Margin %', value: (row) => row.marginPercent, kind: 'percent' },
        ] satisfies ReportColumn<ProductSalesRow>[])
      : []),
  ];

  const taxable = sum(rows, (row) => row.taxableAmount);
  const margin = sum(rows, (row) => row.margin);

  return (
    <ReportFrame
      title="Item-wise sales"
      caption={periodLabel(period)}
      sections={[toSection('Item-wise sales', columns, rows)]}
      filename={fileName('item-wise-sales', period)}
      isPending={isPending}
      error={error}
      controls={
        <>
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Filter products…" aria-label="Filter products" className="h-8 w-44 rounded-md border border-input bg-background px-2 text-sm" />
          <div className="flex overflow-hidden rounded-md border border-border text-xs font-medium">
            {(['value', 'quantity', ...(withCost ? ['margin'] : [])] as ProductOrder[]).map((option) => (
              <button key={option} type="button" onClick={() => setOrder(option)} className={cn('px-2.5 py-1.5 capitalize', order === option ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
                By {option}
              </button>
            ))}
          </div>
        </>
      }
      tiles={
        <>
          <StatTile compact label="Products sold" value={rows.filter((row) => row.netQuantity > 0).length} icon={ShoppingBag} />
          <StatTile compact label="Units, net" value={sum(rows, (row) => row.netQuantity).toLocaleString('en-IN')} icon={ReceiptText} />
          <StatTile compact label="Sales value" value={rupees(sum(rows, (row) => row.amount))} tone="success" icon={IndianRupee} />
          {withCost && <StatTile compact label="Margin" value={rupees(margin)} hint={taxable > 0 ? `${((margin / taxable) * 100).toFixed(1)}% of taxable value` : undefined} tone="success" icon={Percent} />}
        </>
      }
    >
      <ReportTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.productId}
        emptyText={search ? 'No product matches the filter.' : 'Nothing was sold in this period.'}
        rowClassName={(row) => (row.margin != null && row.margin < 0 ? 'bg-destructive/5' : undefined)}
      />
    </ReportFrame>
  );
}

// ---- Collections -----------------------------------------------------------------------

interface CollectionDay {
  date: string;
  byMethod: Record<SalePaymentMethod, number>;
  refunded: number;
  net: number;
}

/** Money taken at the counter per day and payment mode, with refunds taken off — what the
 * cash drawer and the card and UPI settlements should add up to. */
export function CollectionsReport({ period, storeId }: PeriodReportProps) {
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'collections', period, storeId],
    queryFn: () => pharmacyApi.getCollectionsReport(query(period, storeId)),
  });

  const days = useMemo(() => {
    const byDate = new Map<string, CollectionDay>();
    (data ?? []).forEach((row: CollectionRow) => {
      const day = byDate.get(row.date) ?? { date: row.date, byMethod: { Cash: 0, Card: 0, Upi: 0, BankTransfer: 0 }, refunded: 0, net: 0 };
      day.byMethod[row.method] += row.received;
      day.refunded += row.refunded;
      day.net += row.net;
      byDate.set(row.date, day);
    });
    return [...byDate.values()].sort((a, b) => a.date.localeCompare(b.date));
  }, [data]);

  const usedMethods = SALE_PAYMENT_METHODS.filter((method) => days.some((day) => day.byMethod[method] !== 0));
  const columns: ReportColumn<CollectionDay>[] = [
    { header: 'Date', value: (day) => dateLabel(day.date), nowrap: true },
    ...usedMethods.map((method): ReportColumn<CollectionDay> => ({ header: PAYMENT_METHOD_LABELS[method], value: (day) => day.byMethod[method], kind: 'money', total: true })),
    { header: 'Refunded', value: (day) => day.refunded, kind: 'money', total: true },
    { header: 'Net', value: (day) => day.net, kind: 'money', total: true },
  ];

  const byMethod = (method: SalePaymentMethod, pick: (row: CollectionRow) => number) => sum(data ?? [], (row) => (row.method === method ? pick(row) : 0));
  const tileMethods = SALE_PAYMENT_METHODS.filter((method) => method === 'Cash' || (data ?? []).some((row) => row.method === method));

  return (
    <ReportFrame
      title="Collections"
      caption={periodLabel(period)}
      sections={[toSection('Collections', columns, days)]}
      filename={fileName('collections', period)}
      isPending={isPending}
      error={error}
      tiles={
        <>
          {tileMethods.map((method) => {
            const refunded = byMethod(method, (row) => row.refunded);
            return (
              <StatTile
                compact
                key={method}
                label={PAYMENT_METHOD_LABELS[method]}
                value={rupees(byMethod(method, (row) => row.net))}
                hint={refunded > 0 ? `${rupees(byMethod(method, (row) => row.received))} taken, ${rupees(refunded)} refunded` : 'Net of refunds'}
                icon={method === 'Cash' ? Banknote : Wallet}
              />
            );
          })}
          <StatTile compact label="Refunded" value={rupees(sum(days, (day) => day.refunded))} tone="warning" icon={Undo2} />
          <StatTile compact label="Net collected" value={rupees(sum(days, (day) => day.net))} tone="success" icon={IndianRupee} />
        </>
      }
    >
      <ReportTable columns={columns} rows={days} rowKey={(day) => day.date} emptyText="No money was taken or refunded in this period." />
    </ReportFrame>
  );
}
