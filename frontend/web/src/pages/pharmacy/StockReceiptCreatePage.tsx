import { ApiError, type StockReceiptFormValues } from '@hms/shared';
import { ArrowLeft, PackagePlus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
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
        <div className="px-6 pt-4 lg:px-8">
          <Link to="/pharmacy/stock-receipts" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
            <ArrowLeft className="h-4 w-4" />
            Back to stock receipts
          </Link>
        </div>

        <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <PackagePlus className="h-5 w-5" />
            </span>
            <h1 className="text-xl font-semibold tracking-tight">Receive Stock</h1>
          </div>
          <p className="max-w-2xl text-sm text-page-banner-foreground/85">Record newly received stock against a product/batch.</p>
        </div>

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
