import type { PatientVisit } from '@hms/shared';
import { CalendarDays, FileText, Loader2, Plus, RotateCcw, Search, Wallet } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { describeBillingItem, formatCurrency, usePatientInvoicesQuery, type Billing, type BillingItem } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { encounterTypeShortLabel } from '../encounterTypeLabel';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';
import { inDateRange } from './dateRange';

type ContextTone = 'opd' | 'ipd' | 'other' | 'none';
type InvoiceStatus = 'Paid' | 'Pending' | 'Partially Paid' | 'Voided';
type StatusFilter = 'all' | 'Paid' | 'Pending' | 'Voided';

const CONTEXT_TONE_CLASSES: Record<ContextTone, string> = {
  opd: 'bg-emerald-500 text-white',
  ipd: 'bg-amber-400 text-white',
  other: 'bg-sky-500 text-white',
  none: 'border border-border bg-background text-muted-foreground',
};

const STATUS_PILL_CLASSES: Record<InvoiceStatus, string> = {
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400',
  Pending: 'bg-rose-100 text-rose-600 dark:bg-rose-500/15 dark:text-rose-400',
  'Partially Paid': 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  Voided: 'bg-muted text-muted-foreground line-through',
};

/** OPD/IPD context for one invoice, from the data the invoice is actually linked to — never
 * from service names. An invoice's visitId points at the Patients module's visit record, whose
 * visitType is the real encounter type (OP → OPD, IP → IPD, else e.g. Emergency/Day Care). The
 * IPD final bill has no visit (IPDBillingService falls back to visitId = patientId) but is the
 * only source of InpatientCharge lines, so those are IPD by construction. Anything else with no
 * visit link (e.g. a pharmacy dispense bill) is labelled as such rather than guessed. */
function billingContext(billing: Billing, visits: PatientVisit[] | undefined): { label: string; tone: ContextTone } {
  const visit = visits?.find((v) => v.visitId === billing.visitId);
  if (visit) {
    if (visit.visitType === 'OP') return { label: 'OPD', tone: 'opd' };
    if (visit.visitType === 'IP') return { label: 'IPD', tone: 'ipd' };
    return { label: encounterTypeShortLabel(visit.visitType), tone: 'other' };
  }
  if (billing.items.length > 0 && billing.items.every((item) => item.billingType === 'InpatientCharge')) {
    return { label: 'IPD', tone: 'ipd' };
  }
  return { label: 'No visit link', tone: 'none' };
}

/** The billing category a line belongs to — the "Department" column/filter. InpatientCharge
 * lines are the IPD final bill, so they read as IPD rather than the raw enum name. */
function itemDepartment(item: BillingItem): string {
  return item.billingType === 'InpatientCharge' ? 'IPD' : item.billingType;
}

function invoiceStatus(billing: Billing): InvoiceStatus {
  if (billing.isVoided) return 'Voided';
  const paid = billing.items.filter((item) => item.paymentStatus === 'Paid').length;
  if (paid === billing.items.length) return 'Paid';
  return paid === 0 ? 'Pending' : 'Partially Paid';
}

function StatusPill({ status }: { status: InvoiceStatus }) {
  return <span className={cn('inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium', STATUS_PILL_CLASSES[status])}>{status}</span>;
}

function SummaryTile({ label, value, className }: { label: string; value: string; className: string }) {
  return (
    <div className={cn('flex min-w-[6.5rem] flex-col items-center rounded-lg px-3 py-1.5', className)}>
      <span className="text-[11px] text-muted-foreground">{label}</span>
      <span className="text-base font-bold tabular-nums">{value}</span>
    </div>
  );
}

