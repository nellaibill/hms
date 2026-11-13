import { Wallet } from 'lucide-react';
import { HubCard } from '@/components/HubCard';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { usePendingDiscountCountQuery, usePendingRevertCountQuery } from '@/features/billing';
import { accountsPath, ACCOUNTS_CATEGORIES } from '@/features/reports';

/**
 * Accounts and Finance's landing page (ADR-134): one card per category — OP, IP, Pharmacy and
 * Lab Accounts — laid out like Reception & Registration's hub. Each card opens that category's
 * own tab bar (Recent Bills, All Invoices and its reports). A category whose module the hospital
 * doesn't have is left out. OP's card carries the approvals waiting in its queues.
 */
export default function AccountsHubPage() {
  const { hasFeature } = useAuth();
  const categories = ACCOUNTS_CATEGORIES.filter((category) => !category.feature || hasFeature(category.feature));
  const { data: pendingRevertCount = 0 } = usePendingRevertCountQuery();
  const { data: pendingDiscountCount = 0 } = usePendingDiscountCountQuery();
  const pendingApprovals = pendingRevertCount + pendingDiscountCount;

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner icon={Wallet} title="Accounts and Finance" subtitle="Choose an account — OP, IP, Pharmacy or Lab — to see its bills and reports." />

      <div className="flex flex-1 flex-col items-center justify-center p-6 lg:p-8">
        <div className="grid w-full max-w-4xl grid-cols-1 gap-6 sm:grid-cols-2">
          {categories.map((category) => (
            <HubCard
              key={category.key}
              to={accountsPath(category.key)}
              title={category.label}
              description={category.description}
              icon={category.icon}
              className="h-full max-w-none sm:max-w-none"
              badge={
                category.key === 'op' && pendingApprovals > 0 ? (
                  <span className="rounded-full bg-warning px-2 py-0.5 text-xs font-semibold text-warning-foreground">{pendingApprovals} pending</span>
                ) : undefined
              }
            />
          ))}
        </div>
      </div>
    </div>
  );
}
