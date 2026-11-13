import { Clock3, Loader2, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { ACCOUNTS_HUB_PATH, AccountsNavTabs, useAccountsCategory } from '@/features/reports';
import { RecentPatientBillsTable, useRecentBillsQuery } from '../../features/billing';

const RECENT_BILLS_COUNT = 10;

/** The category's "at a glance" strip — its latest 10 bills, fetched pre-limited and filtered
 * by category on the server (ADR-134), not paged/filtered client-side; the searchable/paginated
 * ledger is the "All Invoices" tab (AllInvoicesPage.tsx). */
function RecentPatientBillsSection() {
  const category = useAccountsCategory();
  const { data: bills, isPending, isError } = useRecentBillsQuery(RECENT_BILLS_COUNT, category.invoiceCategory);

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-1.5">
        <Clock3 className="h-4 w-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Recent {category.shortLabel} Bills</h2>
      </div>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading recent bills…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load recent bills.
        </p>
      )}

      {!isPending && !isError && bills && bills.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-10 text-center">
            <p className="text-sm text-muted-foreground">No {category.shortLabel} bills recorded yet.</p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && bills && bills.length > 0 && <RecentPatientBillsTable bills={bills} />}
    </div>
  );
}

/** The "Recent Bills" tab — the landing page of the OP, IP and Lab Accounts categories
 * (ADR-134; docs/ScreenInventory.md "Finance & Billing Domain"). AccountsNavTabs is the
 * category's tab bar, shared by this page, All Invoices and the category's reports. */
export default function InvoiceLedgerPage() {
  const { hasPermission } = useAuth();
  const category = useAccountsCategory();

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
        <div className="flex flex-wrap items-center justify-between gap-3">
          <AccountsNavTabs />
          {category.canCreateInvoice && hasPermission('finance-billing.create') && (
            <Button asChild className="gap-1.5">
              <Link to="/finance/accounts/new">
                <Plus className="h-4 w-4" />
                New Invoice
              </Link>
            </Button>
          )}
        </div>

        <RecentPatientBillsSection />
      </div>
    </div>
  );
}
