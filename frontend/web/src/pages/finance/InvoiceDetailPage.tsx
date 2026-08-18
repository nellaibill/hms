import { ArrowLeft, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  InvoiceDetailCard,
  RecordPaymentDialog,
  describeBillingItem,
  useBillingQuery,
  useRecordPaymentMutation,
  type BillingItem,
  type PaymentMethod,
} from '../../features/billing';

export default function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: billing, isPending, isError } = useBillingQuery(id);
  const [itemPendingPayment, setItemPendingPayment] = useState<BillingItem | null>(null);
  const recordPaymentMutation = useRecordPaymentMutation();

  function handleConfirmPayment(method: PaymentMethod) {
    if (!billing || !itemPendingPayment) return;
    recordPaymentMutation.mutate(
      { billingId: billing.id, itemId: itemPendingPayment.id, method },
      { onSuccess: () => setItemPendingPayment(null) },
    );
  }

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading invoice…
      </div>
    );
  }

  if (isError || !billing) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Invoice not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/finance/accounts" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to Accounts and Finance
        </Link>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        {/* Left-aligned, not centered — matches PatientRegistrationForm/InvoiceCreatePage
            rather than leaving a large empty gutter before the card. */}
        <div className="w-full max-w-3xl">
          <InvoiceDetailCard billing={billing} onRecordPayment={setItemPendingPayment} />
        </div>
      </div>

      {itemPendingPayment && (
        <RecordPaymentDialog
          serviceLabel={`${itemPendingPayment.billingType} — ${describeBillingItem(itemPendingPayment).serviceLabel}`}
          amount={itemPendingPayment.total}
          isSaving={recordPaymentMutation.isPending}
          onConfirm={handleConfirmPayment}
          onCancel={() => setItemPendingPayment(null)}
        />
      )}
    </div>
  );
}
