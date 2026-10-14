import type { GstRateRow, PurchaseRegisterRow, PurchaseReturnRegisterRow } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { ArrowDownToLine, ArrowUpFromLine, FileText, IndianRupee, PackagePlus, Scale, Undo2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { dateLabel, rupees } from '@/features/pharmacy/common/format';
import { RETURN_REASON_LABELS } from '@/features/pharmacy/common/labels';
import { pharmacyApi } from '@/services/apiClient';
import { sum, toSection, type ReportColumn } from './columns';
import { periodLabel, type ReportPeriod } from './period';
import { ReportFrame } from './ReportFrame';
import { ReportTable } from './ReportTable';
import type { PeriodReportProps } from './SalesReports';
import { StatTile } from './StatTile';

function query(period: ReportPeriod, storeId: string) {
  return { ...period, storeId: storeId || undefined };
}

// ---- GST -------------------------------------------------------------------------------

function gstColumns(withIgst: boolean): ReportColumn<GstRateRow>[] {
  return [
    { header: 'HSN', value: (row) => row.hsnCode ?? '', mono: true, render: (row) => row.hsnCode ?? <span className="text-muted-foreground">Not set</span> },
    { header: 'Rate', value: (row) => row.gstRatePercent, kind: 'percent' },
    { header: 'Taxable value', value: (row) => row.taxableAmount, kind: 'money', total: true },
    { header: 'CGST', value: (row) => row.cgst, kind: 'money', total: true },
    { header: 'SGST', value: (row) => row.sgst, kind: 'money', total: true },
    ...(withIgst ? [{ header: 'IGST', value: (row: GstRateRow) => row.igst, kind: 'money' as const, total: true }] : []),
    { header: 'Total tax', value: (row) => row.taxAmount, kind: 'money', total: true },
  ];
}

function GstTable({ title, rows, withIgst }: { title: string; rows: GstRateRow[]; withIgst: boolean }) {
  return (
    <div className="flex flex-col gap-1.5">
      <h3 className="text-sm font-semibold text-foreground">{title}</h3>
      <ReportTable columns={gstColumns(withIgst)} rows={rows} rowKey={(row) => `${row.hsnCode ?? '-'}@${row.gstRatePercent}`} emptyText="None in this period." />
    </div>
  );
}

/**
 * GST for the period by HSN code and rate — the figures a GSTR-1 (outward supplies, less
 * credit notes for sale returns) and the input-tax side of GSTR-3B are filled from. Counter
 * sales are within the state, so their tax is CGST + SGST; purchases follow each invoice.
 */
export function GstReport({ period, storeId }: PeriodReportProps) {
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'gst', period, storeId],
    queryFn: () => pharmacyApi.getGstSummary(query(period, storeId)),
  });

  const showPurchases = data?.inputTax != null;
  const purchasesHaveIgst = [...(data?.purchases ?? []), ...(data?.purchaseReturns ?? [])].some((row) => row.igst !== 0);
  const sections = data
    ? [
        toSection('Sales', gstColumns(false), data.sales),
        toSection('Sale returns', gstColumns(false), data.saleReturns),
        ...(showPurchases ? [toSection('Purchases', gstColumns(true), data.purchases), toSection('Purchase returns', gstColumns(true), data.purchaseReturns)] : []),
      ]
    : [];
  const net = data && data.inputTax != null ? data.outputTax - data.inputTax : null;

  return (
    <ReportFrame
      title="GST summary"
      caption={`${periodLabel(period)} · by HSN code and rate`}
      sections={sections}
      filename={`pharmacy-gst-${period.fromDate}-to-${period.toDate}.xlsx`}
      isPending={isPending}
      error={error}
      tiles={
        data && (
          <>
            <StatTile label="Taxable sales" value={rupees(sum(data.sales, (row) => row.taxableAmount) - sum(data.saleReturns, (row) => row.taxableAmount))} hint="Net of returns" icon={ArrowUpFromLine} />
            <StatTile label="Output tax" value={rupees(data.outputTax)} hint="On sales, less returns" icon={IndianRupee} />
            {showPurchases && (
              <>
                <StatTile label="Taxable purchases" value={rupees(sum(data.purchases, (row) => row.taxableAmount) - sum(data.purchaseReturns, (row) => row.taxableAmount))} hint="Net of returns" icon={ArrowDownToLine} />
                <StatTile label="Input tax" value={rupees(data.inputTax)} hint="On purchases, less returns" icon={IndianRupee} />
                <StatTile label={net != null && net < 0 ? 'Input credit left' : 'Net payable'} value={rupees(Math.abs(net ?? 0))} hint="Pharmacy only" tone={net != null && net < 0 ? 'success' : 'warning'} icon={Scale} />
              </>
            )}
          </>
        )
      }
    >
      {data && (
        <div className="flex flex-col gap-4">
          <GstTable title="Sales (outward supplies)" rows={data.sales} withIgst={false} />
          {data.saleReturns.length > 0 && <GstTable title="Less: sale returns (credit notes)" rows={data.saleReturns} withIgst={false} />}
          {showPurchases ? (
            <>
              <GstTable title="Purchases (inward supplies)" rows={data.purchases} withIgst={purchasesHaveIgst} />
              {data.purchaseReturns.length > 0 && <GstTable title="Less: purchase returns (debit notes)" rows={data.purchaseReturns} withIgst={purchasesHaveIgst} />}
            </>
          ) : (
            <p className="text-xs text-muted-foreground">Purchase GST is shown to users who may see what the pharmacy pays.</p>
          )}
          <p className="text-xs text-muted-foreground print:hidden">These are the pharmacy's figures only — the hospital's other GST is in Accounts and Finance. Confirm the treatment of inpatient sales with your accountant before filing.</p>
        </div>
      )}
    </ReportFrame>
  );
}

