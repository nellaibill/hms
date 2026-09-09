import { Clock3, Loader2, Plus, Wallet } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import { AccountsNavTabs } from '@/features/reports';
import { RecentPatientBillsTable, useRecentBillsQuery } from '../../features/billing';

const RECENT_BILLS_COUNT = 10;

/** The Patient Billing page's "at a glance" strip — the latest 10 bills across every patient,
 * fetched pre-limited from the server (not paged/filtered client-side), separate from the
 * searchable/paginated ledger on the "All Invoices" tab (AllInvoicesPage.tsx). */
function RecentPatientBillsSection() {
  const { data: bills, isPending, isError } = useRecentBillsQuery(RECENT_BILLS_COUNT);

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-1.5">
        <Clock3 className="h-4 w-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Recent Patient Bills</h2>
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
            <p className="text-sm text-muted-foreground">No bills recorded yet.</p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && bills && bills.length > 0 && <RecentPatientBillsTable bills={bills} />}
    </div>
  );
}

/** Finance & Billing's landing screen — the "Recent Bills" tab of the Accounts & Finance
 * section (docs/ScreenInventory.md "Finance & Billing Domain"). AccountsNavTabs is the single
 * tab bar shared by this page, All Invoices, and every report — previously the Ledger had its
 * own separate tab widget and the reports had a second, different one, reached only via a
 * "Reports" button; combined into one continuous bar so every view in the section is a single
 * click away from any other. */
export default function InvoiceLedgerPage() {
  const { hasPermission } = useAuth();

  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Wallet className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Accounts and Finance</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Unified invoice ledger — every OP, Radiology, Laboratory, and Procedure bill in one place.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <AccountsNavTabs />
          {hasPermission('finance-billing.create') && (
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
