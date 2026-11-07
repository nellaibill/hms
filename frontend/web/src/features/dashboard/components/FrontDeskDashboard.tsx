import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import {
  ArrowRight,
  BadgePercent,
  Loader2,
  Receipt,
  RotateCcw,
  Stethoscope,
  UserPlus,
  Wallet,
  type LucideIcon,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/AuthContext';
import {
  formatCurrency,
  formatShortInvoiceNumber,
  PAYMENT_METHOD_LABELS,
} from '@/features/billing';
import { formatConsultantAvailability } from '@/features/masters';
import { cn } from '@/lib/utils';
import {
  frontDeskBillState,
  getTodayRange,
  sumCollectionsToday,
  sumOpdSummary,
  summarizeBillsToday,
  type FrontDeskBillState,
} from '../frontDeskToday';
import {
  useBillsTodayQuery,
  useCollectionsTodayQuery,
  useOpdTodayQuery,
  usePendingApprovalsQuery,
  useRegistrationsTodayQuery,
} from '../hooks/useFrontDeskToday';
import { NotificationsCard } from './NotificationsCard';
import { SectionHeader } from './SectionHeader';

interface StatTileProps {
  icon: LucideIcon;
  iconClassName: string;
  label: string;
  value: ReactNode;
  detail?: ReactNode;
  to?: string;
  loading?: boolean;
  failed?: boolean;
}

function StatTile({
  icon: Icon,
  iconClassName,
  label,
  value,
  detail,
  to,
  loading,
  failed,
}: StatTileProps) {
  const body = (
    <Card className={cn('h-full transition-shadow', to && 'hover:shadow-soft-lg')}>
      <CardContent className="flex items-start gap-4 p-5">
        <div
          className={cn(
            'flex h-10 w-10 shrink-0 items-center justify-center rounded-lg',
            iconClassName,
          )}
        >
          <Icon className="h-5 w-5" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="mt-0.5 text-2xl font-semibold tabular-nums text-foreground">
            {loading ? (
              <Loader2 className="mt-1 h-5 w-5 animate-spin text-muted-foreground" />
            ) : failed ? (
              '—'
            ) : (
              value
            )}
          </p>
          {!loading && (
            <p className="mt-1 text-xs text-muted-foreground">
              {failed ? "Couldn't load — it will try again shortly." : detail}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
  return to ? (
    <Link
      to={to}
      className="block h-full rounded-lg focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      {body}
    </Link>
  ) : (
    body
  );
}

function CardMessage({ children }: { children: ReactNode }) {
  return <p className="px-1 py-6 text-center text-sm text-muted-foreground">{children}</p>;
}

const BILL_STATE_BADGES: Record<
  FrontDeskBillState,
  { label: string; variant: 'success' | 'warning' | 'secondary' }
> = {
  Paid: { label: 'Paid', variant: 'success' },
  Pending: { label: 'Pending', variant: 'warning' },
  OnHold: { label: 'On hold', variant: 'warning' },
  Voided: { label: 'Voided', variant: 'secondary' },
  Reverted: { label: 'Reverted', variant: 'secondary' },
};

function timeOf(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-IN', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function HeaderLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      className="inline-flex shrink-0 items-center gap-1 text-sm font-medium text-primary hover:underline"
    >
      {children}
      <ArrowRight className="h-3.5 w-3.5" />
    </Link>
  );
}

/**
 * The receptionist's dashboard (ADR-129): today only, refreshed every minute. Tiles show today's
 * OP visits, new registrations, money taken and bills still to collect. Below them are each
 * doctor's queue, the latest bills, and requests waiting for a decision. Each part needs the
 * permission its data does, so a role without one just doesn't see that part.
 */
export function FrontDeskDashboard() {
  const { hasPermission } = useAuth();
  const canViewOpd = hasPermission('clinical-care.view');
  const canViewPatients = hasPermission('patient-management.view');
  const canViewBilling = hasPermission('finance-billing.view');

  // Worked out on every render, so a dashboard left open past midnight moves to the new day on its next refresh.
  const range = getTodayRange();

  const opd = useOpdTodayQuery(range, canViewOpd);
  const registrations = useRegistrationsTodayQuery(range, canViewPatients);
  const collections = useCollectionsTodayQuery(range, canViewBilling);
  const bills = useBillsTodayQuery(range, canViewBilling);
  const approvals = usePendingApprovalsQuery(canViewBilling);

  const opdRows = [...(opd.data ?? [])].sort(
    (a, b) => b.waiting - a.waiting || b.totalPatients - a.totalPatients,
  );
  const opdTotals = sumOpdSummary(opdRows);
  const collected = sumCollectionsToday(collections.data ?? []);
  const billsToday = summarizeBillsToday(bills.data ?? []);

  return (
    <div className="flex flex-1 flex-col gap-8 p-6 lg:p-8">
      <section>
        <SectionHeader title="Today" description="Updates every minute" />
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
          {canViewOpd && (
            <StatTile
              icon={Stethoscope}
              iconClassName="bg-sky-500/10 text-sky-600"
              label="OP visits"
              value={opdTotals.total}
              detail={`${opdTotals.waiting} waiting · ${opdTotals.inConsultation} with doctor · ${opdTotals.completed} done`}
              to="/clinical/opd"
              loading={opd.isLoading}
              failed={opd.isError && !opd.data}
            />
          )}
          {canViewPatients && (
            <StatTile
              icon={UserPlus}
              iconClassName="bg-violet-500/10 text-violet-600"
              label="New registrations"
              value={registrations.data ?? 0}
              detail="Patients registered for the first time today"
              to="/patients/registration"
              loading={registrations.isLoading}
              failed={registrations.isError && registrations.data === undefined}
            />
          )}
          {canViewBilling && (
            <StatTile
              icon={Wallet}
              iconClassName="bg-emerald-500/10 text-emerald-600"
              label="Collected"
              value={formatCurrency(collected.total)}
              detail={
                collected.byMethod.length > 0
                  ? collected.byMethod
                      .map(
                        (entry) =>
                          `${PAYMENT_METHOD_LABELS[entry.method] ?? entry.method} ${formatCurrency(entry.amount)}`,
                      )
                      .join(' · ')
                  : 'No payments yet today'
              }
              loading={collections.isLoading}
              failed={collections.isError && !collections.data}
            />
          )}
          {canViewBilling && (
            <StatTile
              icon={Receipt}
              iconClassName="bg-amber-500/10 text-amber-600"
              label="Waiting for payment"
              value={billsToday.unpaidCount}
              detail={
                billsToday.unpaidCount === 0 && billsToday.onHoldCount === 0
                  ? `All ${billsToday.count} of today's bills settled`
                  : [
                      `${formatCurrency(billsToday.unpaidAmount)} to collect`,
                      billsToday.onHoldCount > 0 && `${billsToday.onHoldCount} on hold`,
                    ]
                      .filter(Boolean)
                      .join(' · ')
              }
              to="/finance/accounts"
              loading={bills.isLoading}
              failed={bills.isError && !bills.data}
            />
          )}
        </div>
      </section>

      <section className="grid grid-cols-12 gap-5">
        {canViewOpd && (
          <Card className={cn('col-span-12', canViewBilling && 'xl:col-span-7')}>
            <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-3">
              <div>
                <CardTitle className="text-base">Doctors today</CardTitle>
                <CardDescription className="mt-0.5">
                  Each doctor's OP queue, busiest first
                </CardDescription>
              </div>
              <HeaderLink to="/clinical/opd">Open OPD</HeaderLink>
            </CardHeader>
            <CardContent>
              {opd.isLoading ? (
                <CardMessage>Loading…</CardMessage>
              ) : opdRows.length === 0 ? (
                <CardMessage>
                  {opd.isError ? "Couldn't load today's visits." : 'No OP visits yet today.'}
                </CardMessage>
              ) : (
                <div className="overflow-x-auto rounded-lg border border-border">
                  <table className="w-full min-w-[560px] text-sm">
                    <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
                      <tr>
                        <th className="px-4 py-2.5">Doctor</th>
                        <th className="px-4 py-2.5 text-right">Waiting</th>
                        <th className="px-4 py-2.5 text-right">With doctor</th>
                        <th className="px-4 py-2.5 text-right">Done</th>
                        <th className="px-4 py-2.5 text-right">Total</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {opdRows.map((row) => {
                        const hours = formatConsultantAvailability({
                          availableDays: [],
                          visitStartTime: row.visitStartTime,
                          visitEndTime: row.visitEndTime,
                          visitStartTime2: row.visitStartTime2,
                          visitEndTime2: row.visitEndTime2,
                        });
                        return (
                          <tr
                            key={`${row.consultantId}-${row.departmentId}`}
                            className="hover:bg-muted/30"
                          >
                            <td className="px-4 py-2.5">
                              <p className="font-medium text-foreground">{row.consultantName}</p>
                              <p className="text-xs text-muted-foreground">
                                {[row.departmentName, hours].filter(Boolean).join(' · ')}
                              </p>
                            </td>
                            <td
                              className={cn(
                                'px-4 py-2.5 text-right tabular-nums',
                                row.waiting > 0
                                  ? 'font-semibold text-foreground'
                                  : 'text-muted-foreground',
                              )}
                            >
                              {row.waiting}
                            </td>
                            <td className="px-4 py-2.5 text-right tabular-nums text-foreground">
                              {row.inConsultation}
                            </td>
                            <td className="px-4 py-2.5 text-right tabular-nums text-foreground">
                              {row.completed}
                            </td>
                            <td className="px-4 py-2.5 text-right font-medium tabular-nums text-foreground">
                              {row.totalPatients}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        )}

        {canViewBilling && (
          <Card className={cn('col-span-12', canViewOpd && 'xl:col-span-5')}>
            <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-3">
              <div>
                <CardTitle className="text-base">Recent bills</CardTitle>
                <CardDescription className="mt-0.5">Today's latest bills</CardDescription>
              </div>
              <HeaderLink to="/finance/accounts">All bills</HeaderLink>
            </CardHeader>
            <CardContent>
              {bills.isLoading ? (
                <CardMessage>Loading…</CardMessage>
              ) : billsToday.recent.length === 0 ? (
                <CardMessage>
                  {bills.isError ? "Couldn't load today's bills." : 'No bills yet today.'}
                </CardMessage>
              ) : (
                <ul className="divide-y divide-border">
                  {billsToday.recent.map((bill) => {
                    const badge = BILL_STATE_BADGES[frontDeskBillState(bill)];
                    return (
                      <li key={bill.id}>
                        <Link
                          to={`/finance/accounts/${bill.id}`}
                          className="flex items-center gap-3 rounded-md px-1 py-2.5 hover:bg-muted/40"
                        >
                          <span className="w-16 shrink-0 font-mono text-xs text-muted-foreground">
                            {timeOf(bill.createdAt)}
                          </span>
                          <span className="min-w-0 flex-1">
                            <span className="block truncate text-sm font-medium text-foreground">
                              {bill.patientName}
                            </span>
                            <span
                              className="block text-xs text-muted-foreground"
                              title={bill.invoiceNumber}
                            >
                              Bill {formatShortInvoiceNumber(bill.invoiceNumber)} ·{' '}
                              {bill.patientUhid}
                            </span>
                          </span>
                          <span className="flex shrink-0 flex-col items-end gap-1">
                            <span className="text-sm font-medium tabular-nums text-foreground">
                              {formatCurrency(bill.netAmount)}
                            </span>
                            <Badge variant={badge.variant}>{badge.label}</Badge>
                          </span>
                        </Link>
                      </li>
                    );
                  })}
                </ul>
              )}
            </CardContent>
          </Card>
        )}
      </section>

      <section className="grid grid-cols-12 gap-5">
        {canViewBilling && (
          <Card className="col-span-12 lg:col-span-5">
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Waiting for approval</CardTitle>
              <CardDescription className="mt-0.5">
                Requests not decided yet, from any day
              </CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-2">
              {[
                {
                  icon: BadgePercent,
                  label: 'Discount requests',
                  hint: 'Bills on hold until decided',
                  count: approvals.data?.discounts,
                  to: '/finance/accounts/discount-requests',
                },
                {
                  icon: RotateCcw,
                  label: 'Revert requests',
                  hint: 'Bills waiting to be reverted',
                  count: approvals.data?.reverts,
                  to: '/finance/accounts/revert-requests',
                },
              ].map((entry) => (
                <Link
                  key={entry.label}
                  to={entry.to}
                  className="flex items-center gap-3 rounded-lg border border-border px-4 py-3 hover:bg-muted/40"
                >
                  <entry.icon className="h-5 w-5 shrink-0 text-muted-foreground" />
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-medium text-foreground">{entry.label}</span>
                    <span className="block text-xs text-muted-foreground">{entry.hint}</span>
                  </span>
                  <span
                    className={cn(
                      'text-xl font-semibold tabular-nums',
                      entry.count ? 'text-foreground' : 'text-muted-foreground',
                    )}
                  >
                    {approvals.isLoading ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      (entry.count ?? '—')
                    )}
                  </span>
                </Link>
              ))}
            </CardContent>
          </Card>
        )}
        <div className={cn('col-span-12', canViewBilling && 'lg:col-span-7')}>
          <NotificationsCard />
        </div>
      </section>
    </div>
  );
}
