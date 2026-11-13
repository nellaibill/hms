import { Link, useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { SalesBrowser } from '@/features/pharmacy/sales/SalesBrowser';
import { ACCOUNTS_HUB_PATH, AccountsNavTabs, useAccountsCategory } from '@/features/reports';

/**
 * Pharmacy Accounts → Pharmacy Bills (ADR-134): the pharmacy's counter bills, balances due and
 * returns, read-only. Pharmacy bills are the pharmacy's own documents (ADR-094), never hospital
 * invoices, so they're listed from the pharmacy's API, which lets billing staff read them. A bill
 * opens on the pharmacy's own bill page, so rows open only for users with pharmacy access.
 */
export default function PharmacyBillsPage() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const category = useAccountsCategory();
  const canOpenBills = hasPermission('pharmacy.view');

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={category.icon}
        title={category.label}
        subtitle={category.description}
        backTo={ACCOUNTS_HUB_PATH}
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <SalesBrowser onOpenBill={canOpenBills ? (saleId) => navigate(`/pharmacy/sales/${saleId}`) : undefined} />

        {canOpenBills && (
          <p className="text-xs text-muted-foreground">
            Open a bill to reprint it or take a balance. Returns and new bills are made in{' '}
            <Link to="/pharmacy/sales" className="text-primary hover:underline">
              Pharmacy → Sales and returns
            </Link>
            .
          </p>
        )}
      </div>
    </div>
  );
}