// ---- Purchase register -----------------------------------------------------------------

const PURCHASE_COLUMNS: ReportColumn<PurchaseRegisterRow>[] = [
  { header: 'Received', value: (row) => dateLabel(row.receivedDate), nowrap: true },
  {
    header: 'GRN no.',
    value: (row) => row.receiptNo,
    mono: true,
    nowrap: true,
    render: (row) => (
      <Link to={`/pharmacy/purchases/${row.receiptId}`} className="font-medium text-primary hover:underline print:text-black print:no-underline">
        {row.receiptNo}
      </Link>
    ),
  },
  { header: 'Supplier', value: (row) => row.supplierName },
  { header: 'Invoice no.', value: (row) => row.supplierInvoiceNo, nowrap: true },
  { header: 'Invoice date', value: (row) => (row.supplierInvoiceDate ? dateLabel(row.supplierInvoiceDate) : ''), nowrap: true },
  { header: 'Tax', value: (row) => (row.isInterState ? 'IGST' : 'CGST+SGST'), render: (row) => <Badge variant={row.isInterState ? 'warning' : 'secondary'}>{row.isInterState ? 'IGST' : 'Local'}</Badge> },
  { header: 'Gross', value: (row) => row.grossAmount, kind: 'money', total: true },
  { header: 'Discount', value: (row) => row.discountAmount, kind: 'money', total: true },
  { header: 'Taxable', value: (row) => row.taxableAmount, kind: 'money', total: true },
  { header: 'GST', value: (row) => row.taxAmount, kind: 'money', total: true },
  { header: 'Other', value: (row) => row.otherCharges, kind: 'money', total: true },
  { header: 'Round off', value: (row) => row.roundOff, kind: 'money', total: true },
  { header: 'Net', value: (row) => row.netAmount, kind: 'money', total: true },
];

const PURCHASE_RETURN_COLUMNS: ReportColumn<PurchaseReturnRegisterRow>[] = [
  { header: 'Date', value: (row) => dateLabel(row.returnDate), nowrap: true },
  { header: 'Return no.', value: (row) => row.returnNo, mono: true, nowrap: true },
  { header: 'Supplier', value: (row) => row.supplierName },
  { header: 'Reason', value: (row) => RETURN_REASON_LABELS[row.reason] },
  { header: 'Credit note', value: (row) => row.creditNoteNo ?? '' },
  { header: 'Taxable', value: (row) => row.taxableAmount, kind: 'money', total: true },
  { header: 'GST', value: (row) => row.taxAmount, kind: 'money', total: true },
  { header: 'Net', value: (row) => row.netAmount, kind: 'money', total: true },
];

/** Every supplier invoice received and every purchase return in the period. */
export function PurchaseRegisterReport({ period, storeId }: PeriodReportProps) {
  const { data, isPending, error } = useQuery({
    queryKey: ['pharmacy', 'reports', 'purchase-register', period, storeId],
    queryFn: () => pharmacyApi.getPurchaseRegister(query(period, storeId)),
  });

  const purchases = data?.purchases ?? [];
  const returns = data?.returns ?? [];
  const bought = sum(purchases, (row) => row.netAmount);
  const sentBack = sum(returns, (row) => row.netAmount);

  return (
    <ReportFrame
      title="Purchase register"
      caption={`${periodLabel(period)} · ${purchases.length} invoices · ${returns.length} returns`}
      sections={[toSection('Purchases', PURCHASE_COLUMNS, purchases), toSection('Purchase returns', PURCHASE_RETURN_COLUMNS, returns)]}
      filename={`pharmacy-purchase-register-${period.fromDate}-to-${period.toDate}.xlsx`}
      isPending={isPending}
      error={error}
      tiles={
        <>
          <StatTile label="Invoices" value={purchases.length} icon={FileText} />
          <StatTile label="Purchases" value={rupees(bought)} icon={PackagePlus} />
          <StatTile label="Returns" value={rupees(sentBack)} tone="warning" icon={Undo2} />
          <StatTile label="Net purchases" value={rupees(bought - sentBack)} tone="success" icon={IndianRupee} />
        </>
      }
    >
      <ReportTable columns={PURCHASE_COLUMNS} rows={purchases} rowKey={(row) => row.receiptId} emptyText="No purchases were received in this period." />
      {returns.length > 0 && (
        <>
          <h3 className="pt-2 text-sm font-semibold text-foreground">Purchase returns</h3>
          <ReportTable columns={PURCHASE_RETURN_COLUMNS} rows={returns} rowKey={(row) => row.returnId} emptyText="" />
        </>
      )}
    </ReportFrame>
  );
}
