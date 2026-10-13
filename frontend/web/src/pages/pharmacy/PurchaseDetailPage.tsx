import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Loader2, Plus, Printer } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { dateLabel, dateTimeLabel, expiryLabel, money, quantity } from '@/features/pharmacy/common/format';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { pharmacyApi } from '@/services/apiClient';

/** One purchase invoice as booked — read-only, and laid out to print as the goods receipt note. */
export default function PurchaseDetailPage() {
  const { id = '' } = useParams();
  const { data: receipt, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'goods-receipts', 'byId', id],
    queryFn: () => pharmacyApi.getGoodsReceiptById(id),
    enabled: Boolean(id),
  });

  return (
    <PharmacyPage
      title={receipt ? `Purchase ${receipt.receiptNo}` : 'Purchase'}
      subtitle={receipt ? `${receipt.supplierName} · Invoice ${receipt.supplierInvoiceNo}` : undefined}
      subLinks={PURCHASE_LINKS}
      actions={
        <>
          <Button size="sm" variant="outline" asChild>
            <Link to="/pharmacy/purchases">
              <ArrowLeft className="h-4 w-4" />
              All purchases
            </Link>
          </Button>
          <Button size="sm" variant="outline" onClick={() => window.print()} disabled={!receipt}>
            <Printer className="h-4 w-4" />
            Print
          </Button>
          <Button size="sm" asChild>
            <Link to="/pharmacy/purchases/new">
              <Plus className="h-4 w-4" />
              New purchase
            </Link>
          </Button>
        </>
      }
    >
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          This purchase could not be loaded.
        </p>
      )}

      {receipt && (
        <article className="rounded-lg border border-border bg-background p-5 print:border-0 print:p-0">
          <header className="flex flex-wrap items-start justify-between gap-4 border-b border-border pb-4">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Goods receipt note</p>
              <p className="text-xl font-semibold text-foreground">{receipt.receiptNo}</p>
              <p className="text-sm text-muted-foreground">
                Received {dateLabel(receipt.receivedDate)} into {receipt.storeName}
              </p>
            </div>
            <dl className="grid grid-cols-[auto_auto] gap-x-4 gap-y-0.5 text-sm">
              <dt className="text-muted-foreground">Supplier</dt>
              <dd className="font-medium text-foreground">{receipt.supplierName}</dd>
              <dt className="text-muted-foreground">Invoice no.</dt>
              <dd>{receipt.supplierInvoiceNo}</dd>
              <dt className="text-muted-foreground">Invoice date</dt>
              <dd>{dateLabel(receipt.supplierInvoiceDate)}</dd>
              <dt className="text-muted-foreground">Tax</dt>
              <dd>{receipt.isInterState ? 'IGST (other state)' : 'CGST + SGST'}</dd>
            </dl>
          </header>

          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-border text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="py-2 pr-3">#</th>
                  <th className="py-2 pr-3">Product</th>
                  <th className="py-2 pr-3">Batch</th>
                  <th className="py-2 pr-3">Expiry</th>
                  <th className="py-2 pr-3 text-right">Qty</th>
                  <th className="py-2 pr-3 text-right">Free</th>
                  <th className="py-2 pr-3 text-right">Rate</th>
                  <th className="py-2 pr-3 text-right">MRP</th>
                  <th className="py-2 pr-3 text-right">Disc %</th>
                  <th className="py-2 pr-3 text-right">Taxable</th>
                  <th className="py-2 pr-3 text-right">GST</th>
                  <th className="py-2 text-right">Amount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {receipt.lines.map((line) => (
                  <tr key={line.id}>
                    <td className="py-2 pr-3 text-muted-foreground">{line.lineNo}</td>
                    <td className="py-2 pr-3">
                      <div className="font-medium text-foreground">{line.productName}</div>
                      <div className="text-xs text-muted-foreground">
                        {quantity(line.receivedUnits)} units into stock
                        {line.hsnCode ? ` · HSN ${line.hsnCode}` : ''}
                        {line.returnedUnits > 0 ? ` · ${quantity(line.returnedUnits)} returned` : ''}
                      </div>
                    </td>
                    <td className="py-2 pr-3 font-mono text-xs">{line.batchNo}</td>
                    <td className="py-2 pr-3">{expiryLabel(line.expiryDate)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{quantity(line.quantity)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{line.freeQuantity ? quantity(line.freeQuantity) : '—'}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{money(line.purchaseRate)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{money(line.mrp)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{line.discountPercent ? quantity(line.discountPercent) : '—'}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{money(line.taxableAmount)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">
                      {money(line.taxAmount)} <span className="text-xs text-muted-foreground">({quantity(line.gstRatePercent)}%)</span>
                    </td>
                    <td className="py-2 text-right font-medium tabular-nums">{money(line.lineTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="mt-4 flex flex-wrap items-end justify-between gap-4 border-t border-border pt-4">
            <div className="text-xs text-muted-foreground">
              {receipt.remarks && <p className="text-sm text-foreground">{receipt.remarks}</p>}
              <p>Entered {dateTimeLabel(receipt.createdAt)}</p>
            </div>
            <dl className="w-64 text-sm">
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Gross</dt>
                <dd className="tabular-nums">{money(receipt.grossAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Discount</dt>
                <dd className="tabular-nums">− {money(receipt.discountAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Taxable</dt>
                <dd className="tabular-nums">{money(receipt.taxableAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">{receipt.isInterState ? 'IGST' : 'CGST + SGST'}</dt>
                <dd className="tabular-nums">{money(receipt.taxAmount)}</dd>
              </div>
              {receipt.otherCharges > 0 && (
                <div className="flex justify-between py-0.5">
                  <dt className="text-muted-foreground">Other charges</dt>
                  <dd className="tabular-nums">{money(receipt.otherCharges)}</dd>
                </div>
              )}
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Round off</dt>
                <dd className="tabular-nums">{money(receipt.roundOff)}</dd>
              </div>
              <div className="mt-1 flex justify-between border-t border-border pt-1.5 text-base font-semibold">
                <dt>Total</dt>
                <dd className="tabular-nums">₹{money(receipt.netAmount)}</dd>
              </div>
            </dl>
          </footer>
        </article>
      )}
    </PharmacyPage>
  );
}
