import { ApiError, type StockReceiptFormValues } from '@hms/shared';
import { PackagePlus } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { useToast } from '@/components/ui/toast-context';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { StockReceiptForm, useCreateStockReceiptMutation } from '@/features/pharmacy/stock-receipts';

export default function StockReceiptCreatePage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const mutation = useCreateStockReceiptMutation();

  function handleSubmit(values: StockReceiptFormValues) {
    mutation.mutate(
      {
        productId: values.productId,
        productBatchId: values.productBatchId,
        quantity: values.quantity,
        remarks: values.remarks || undefined,
      },
      {
        onSuccess: (receipt) => {
          toast({
            title: 'Stock receipt recorded',
            description: `${receipt.quantity} unit(s) of ${receipt.productName} (batch ${receipt.batchNo}) received — balance now ${receipt.balanceAfter}.`,
            variant: 'success',
          });
          navigate('/pharmacy/stock-receipts');
        },
      },
    );
  }

  return (
    <RequirePermission permission="pharmacy.create">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={PackagePlus}
          title="Receive Stock"
          subtitle="Record newly received stock against a product/batch."
          backTo="/pharmacy/stock-receipts"
          backLabel="Back to stock receipts"
        />

        <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
          <StockReceiptForm
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
