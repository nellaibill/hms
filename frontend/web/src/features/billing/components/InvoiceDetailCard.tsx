import { Receipt } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useAuth } from '@/features/auth/AuthContext';
import { describeBillingItem, formatCurrency } from '../billingCalculations';
import type { Billing, BillingItem } from '../types';
import { PaymentStatusBadge } from './PaymentStatusBadge';

interface InvoiceDetailCardProps {
  billing: Billing;
  onRecordPayment: (item: BillingItem) => void;
}

export function InvoiceDetailCard({ billing, onRecordPayment }: InvoiceDetailCardProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('finance-billing.edit');
  return (
    <Card>
      <CardHeader className="flex-row items-center gap-3 space-y-0">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
          <Receipt className="h-5 w-5" />
        </span>
        <div className="flex flex-col gap-1">
          <CardTitle className="text-lg">
            {billing.patientName} <span className="font-normal text-muted-foreground">· {billing.patientUhid}</span>
          </CardTitle>
          <CardDescription>
            Invoice {billing.invoiceNumber ?? billing.id} · {new Date(billing.createdAt).toLocaleString('en-IN')}
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
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
                  {item.paymentStatus === 'Pending' && canEdit && (
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
