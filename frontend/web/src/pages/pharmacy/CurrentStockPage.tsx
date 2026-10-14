import { PackagePlus, ReceiptText } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { STOCK_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StockSummaryPanel } from '@/features/pharmacy/stock-balances';

/**
 * Current stock ('/pharmacy/stock') — stock per product, searchable, with the needs-reorder and
 * expiry views. The pharmacy's landing page is the dashboard; everything else in the module is
 * reached from the pharmacy menu across the top.
 */
export default function CurrentStockPage() {
  const { hasPermission } = useAuth();
  const canCreate = hasPermission('pharmacy.create');

  return (
    <PharmacyPage
      title="Current stock"
      subtitle="What is on the shelf, what needs reordering, and what is close to expiry."
      subLinks={STOCK_LINKS}
      actions={
        canCreate && (
          <>
            <Button size="sm" variant="outline" asChild>
              <Link to="/pharmacy/counter">
                <ReceiptText className="h-4 w-4" />
                Billing counter
              </Link>
            </Button>
            <Button size="sm" asChild>
              <Link to="/pharmacy/purchases/new">
                <PackagePlus className="h-4 w-4" />
                New purchase
              </Link>
            </Button>
          </>
        )
      }
    >
      <StockSummaryPanel />
    </PharmacyPage>
  );
}
