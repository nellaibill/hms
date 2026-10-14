import { ApiError, SUPPLIER_PAYMENT_METHODS, type SupplierBalanceResponse, type SupplierPaymentMethod } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Printer, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, money, toNumber } from '@/features/pharmacy/common/format';
import { SUPPLIER_PAYMENT_LABELS } from '@/features/pharmacy/common/labels';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const input = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * What the pharmacy owes each supplier. The left side is every supplier's balance; choosing
 * one shows their statement — purchases, returns and payments in date order with a running
 * balance, the way the supplier's own statement reads — and lets a payment be recorded.
 */
export default function SupplierAccountsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canSeeCost = hasPermission('pharmacy.view-cost');

  const [selected, setSelected] = useState<SupplierBalanceResponse | null>(null);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [paying, setPaying] = useState(false);
  const [amount, setAmount] = useState('');
  const [method, setMethod] = useState<SupplierPaymentMethod>('BankTransfer');
  const [reference, setReference] = useState('');
  const [error, setError] = useState<string | null>(null);

  const { data: balances, isPending, isError } = useQuery({ queryKey: ['pharmacy', 'supplier-accounts'], queryFn: () => pharmacyApi.getSupplierBalances(), enabled: canSeeCost });
  const { data: statement, isFetching } = useQuery({
    queryKey: ['pharmacy', 'supplier-accounts', 'statement', selected?.supplierId, fromDate, toDate],
    queryFn: () => pharmacyApi.getSupplierStatement(selected!.supplierId, fromDate || undefined, toDate || undefined),
    enabled: Boolean(selected),
  });

  const payMutation = useMutation({
    mutationFn: () => pharmacyApi.recordSupplierPayment(selected!.supplierId, { amount: toNumber(amount), method, referenceNo: reference.trim() || undefined }),
    onSuccess: (payment) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy', 'supplier-accounts'] });
      toast({ title: `Payment recorded · ${payment.paymentNo}`, description: `${payment.supplierName} · ₹${money(payment.amount)}`, variant: 'success' });
      setSelected((current) => (current ? { ...current, payments: current.payments + payment.amount, outstanding: current.outstanding - payment.amount } : current));
      setPaying(false);
      setAmount('');
      setReference('');
      setError(null);
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not record the payment.'),
  });

  const totalOutstanding = balances?.reduce((sum, balance) => sum + Math.max(balance.outstanding, 0), 0) ?? 0;

  if (!canSeeCost) {
    return (
      <PharmacyPage title="Supplier accounts" subLinks={PURCHASE_LINKS}>
        <p className="rounded-lg border border-border bg-background p-6 text-sm text-muted-foreground">Supplier accounts show what the pharmacy pays its suppliers, so they are limited to users who can view cost.</p>
      </PharmacyPage>
    );
  }

  return (
    <PharmacyPage title="Supplier accounts" subtitle={balances ? `₹${money(totalOutstanding)} owed to suppliers in all` : 'What is owed to each supplier.'} subLinks={PURCHASE_LINKS}>
      <div className="grid gap-4 xl:grid-cols-[minmax(320px,2fr)_minmax(0,3fr)]">
        <section className="h-fit overflow-hidden rounded-lg border border-border bg-background print:hidden">
          {isPending && (
            <p className="flex items-center gap-2 p-6 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading…
            </p>
          )}
          {isError && <p className="p-6 text-sm text-destructive">Supplier accounts could not be loaded.</p>}
          {balances && balances.length === 0 && <p className="p-6 text-sm text-muted-foreground">No purchases have been entered yet.</p>}
          {balances && balances.length > 0 && (
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-3 py-2">Supplier</th>
                  <th className="px-3 py-2 text-right">Purchases</th>
                  <th className="px-3 py-2 text-right">Outstanding</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {balances.map((balance) => (
                  <tr
                    key={balance.supplierId}
                    onClick={() => { setSelected(balance); setPaying(false); setError(null); }}
                    className={cn('cursor-pointer', selected?.supplierId === balance.supplierId ? 'bg-primary/10' : 'hover:bg-muted/40')}
                  >
                    <td className="px-3 py-2">
                      <div className="font-medium text-foreground">{balance.supplierName}</div>
                      <div className="text-xs text-muted-foreground">Last purchase {dateLabel(balance.lastPurchaseDate)}</div>
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{money(balance.purchases)}</td>
                    <td className={cn('px-3 py-2 text-right font-medium tabular-nums', balance.outstanding > 0 ? 'text-foreground' : 'text-success')}>{money(balance.outstanding)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        <section className="print-target rounded-lg border border-border bg-background">
          {!selected ? (
            <p className="p-10 text-center text-sm text-muted-foreground">Choose a supplier to see their statement.</p>
          ) : (
            <>
              <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3">
                <div>
                  <h2 className="font-semibold text-foreground">{selected.supplierName}</h2>
                  <p className="text-xs text-muted-foreground">
                    Bought {money(selected.purchases)} · returned {money(selected.returns)} · paid {money(selected.payments)}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2 print:hidden">
                  <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    From
                    <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} className={input} />
                  </label>
                  <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    To
                    <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} className={input} />
                  </label>
                  <Button size="sm" variant="outline" onClick={() => window.print()}>
                    <Printer className="h-4 w-4" />
                    Print
                  </Button>
                  {hasPermission('pharmacy.create') && (
                    <Button size="sm" onClick={() => { setPaying(!paying); setAmount(selected.outstanding > 0 ? String(selected.outstanding) : ''); }}>
                      <Wallet className="h-4 w-4" />
                      Record payment
                    </Button>
                  )}
                </div>
              </header>

              {paying && (
                <div className="flex flex-wrap items-end gap-3 border-b border-border bg-primary/5 px-4 py-3 print:hidden">
                  <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                    Amount
                    <input value={amount} inputMode="decimal" autoFocus onChange={(event) => setAmount(event.target.value)} className={cn(input, 'w-32 text-right tabular-nums')} />
                  </label>
                  <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                    Paid by
                    <select value={method} onChange={(event) => setMethod(event.target.value as SupplierPaymentMethod)} className={input}>
                      {SUPPLIER_PAYMENT_METHODS.map((value) => (
                        <option key={value} value={value}>
                          {SUPPLIER_PAYMENT_LABELS[value]}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="flex flex-col gap-1 text-xs text-muted-foreground">
                    Cheque / reference no.
                    <input value={reference} maxLength={100} onChange={(event) => setReference(event.target.value)} className={input} />
                  </label>
                  <Button onClick={() => (toNumber(amount) > 0 ? payMutation.mutate() : setError('Enter the amount paid.'))} disabled={payMutation.isPending}>
                    {payMutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                    Save payment
                  </Button>
                  {error && <p className="w-full text-sm text-destructive">{error}</p>}
                </div>
              )}

              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th className="px-4 py-2">Date</th>
                      <th className="px-4 py-2">Entry</th>
                      <th className="px-4 py-2">Document</th>
                      <th className="px-4 py-2">Reference</th>
                      <th className="px-4 py-2 text-right">Purchase</th>
                      <th className="px-4 py-2 text-right">Return / paid</th>
                      <th className="px-4 py-2 text-right">Balance</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {statement && statement.openingBalance !== 0 && (
                      <tr className="text-muted-foreground">
                        <td className="px-4 py-2" colSpan={6}>
                          Balance brought forward
                        </td>
                        <td className="px-4 py-2 text-right tabular-nums">{money(statement.openingBalance)}</td>
                      </tr>
                    )}
                    {statement?.entries.map((entry) => (
                      <tr key={`${entry.type}-${entry.documentId}`}>
                        <td className="whitespace-nowrap px-4 py-2">{dateLabel(entry.date)}</td>
                        <td className="px-4 py-2">
                          <Badge variant={entry.type === 'Purchase' ? 'secondary' : 'success'}>{entry.type}</Badge>
                        </td>
                        <td className="whitespace-nowrap px-4 py-2 font-mono text-xs">
                          {entry.type === 'Purchase' ? (
                            <Link to={`/pharmacy/purchases/${entry.documentId}`} className="font-medium text-primary hover:underline">
                              {entry.documentNo}
                            </Link>
                          ) : (
                            entry.documentNo
                          )}
                        </td>
                        <td className="px-4 py-2 text-muted-foreground">{entry.reference ?? '—'}</td>
                        <td className="px-4 py-2 text-right tabular-nums">{entry.credit ? money(entry.credit) : ''}</td>
                        <td className="px-4 py-2 text-right tabular-nums text-success">{entry.debit ? money(entry.debit) : ''}</td>
                        <td className="px-4 py-2 text-right font-medium tabular-nums">{money(entry.balance)}</td>
                      </tr>
                    ))}
                    {statement && statement.entries.length === 0 && (
                      <tr>
                        <td colSpan={7} className="px-4 py-10 text-center text-muted-foreground">
                          Nothing in this period.
                        </td>
                      </tr>
                    )}
                  </tbody>
                  {statement && (
                    <tfoot>
                      <tr className="border-t border-border font-semibold">
                        <td className="px-4 py-2" colSpan={6}>
                          {isFetching ? 'Updating…' : 'Amount owed'}
                        </td>
                        <td className="px-4 py-2 text-right tabular-nums">₹{money(statement.closingBalance)}</td>
                      </tr>
                    </tfoot>
                  )}
                </table>
              </div>
            </>
          )}
        </section>
      </div>
    </PharmacyPage>
  );
}
