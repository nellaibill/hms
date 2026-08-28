import { Receipt, TriangleAlert } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useAuth } from '@/features/auth/AuthContext';
import { useMasterOptionsQuery } from '@/features/masters';
import { describeBillingItem, formatCurrency } from '../billingCalculations';
import type { Billing, BillingItem } from '../types';
import { PaymentStatusBadge } from './PaymentStatusBadge';

interface InvoiceDetailCardProps {
  billing: Billing;
  onRecordPayment: (item: BillingItem) => void;
  onVoidInvoice: () => void;
}

export function InvoiceDetailCard({ billing, onRecordPayment, onVoidInvoice }: InvoiceDetailCardProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('finance-billing.edit');
  const canVoid = hasPermission('finance-billing.delete');
  const hasAnyPayment = billing.items.some((item) => item.paymentStatus === 'Paid');
  // Primes the Masters reference cache describeBillingItem reads from below, so every line
  // item resolves to its real name instead of a raw id — this page can be opened directly,
  // without ever visiting the live billing form.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  return (
    <Card>
      <CardHeader className="flex-row items-center gap-3 space-y-0">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
          <Receipt className="h-5 w-5" />
        </span>
        <div className="flex flex-1 flex-col gap-1">
          <CardTitle className="text-lg">
            {billing.patientName} <span className="font-normal text-muted-foreground">· {billing.patientUhid}</span>
          </CardTitle>
          <CardDescription>
            Invoice {billing.invoiceNumber ?? billing.id} · {new Date(billing.createdAt).toLocaleString('en-IN')}
          </CardDescription>
        </div>
        {!billing.isVoided && !hasAnyPayment && canVoid && (
          <Button size="sm" variant="outline" className="text-destructive hover:text-destructive" onClick={onVoidInvoice}>
            Void Invoice
          </Button>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
        {billing.isVoided && (
          <div className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
            <div className="flex flex-col">
              <span className="font-medium">
                Voided{billing.voidedAt ? ` on ${new Date(billing.voidedAt).toLocaleString('en-IN')}` : ''}
              </span>
              {billing.voidReason && <span>{billing.voidReason}</span>}
            </div>
          </div>
        )}
        <div className="flex flex-col divide-y divide-border rounded-md border border-border">
          {billing.items.map((item) => {
            const { serviceLabel, consultantName } = describeBillingItem(item);
            return (
              <div key={item.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-medium text-foreground">
                    {item.billingType} — {serviceLabel}
                  </span>
                  <span className="text-xs text-muted-foreground">{consultantName}</span>
                  {item.discount > 0 && (
                    <span className="text-xs text-muted-foreground">
                      {formatCurrency(item.unitPrice)} − {formatCurrency(item.discount)} discount
                      {item.discountApprovedBy ? ` (approved by ${item.discountApprovedBy})` : ''}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-sm font-semibold text-foreground">{formatCurrency(item.total)}</span>
                  <PaymentStatusBadge status={item.paymentStatus} />
                  {item.paymentStatus === 'Pending' && canEdit && !billing.isVoided && (
                    <Button size="sm" variant="outline" onClick={() => onRecordPayment(item)}>
                      Record Payment
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        <Separator />

        <div className="flex flex-col gap-1.5 text-sm">
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Gross total</span>
            <span className="font-medium text-foreground">{formatCurrency(billing.grossAmount)}</span>
          </div>
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Discount</span>
            <span className="font-medium text-destructive">
              {billing.totalDiscount > 0 ? `- ${formatCurrency(billing.totalDiscount)}` : formatCurrency(0)}
            </span>
          </div>
          <div className="mt-1 flex items-center justify-between gap-3 border-t border-border pt-2">
            <span className="text-base font-semibold text-foreground">Net amount</span>
            <span className="text-base font-bold text-primary">{formatCurrency(billing.netAmount)}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