function formatAmount(value: number) {
  return value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

interface VisibleInvoice {
  billing: Billing;
  context: { label: string; tone: ContextTone };
  status: InvoiceStatus;
  items: BillingItem[];
}

/**
 * Patient Details' Billing panel — every invoice for this patient as its own itemised table,
 * with Total Billed/Paid/Pending tiles and client-side filters (date, encounter, department,
 * payment status, service search). Billing is its own bounded context (see features/billing);
 * this only reads it via the real Billing API. Invoice numbers open the Invoice Detail page
 * (record payment / void / print live there), and "New Bill" opens Invoice Create with this
 * patient preselected.
 */
export function PatientBillingPanel({ patientId }: { patientId: string }) {
  const { hasPermission } = useAuth();
  const canCreateBill = hasPermission('finance-billing.create');
  const { data: billings, isPending, isError } = usePatientInvoicesQuery(patientId);
  const { data: visits } = usePatientVisitsQuery(patientId);
  // Primes the Masters reference cache describeBillingItem reads from, so every line item
  // resolves to its real name instead of a raw id.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  // Radiology/Laboratory read the typed DiagnosticService/DiagnosticPackage catalogs (see
  // billingCalculations.ts's describeBillingItem) — these prime that cache the same way.
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [encounter, setEncounter] = useState('all');
  const [department, setDepartment] = useState('all');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const invoices = useMemo(
    () => (billings ?? []).map((billing) => ({ billing, context: billingContext(billing, visits), status: invoiceStatus(billing) })),
    [billings, visits],
  );
  const encounterOptions = useMemo(() => [...new Set(invoices.map((invoice) => invoice.context.label))], [invoices]);
  const departmentOptions = useMemo(
    () => [...new Set(invoices.flatMap((invoice) => invoice.billing.items.map(itemDepartment)))].sort(),
    [invoices],
  );

  const visible = useMemo<VisibleInvoice[]>(() => {
    const term = search.trim().toLowerCase();
    return invoices.flatMap((invoice) => {
      if (!inDateRange(invoice.billing.createdAt, from, to)) return [];
      if (encounter !== 'all' && invoice.context.label !== encounter) return [];
      if (status === 'Voided' && !invoice.billing.isVoided) return [];
      const items = invoice.billing.items.filter((item) => {
        if (department !== 'all' && itemDepartment(item) !== department) return false;
        if ((status === 'Paid' || status === 'Pending') && (invoice.billing.isVoided || item.paymentStatus !== status)) return false;
        if (term) {
          const { serviceLabel } = describeBillingItem(item);
          const haystack = `${item.billingType} ${serviceLabel} ${invoice.billing.invoiceNumber ?? ''}`.toLowerCase();
          if (!haystack.includes(term)) return false;
        }
        return true;
      });
      return items.length > 0 ? [{ ...invoice, items }] : [];
    });
  }, [invoices, from, to, encounter, department, status, search]);

  // Voided invoices were never really billed, so they're left out of every total.
  const totals = useMemo(() => {
    let billed = 0;
    let paid = 0;
    for (const invoice of visible) {
      if (invoice.billing.isVoided) continue;
      for (const item of invoice.items) {
        billed += item.total;
        if (item.paymentStatus === 'Paid') paid += item.total;
      }
    }
    return { billed, paid, pending: billed - paid };
  }, [visible]);

  const filtersActive = Boolean(from || to || search) || encounter !== 'all' || department !== 'all' || status !== 'all';

  function resetFilters() {
    setFrom('');
    setTo('');
    setEncounter('all');
    setDepartment('all');
    setStatus('all');
    setSearch('');
  }

  return (
    <section className="flex min-w-0 flex-col gap-3 rounded-xl border border-border bg-card p-4 shadow-sm">
      <header className="flex flex-wrap items-center gap-3">
        <h2 className="flex items-center gap-2 text-lg font-semibold text-foreground">
          <Wallet className="h-5 w-5 text-primary" />
          Billing
        </h2>
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <SummaryTile label="Total Billed" value={formatCurrency(totals.billed)} className="bg-sky-50 text-sky-700 dark:bg-sky-500/10 dark:text-sky-400" />
          <SummaryTile label="Total Paid" value={formatCurrency(totals.paid)} className="bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400" />
          <SummaryTile label="Pending" value={formatCurrency(totals.pending)} className="bg-orange-50 text-orange-600 dark:bg-orange-500/10 dark:text-orange-400" />
          {canCreateBill && (
            <Button asChild className="gap-1.5">
              <Link to={`/finance/accounts/new?patientId=${encodeURIComponent(patientId)}`}>
                <Plus className="h-4 w-4" />
                New Bill
              </Link>
            </Button>
          )}
        </div>
      </header>

      <div className="flex flex-wrap items-center gap-2">
        <div className="flex h-9 items-center gap-1.5 rounded-md border border-input bg-background px-2 text-sm">
          <CalendarDays className="h-4 w-4 shrink-0 text-muted-foreground" />
          <input type="date" aria-label="From date" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} className="bg-transparent outline-none" />
          <span className="text-muted-foreground">→</span>
          <input type="date" aria-label="To date" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} className="bg-transparent outline-none" />
        </div>
        <Select value={encounter} onValueChange={setEncounter}>
          <SelectTrigger className="h-9 w-32" aria-label="Filter by visit">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Visits</SelectItem>
            {encounterOptions.map((option) => (
              <SelectItem key={option} value={option}>
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={department} onValueChange={setDepartment}>
          <SelectTrigger className="h-9 w-40" aria-label="Filter by department">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Departments</SelectItem>
            {departmentOptions.map((option) => (
              <SelectItem key={option} value={option}>
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={(value) => setStatus(value as StatusFilter)}>
          <SelectTrigger className="h-9 w-32" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Status</SelectItem>
            <SelectItem value="Paid">Paid</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="Voided">Voided</SelectItem>
          </SelectContent>
        </Select>
        <div className="relative min-w-[10rem] flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input type="search" placeholder="Search by service…" value={search} onChange={(e) => setSearch(e.target.value)} className="h-9 pl-9" />
        </div>
        <Button type="button" variant="secondary" className="h-9 gap-1.5" onClick={resetFilters} disabled={!filtersActive}>
          <RotateCcw className="h-4 w-4" />
          Reset
        </Button>
      </div>

      {isPending ? (
        <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading billing…
        </div>
      ) : isError ? (
        // A failed request (e.g. a 403 for a role without finance-billing.view) must not fall
        // through to "No billing recorded", which misreads as fact.
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Couldn't load billing for this patient — please try again.
        </p>
      ) : invoices.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-10 text-sm text-muted-foreground">
          <FileText className="h-8 w-8" />
          No billing recorded for this patient yet.
        </div>
      ) : visible.length === 0 ? (
        <p className="py-10 text-center text-sm text-muted-foreground">No bills match the current filters.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {visible.map((invoice) => (
            <InvoiceTable key={invoice.billing.id} invoice={invoice} />
          ))}
        </div>
      )}
    </section>
  );
}

function InvoiceTable({ invoice }: { invoice: VisibleInvoice }) {
  const { billing, context, status, items } = invoice;
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <div className="flex flex-wrap items-center gap-3 bg-muted/50 px-3 py-2">
        <span
          className={cn('rounded-full px-3 py-0.5 text-xs font-semibold', CONTEXT_TONE_CLASSES[context.tone])}
          title="Encounter this bill belongs to"
        >
          {context.label}
        </span>
        <Link to={`/finance/accounts/${billing.id}`} className="text-sm font-semibold text-foreground hover:text-primary hover:underline">
          {billing.invoiceNumber ?? billing.id}
        </Link>
        <span className="text-sm text-muted-foreground">{new Date(billing.createdAt).toLocaleDateString('en-IN')}</span>
        <span className="ml-auto">
          <StatusPill status={status} />
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[40rem] text-sm">
          <thead>
            <tr className="border-b border-border text-xs text-muted-foreground">
              <th className="w-10 px-3 py-2 text-left font-medium">#</th>
              <th className="px-3 py-2 text-left font-medium">Service / Item</th>
              <th className="px-3 py-2 text-left font-medium">Department</th>
              <th className="px-3 py-2 text-center font-medium">Qty</th>
              <th className="px-3 py-2 text-right font-medium">Rate (₹)</th>
              <th className="px-3 py-2 text-right font-medium">Amount (₹)</th>
              <th className="px-3 py-2 text-center font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item, index) => {
              const described = describeBillingItem(item);
              const serviceLabel = described.serviceLabel;
              // describeBillingItem uses '—' as its "no consultant" placeholder (Pharmacy,
              // InpatientCharge, lines billed without a doctor) — don't render it as a sub-line.
              const consultantName = described.consultantName === '—' ? '' : described.consultantName;
              return (
                <tr key={item.id} className="border-b border-border last:border-b-0">
                  <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                  <td className="px-3 py-2">
                    <span className="text-foreground">
                      {item.billingType} — {serviceLabel}
                    </span>
                    {(consultantName || item.discount > 0) && (
                      <span className="block text-xs text-muted-foreground">
                        {consultantName}
                        {item.discount > 0 ? `${consultantName ? ' · ' : ''}Discount ${formatCurrency(item.discount)}` : ''}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2">{itemDepartment(item)}</td>
                  <td className="px-3 py-2 text-center tabular-nums">{item.quantity}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{formatAmount(item.unitPrice)}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{formatAmount(item.total)}</td>
                  <td className="px-3 py-2 text-center">
                    <StatusPill status={billing.isVoided ? 'Voided' : item.paymentStatus} />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <div className="flex flex-wrap items-center justify-end gap-x-8 gap-y-1 border-t border-border px-3 py-2 text-sm">
        <span className="text-muted-foreground">
          Gross Amount <span className="ml-3 font-semibold text-foreground">{formatCurrency(billing.grossAmount)}</span>
        </span>
        {billing.totalDiscount > 0 && (
          <span className="text-muted-foreground">
            Discount <span className="ml-3 font-semibold text-foreground">−{formatCurrency(billing.totalDiscount)}</span>
          </span>
        )}
        <span className="text-muted-foreground">
          Net Amount <span className="ml-3 font-semibold text-orange-500">{formatCurrency(billing.netAmount)}</span>
        </span>
      </div>
    </div>
  );
}
