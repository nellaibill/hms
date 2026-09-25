import type { Patient } from '@hms/shared';
import { CalendarDays, ChevronRight, ClipboardList, FileText, FlaskConical, History, Loader2, Receipt, Stethoscope, UserPlus } from 'lucide-react';
import { useMemo, useState, type ElementType } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAuth } from '@/features/auth/AuthContext';
import { formatCurrency, usePatientInvoicesQuery } from '@/features/billing';
import { useLabOrdersByPatientQuery } from '@/features/laboratory';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { useOpdConsultationsByPatientQuery } from '@/features/opdConsultation';
import { usePatientDocumentsQuery } from '../hooks/usePatientDocumentsQuery';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';
import { inDateRange } from './dateRange';

type EventCategory = 'registration' | 'visit' | 'consultation' | 'billing' | 'lab' | 'document';

const CATEGORY_LABELS: Record<EventCategory, string> = {
  registration: 'Registration',
  visit: 'Visits',
  consultation: 'Consultations',
  billing: 'Billing',
  lab: 'Laboratory',
  document: 'Documents',
};

/** Icon bubble + rail dot colours per category — matches the Billing panel's accent palette. */
const CATEGORY_TONES: Record<EventCategory, { bubble: string; dot: string }> = {
  registration: { bubble: 'bg-emerald-100 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400', dot: 'bg-emerald-500' },
  visit: { bubble: 'bg-sky-100 text-sky-600 dark:bg-sky-500/15 dark:text-sky-400', dot: 'bg-sky-500' },
  consultation: { bubble: 'bg-green-100 text-green-600 dark:bg-green-500/15 dark:text-green-400', dot: 'bg-green-500' },
  billing: { bubble: 'bg-orange-100 text-orange-500 dark:bg-orange-500/15 dark:text-orange-400', dot: 'bg-orange-400' },
  lab: { bubble: 'bg-violet-100 text-violet-600 dark:bg-violet-500/15 dark:text-violet-400', dot: 'bg-violet-500' },
  document: { bubble: 'bg-slate-100 text-slate-600 dark:bg-slate-500/15 dark:text-slate-300', dot: 'bg-slate-400' },
};

/** Where clicking an event goes — another page, or another tab of this same Patient Details. */
type EventTarget = { kind: 'route'; to: string } | { kind: 'tab'; tab: string };

interface TimelineEvent {
  key: string;
  at: string;
  category: EventCategory;
  icon: ElementType;
  title: string;
  detail?: string;
  target?: EventTarget;
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
}

function formatDay(iso: string) {
  return new Date(iso).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
}

interface PatientTimelineTabProps {
  patient: Patient;
  /** Switches Patient Details' own tab — used by events whose natural home is another tab
   * (visits, documents, overview) rather than another page. Omit to leave those unlinked. */
  onNavigateToTab?: (tab: string) => void;
  className?: string;
}

/**
 * Patient Details' Timeline — a newest-first merge of everything already recorded for this
 * patient in the other modules (registration, visits, consultation notes, invoices, lab orders
 * and released reports, documents). Nothing here is stored separately: each source is the same
 * query the other tabs use, so the timeline can't drift from them. Sources the viewer has no
 * permission for are simply left out, and an event only links somewhere the viewer can open.
 * Rendered on its own tab and alongside the Billing panel on the Billing tab.
 */
