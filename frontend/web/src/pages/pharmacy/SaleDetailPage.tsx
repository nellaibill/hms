import { ApiError, SALE_PAYMENT_METHODS, type SalePaymentMethod } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Loader2, Printer, Undo2, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { dateTimeLabel, expiryLabel, money, quantity, toNumber } from '@/features/pharmacy/common/format';
import { SCHEDULE_BADGE } from '@/features/pharmacy/common/labels';
import { BillPrint } from '@/features/pharmacy/counter/BillPrint';
import { SALES_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const METHOD_LABELS: Record<SalePaymentMethod, string> = { Cash: 'Cash', Card: 'Card', Upi: 'UPI', BankTransfer: 'Bank transfer' };
const input = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * One bill, and everything that can still happen to it: a reprint, the balance being paid,
 * goods coming back. A return is entered against the bill's own lines, so it can never be
 * for more than was sold and is always credited at the price actually paid.
 */
export default function SaleDetailPage() {
  const { id = '' } = useParams();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canAct = hasPermission('pharmacy.create');

  const [mode, setMode] = useState<'view' | 'return' | 'collect'>('view');
  const [returnQty, setReturnQty] = useState<Record<string, string>>({});
  const [restock, setRestock] = useState(true);
  const [reason, setReason] = useState('');
  const [method, setMethod] = useState<SalePaymentMethod>('Cash');
  const [amount, setAmount] = useState('');
  const [reference, setReference] = useState('');
  const [error, setError] = useState<string | null>(null);

  const { data: sale, isPending, isError } = useQuery({ queryKey: ['pharmacy', 'sales', 'byId', id], queryFn: () => pharmacyApi.getSaleById(id), enabled: Boolean(id) });
  const { data: returns } = useQuery({ queryKey: ['pharmacy', 'sale-returns', 'forSale', id], queryFn: () => pharmacyApi.getSaleReturns({ saleId: id, pageSize: 50 }), enabled: Boolean(id) });
  const { data: settings } = useQuery({ queryKey: ['pharmacy', 'settings'], queryFn: () => pharmacyApi.getSettings(), staleTime: 300_000 });

  function done(message: string, description: string) {
    void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
    toast({ title: message, description, variant: 'success' });
    setMode('view');
    setReturnQty({});
    setReason('');
    setAmount('');
    setReference('');
    setError(null);
  }

  const onError = (failure: unknown) => setError(failure instanceof ApiError ? failure.message : 'Could not save.');

  const returnMutation = useMutation({
    mutationFn: () =>
      pharmacyApi.createSaleReturn(id, {
        lines: Object.entries(returnQty)
          .map(([saleLineId, text]) => ({ saleLineId, quantity: toNumber(text) }))
          .filter((line) => line.quantity > 0),
        refundMethod: method,
        restock,
        reason: reason.trim() || undefined,
      }),
    onSuccess: (saleReturn) =>
      done(
        `Return ${saleReturn.returnNo} saved`,
        saleReturn.refundAmount > 0 ? `Refund ₹${money(saleReturn.refundAmount)} by ${METHOD_LABELS[saleReturn.refundMethod].toLowerCase()}` : `₹${money(saleReturn.returnAmount)} taken off the amount owed`,
      ),
    onError,
  });

  const collectMutation = useMutation({
    mutationFn: () => pharmacyApi.collectSalePayment(id, { method, amount: toNumber(amount), referenceNo: reference.trim() || undefined }),
    onSuccess: (updated) => done('Payment recorded', updated.balanceDue > 0 ? `₹${money(updated.balanceDue)} still due` : 'Bill is now fully paid'),
    onError,
  });

  const returnable = sale?.lines.some((line) => line.quantity - line.returnedQuantity > 0) ?? false;
  const returnValue =
    sale?.lines.reduce((sum, line) => {
      const qty = Math.min(toNumber(returnQty[line.id]), line.quantity - line.returnedQuantity);
      return sum + (qty > 0 ? (line.lineTotal * qty) / line.quantity : 0);
    }, 0) ?? 0;

  function submitReturn() {
    if (!sale) return;
    const over = sale.lines.find((line) => toNumber(returnQty[line.id]) > line.quantity - line.returnedQuantity);
    if (over) {
      setError(`Only ${quantity(over.quantity - over.returnedQuantity)} of ${over.productName} can be returned.`);
      return;
    }
    if (returnValue <= 0) {
      setError('Enter the quantity being returned on at least one line.');
      return;
    }
    setError(null);
    returnMutation.mutate();
  }

  function submitCollect() {
    if (!sale) return;
    const value = toNumber(amount);
    if (value <= 0 || value > sale.balanceDue) {
      setError(`Enter an amount up to ₹${money(sale.balanceDue)}.`);
      return;
    }
    setError(null);
    collectMutation.mutate();
  }

  return (
    <PharmacyPage
      title={sale ? `Bill ${sale.billNo}` : 'Bill'}
      subtitle={sale ? `${sale.customerName} · ${dateTimeLabel(sale.saleDate)}` : undefined}
      subLinks={SALES_LINKS}
      actions={
        <>
          <Button size="sm" variant="outline" asChild>
            <Link to="/pharmacy/sales">
              <ArrowLeft className="h-4 w-4" />
              All bills
            </Link>
          </Button>
          {sale && canAct && sale.balanceDue > 0 && (
            <Button size="sm" variant={mode === 'collect' ? 'default' : 'outline'} onClick={() => { setMode(mode === 'collect' ? 'view' : 'collect'); setAmount(String(sale.balanceDue)); setError(null); }}>
              <Wallet className="h-4 w-4" />
              Collect balance
            </Button>
          )}
          {sale && canAct && returnable && (
            <Button size="sm" variant={mode === 'return' ? 'default' : 'outline'} onClick={() => { setMode(mode === 'return' ? 'view' : 'return'); setError(null); }}>
              <Undo2 className="h-4 w-4" />
              Return items
            </Button>
          )}
          <Button size="sm" onClick={() => window.print()} disabled={!sale}>
            <Printer className="h-4 w-4" />
            Print
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
          This bill could not be loaded.
        </p>
      )}

      {sale && (
        <>
          <section className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_300px]">
            <div className="overflow-hidden rounded-lg border border-border bg-background">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3 text-sm">
                <div>
                  <p className="font-medium text-foreground">{sale.customerName}</p>
                  <p className="text-xs text-muted-foreground">
                    {[sale.patientUhid, sale.customerPhone, sale.doctorName ? `Dr: ${sale.doctorName.replace(/^dr\.?\s*/i, '')}` : null].filter(Boolean).join(' · ') || 'Walk-in customer'}
                  </p>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  <Badge variant="secondary">{sale.storeName}</Badge>
                  {sale.chargedToAdmission ? <Badge variant="secondary">IP credit</Badge> : sale.balanceDue > 0 ? <Badge variant="warning">Due ₹{money(sale.balanceDue)}</Badge> : <Badge variant="success">Paid</Badge>}
                </div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-muted/40 text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-3 py-2">#</th>
                      <th className="px-3 py-2">Item</th>
                      <th className="px-3 py-2">Batch · expiry</th>
                      <th className="px-3 py-2 text-right">Qty</th>
                      <th className="px-3 py-2 text-right">MRP</th>
                      <th className="px-3 py-2 text-right">Disc %</th>
                      <th className="px-3 py-2 text-right">GST %</th>
                      <th className="px-3 py-2 text-right">Amount</th>
                      {mode === 'return' && <th className="w-28 px-3 py-2 text-right">Return qty</th>}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {sale.lines.map((line) => {
                      const left = line.quantity - line.returnedQuantity;
                      const badge = SCHEDULE_BADGE[line.schedule];
                      return (
                        <tr key={line.id}>
                          <td className="px-3 py-2 text-muted-foreground">{line.lineNo}</td>
                          <td className="px-3 py-2">
                            <span className="font-medium text-foreground">{line.productName}</span>
                            {badge && <span className={cn('ml-1.5 rounded px-1 py-px text-[10px] font-semibold', badge.className)}>{badge.label}</span>}
                            {line.genericName && <div className="text-xs text-muted-foreground">{line.genericName}</div>}
                          </td>
                          <td className="px-3 py-2 font-mono text-xs">
                            {line.batchNo} · {expiryLabel(line.expiryDate)}
                          </td>
                          <td className="px-3 py-2 text-right tabular-nums">
                            {quantity(line.quantity)}
                            {line.returnedQuantity > 0 && <div className="text-xs text-muted-foreground">{quantity(line.returnedQuantity)} returned</div>}
                          </td>
                          <td className="px-3 py-2 text-right tabular-nums">{money(line.unitPrice)}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{line.discountPercent ? quantity(line.discountPercent) : '—'}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{quantity(line.gstRatePercent)}</td>
                          <td className="px-3 py-2 text-right font-medium tabular-nums">{money(line.lineTotal)}</td>
                          {mode === 'return' && (
                            <td className="px-3 py-1.5 text-right">
                              {left > 0 ? (
                                <input
                                  value={returnQty[line.id] ?? ''}
                                  inputMode="decimal"
                                  placeholder={`max ${quantity(left)}`}
                                  aria-label={`Quantity of ${line.productName} to return`}
                                  onChange={(event) => setReturnQty((current) => ({ ...current, [line.id]: event.target.value }))}
                                  className={cn(input, 'h-8 w-24 text-right tabular-nums')}
                                />
                              ) : (
                                <span className="text-xs text-muted-foreground">All returned</span>
                              )}
                            </td>
                          )}
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>

            <dl className="h-fit rounded-lg border border-border bg-background p-4 text-sm">
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Subtotal</dt>
                <dd className="tabular-nums">{money(sale.grossAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Discount</dt>
                <dd className="tabular-nums">− {money(sale.discountAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">GST included</dt>
                <dd className="tabular-nums">{money(sale.taxAmount)}</dd>
              </div>
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Round off</dt>
                <dd className="tabular-nums">{money(sale.roundOff)}</dd>
              </div>
              <div className="mt-1 flex items-baseline justify-between border-t border-border pt-2">
                <dt className="font-medium">Bill amount</dt>
                <dd className="text-xl font-semibold tabular-nums">₹{money(sale.netAmount)}</dd>
              </div>
              {sale.payments.map((payment) => (
                <div key={payment.id} className="flex justify-between py-0.5 text-muted-foreground">
                  <dt>
                    {METHOD_LABELS[payment.method]}
                    {payment.referenceNo ? ` · ${payment.referenceNo}` : ''}
                  </dt>
                  <dd className="tabular-nums">{money(payment.amount)}</dd>
                </div>
              ))}
              {sale.returnedAmount > 0 && (
                <div className="flex justify-between py-0.5 text-muted-foreground">
                  <dt>Returned</dt>
                  <dd className="tabular-nums">− {money(sale.returnedAmount)}</dd>
                </div>
              )}
              {sale.refundedAmount > 0 && (
                <div className="flex justify-between py-0.5 text-muted-foreground">
                  <dt>Refunded</dt>
                  <dd className="tabular-nums">{money(sale.refundedAmount)}</dd>
                </div>
              )}
              {sale.chargedToAdmission && <p className="mt-2 text-xs text-muted-foreground">The unpaid part of this bill is on the patient's admission and is settled with the hospital bill.</p>}
            </dl>
          </section>

          {mode === 'return' && (
            <section className="rounded-lg border border-primary/30 bg-primary/5 p-3">
              <div className="flex flex-wrap items-end gap-3">
                <label className="flex min-w-[220px] flex-1 flex-col gap-1 text-xs text-muted-foreground">
                  Reason
                  <input value={reason} maxLength={500} placeholder="Doctor changed the medicine, excess quantity…" onChange={(event) => setReason(event.target.value)} className={input} />
                </label>
                <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                  Refund by
                  <select value={method} onChange={(event) => setMethod(event.target.value as SalePaymentMethod)} className={input}>
                    {SALE_PAYMENT_METHODS.map((value) => (
                      <option key={value} value={value}>
                        {METHOD_LABELS[value]}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="flex items-center gap-2 pb-2 text-sm text-foreground">
                  <input type="checkbox" checked={restock} onChange={(event) => setRestock(event.target.checked)} />
                  Goods are fit to sell — put back in stock
                </label>
                <div className="ml-auto flex items-center gap-3">
                  <span className="text-sm text-muted-foreground">
                    Return value <span className="text-lg font-semibold tabular-nums text-foreground">₹{money(Math.round(returnValue))}</span>
                  </span>
                  <Button onClick={submitReturn} disabled={returnMutation.isPending}>
                    {returnMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Undo2 className="h-4 w-4" />}
                    Save return
                  </Button>
                </div>
              </div>
              {sale.balanceDue > 0 && <p className="mt-2 text-xs text-muted-foreground">₹{money(sale.balanceDue)} is still owed on this bill, so the return comes off that first and only the rest is refunded.</p>}
            </section>
          )}

          {mode === 'collect' && (
            <section className="flex flex-wrap items-end gap-3 rounded-lg border border-primary/30 bg-primary/5 p-3">
              <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                Paid by
                <select value={method} onChange={(event) => setMethod(event.target.value as SalePaymentMethod)} className={input}>
                  {SALE_PAYMENT_METHODS.map((value) => (
                    <option key={value} value={value}>
                      {METHOD_LABELS[value]}
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                Amount
                <input value={amount} inputMode="decimal" autoFocus onChange={(event) => setAmount(event.target.value)} onKeyDown={(event) => event.key === 'Enter' && submitCollect()} className={cn(input, 'w-32 text-right tabular-nums')} />
              </label>
              {method !== 'Cash' && (
                <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                  Reference no.
                  <input value={reference} maxLength={100} onChange={(event) => setReference(event.target.value)} className={input} />
                </label>
              )}
              <Button onClick={submitCollect} disabled={collectMutation.isPending}>
                {collectMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Wallet className="h-4 w-4" />}
                Record payment
              </Button>
            </section>
          )}

          {error && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}

          {returns && returns.items.length > 0 && (
            <section className="rounded-lg border border-border bg-background">
              <h2 className="border-b border-border px-4 py-2 text-sm font-medium">Returns against this bill</h2>
              <ul className="divide-y divide-border text-sm">
                {returns.items.map((saleReturn) => (
                  <li key={saleReturn.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-2">
                    <span>
                      <span className="font-mono text-xs font-medium text-primary">{saleReturn.returnNo}</span>
                      <span className="ml-2 text-muted-foreground">{dateTimeLabel(saleReturn.returnDate)}</span>
                      <span className="ml-2">{saleReturn.lines.map((line) => `${line.productName} × ${quantity(line.quantity)}`).join(', ')}</span>
                      {saleReturn.reason && <span className="ml-2 text-muted-foreground">({saleReturn.reason})</span>}
                    </span>
                    <span className="flex items-center gap-2 tabular-nums">
                      {!saleReturn.restocked && <Badge variant="warning">Not restocked</Badge>}
                      ₹{money(saleReturn.returnAmount)}
                      {saleReturn.refundAmount > 0 && <span className="text-muted-foreground">· refunded {money(saleReturn.refundAmount)}</span>}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <div className="print-target hidden print:block">
            <BillPrint sale={sale} settings={settings} duplicate />
          </div>
        </>
      )}
    </PharmacyPage>
  );
}