export function PatientTimelineTab({ patient, onNavigateToTab, className }: PatientTimelineTabProps) {
  const { hasPermission } = useAuth();
  const canViewClinical = hasPermission('clinical-care.view');
  const canViewLab = hasPermission('diagnostics.view');
  const canViewBilling = hasPermission('finance-billing.view');

  const visitsQuery = usePatientVisitsQuery(patient.id);
  const invoicesQuery = usePatientInvoicesQuery(patient.id);
  const documentsQuery = usePatientDocumentsQuery(patient.id);
  const consultationsQuery = useOpdConsultationsByPatientQuery(patient.id, canViewClinical);
  const labOrdersQuery = useLabOrdersByPatientQuery(canViewLab ? patient.id : undefined);
  // Primes resolveRecordLabel's cache for department/consultant names on visit rows.
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');

  const [category, setCategory] = useState<'all' | EventCategory>('all');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const isLoading = visitsQuery.isPending || invoicesQuery.isPending || documentsQuery.isPending;

  const events = useMemo<TimelineEvent[]>(() => {
    const tab = (name: string): EventTarget | undefined => (onNavigateToTab ? { kind: 'tab', tab: name } : undefined);
    const list: TimelineEvent[] = [
      { key: 'registered', at: patient.createdAt, category: 'registration', icon: UserPlus, title: 'Patient registered', detail: patient.uhid, target: tab('overview') },
    ];

    for (const visit of visitsQuery.data ?? []) {
      const doctors = visit.consultations
        .map((c) => `${resolveRecordLabel('consultant', c.consultantId)} (${resolveRecordLabel('department', c.departmentId)})`)
        .join(', ');
      list.push({
        key: `visit-${visit.visitId}`,
        at: visit.createdAt,
        category: 'visit',
        icon: ClipboardList,
        title: `${visit.visitType} visit`,
        detail: doctors || undefined,
        target: tab('visits'),
      });
    }

    for (const consultation of consultationsQuery.data ?? []) {
      const { header, note } = consultation;
      list.push({
        key: `consultation-${header.consultationId}`,
        at: note.updatedAt ?? note.createdAt,
        category: 'consultation',
        icon: Stethoscope,
        title: note.status === 'Completed' ? 'Consultation completed' : 'Consultation in progress',
        detail: header.consultantName,
        target: { kind: 'route', to: `/clinical/opd/consultations/${header.consultationId}` },
      });
    }

    for (const invoice of invoicesQuery.data ?? []) {
      const target: EventTarget | undefined = canViewBilling ? { kind: 'route', to: `/finance/accounts/${invoice.id}` } : undefined;
      const number = invoice.invoiceNumber ?? '';
      list.push({
        key: `invoice-${invoice.id}`,
        at: invoice.createdAt,
        category: 'billing',
        icon: Receipt,
        title: `Invoice ${number} created`.replace(/\s+/g, ' ').trim(),
        detail: formatCurrency(invoice.netAmount),
        target,
      });
      if (invoice.isVoided && invoice.voidedAt) {
        list.push({
          key: `invoice-void-${invoice.id}`,
          at: invoice.voidedAt,
          category: 'billing',
          icon: Receipt,
          title: `Invoice ${number} voided`.replace(/\s+/g, ' ').trim(),
          detail: invoice.voidReason,
          target,
        });
      }
    }

    for (const order of labOrdersQuery.data ?? []) {
      const tests = order.items.map((item) => item.testName).join(', ');
      const target: EventTarget = { kind: 'route', to: `/diagnostics/lab/orders/${order.id}` };
      list.push({ key: `lab-${order.id}`, at: order.createdAt, category: 'lab', icon: FlaskConical, title: `Lab order ${order.labOrderNumber}`, detail: tests || undefined, target });
      if (order.reportReleasedAt) {
        list.push({
          key: `lab-report-${order.id}`,
          at: order.reportReleasedAt,
          category: 'lab',
          icon: FlaskConical,
          title: `Lab report released — ${order.labOrderNumber}`,
          detail: tests || undefined,
          target,
        });
      }
    }

    for (const document of documentsQuery.data ?? []) {
      list.push({
        key: `document-${document.id}`,
        at: document.createdAt,
        category: 'document',
        icon: FileText,
        title: 'Document uploaded',
        detail: document.originalFileName,
        target: tab('documents'),
      });
    }

    return list.filter((event) => Boolean(event.at)).sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
  }, [
    patient.createdAt,
    patient.uhid,
    visitsQuery.data,
    consultationsQuery.data,
    invoicesQuery.data,
    labOrdersQuery.data,
    documentsQuery.data,
    canViewBilling,
    onNavigateToTab,
  ]);

  const visibleEvents = events.filter((event) => (category === 'all' || event.category === category) && inDateRange(event.at, from, to));
  const presentCategories = (Object.keys(CATEGORY_LABELS) as EventCategory[]).filter((c) => events.some((event) => event.category === c));

  return (
    <section className={cn('flex min-w-0 flex-col gap-3 rounded-xl border border-border bg-card p-4 shadow-sm', className)}>
      <header className="flex flex-wrap items-center gap-2">
        <h2 className="mr-auto flex items-center gap-2 text-lg font-semibold text-foreground">
          <History className="h-5 w-5 text-primary" />
          Timeline
        </h2>
        <Select value={category} onValueChange={(value) => setCategory(value as 'all' | EventCategory)}>
          <SelectTrigger className="h-9 w-36" aria-label="Filter timeline events">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Events</SelectItem>
            {presentCategories.map((c) => (
              <SelectItem key={c} value={c}>
                {CATEGORY_LABELS[c]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex h-9 items-center gap-1.5 rounded-md border border-input bg-background px-2 text-sm">
          <CalendarDays className="h-4 w-4 shrink-0 text-muted-foreground" />
          <input type="date" aria-label="Timeline from date" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} className="bg-transparent outline-none" />
          <span className="text-muted-foreground">→</span>
          <input type="date" aria-label="Timeline to date" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} className="bg-transparent outline-none" />
        </div>
      </header>

      {isLoading ? (
        <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading timeline…
        </div>
      ) : visibleEvents.length === 0 ? (
        <p className="py-10 text-center text-sm text-muted-foreground">No timeline events match the current filters.</p>
      ) : (
        <ol className="flex flex-col gap-2.5">
          {visibleEvents.map((event, index) => (
            <TimelineRow key={event.key} event={event} isLast={index === visibleEvents.length - 1} onNavigateToTab={onNavigateToTab} />
          ))}
        </ol>
      )}
    </section>
  );
}

function TimelineRow({ event, isLast, onNavigateToTab }: { event: TimelineEvent; isLast: boolean; onNavigateToTab?: (tab: string) => void }) {
  const Icon = event.icon;
  const tone = CATEGORY_TONES[event.category];
  const cardClass =
    'flex min-w-0 flex-1 items-start justify-between gap-3 rounded-lg border border-border bg-background px-3 py-2.5 text-left transition-colors';
  const body = (
    <>
      <span className="min-w-0">
        <span className="block text-sm font-medium text-foreground">{event.title}</span>
        {event.detail && <span className="block truncate text-sm text-muted-foreground">{event.detail}</span>}
      </span>
      <span className="flex shrink-0 items-center gap-1.5">
        <span className="flex flex-col items-end text-xs text-muted-foreground">
          <span className="text-foreground">{formatTime(event.at)}</span>
          <span>{formatDay(event.at)}</span>
        </span>
        {event.target && <ChevronRight className="h-4 w-4 text-muted-foreground" />}
      </span>
    </>
  );

  let card = <div className={cardClass}>{body}</div>;
  if (event.target?.kind === 'route') {
    card = (
      <Link to={event.target.to} className={cn(cardClass, 'hover:border-primary/40 hover:bg-accent/40')}>
        {body}
      </Link>
    );
  } else if (event.target?.kind === 'tab' && onNavigateToTab) {
    const tab = event.target.tab;
    card = (
      <button type="button" onClick={() => onNavigateToTab(tab)} className={cn(cardClass, 'hover:border-primary/40 hover:bg-accent/40')}>
        {body}
      </button>
    );
  }

  return (
    <li className="flex items-stretch gap-3">
      <span className="flex h-11 w-11 shrink-0 items-center justify-center self-center rounded-full">
        <span className={cn('flex h-9 w-9 items-center justify-center rounded-full', tone.bubble)}>
          <Icon className="h-4 w-4" />
        </span>
      </span>
      <span className="relative flex w-3 shrink-0 justify-center">
        <span className={cn('absolute left-1/2 w-px -translate-x-1/2 bg-border', isLast ? 'top-0 h-1/2' : '-bottom-2.5 top-0')} />
        <span className={cn('relative z-10 mt-auto mb-auto h-2 w-2 rounded-full', tone.dot)} />
      </span>
      {card}
    </li>
  );
}
