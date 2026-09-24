import type { Patient } from '@hms/shared';
import { ClipboardList, FileText, FlaskConical, Loader2, Receipt, Stethoscope, UserPlus } from 'lucide-react';
import { useMemo, type ElementType } from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import { formatCurrency, usePatientInvoicesQuery } from '@/features/billing';
import { useLabOrdersByPatientQuery } from '@/features/laboratory';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { useOpdConsultationsByPatientQuery } from '@/features/opdConsultation';
import { usePatientDocumentsQuery } from '../hooks/usePatientDocumentsQuery';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';

interface TimelineEvent {
  key: string;
  at: string;
  icon: ElementType;
  title: string;
  detail?: string;
}

function formatWhen(iso: string) {
  return new Date(iso).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

/**
 * Patient Details' Timeline — a newest-first merge of everything already recorded for this
 * patient in the other modules (registration, visits, consultation notes, invoices, lab orders
 * and released reports, documents). It used to be a hard-coded "No timeline activity" empty
 * state regardless of the patient's history (regression report PAT-01). Nothing here is stored
 * separately: each source is the same query the other tabs use, so the timeline can't drift
 * from them. Sources the viewer has no permission for are simply left out.
 */
export function PatientTimelineTab({ patient }: { patient: Patient }) {
  const { hasPermission } = useAuth();
  const canViewClinical = hasPermission('clinical-care.view');
  const canViewLab = hasPermission('diagnostics.view');

  const visitsQuery = usePatientVisitsQuery(patient.id);
  const invoicesQuery = usePatientInvoicesQuery(patient.id);
  const documentsQuery = usePatientDocumentsQuery(patient.id);
  const consultationsQuery = useOpdConsultationsByPatientQuery(patient.id, canViewClinical);
  const labOrdersQuery = useLabOrdersByPatientQuery(canViewLab ? patient.id : undefined);
  // Primes resolveRecordLabel's cache for department/consultant names on visit rows.
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');

  const isLoading = visitsQuery.isPending || invoicesQuery.isPending || documentsQuery.isPending;

  const events = useMemo<TimelineEvent[]>(() => {
    const list: TimelineEvent[] = [
      { key: 'registered', at: patient.createdAt, icon: UserPlus, title: 'Patient registered', detail: patient.uhid },
    ];

    for (const visit of visitsQuery.data ?? []) {
      const doctors = visit.consultations
        .map((c) => `${resolveRecordLabel('consultant', c.consultantId)} (${resolveRecordLabel('department', c.departmentId)})`)
        .join(', ');
      list.push({ key: `visit-${visit.visitId}`, at: visit.createdAt, icon: ClipboardList, title: `${visit.visitType} visit`, detail: doctors || undefined });
    }

    for (const consultation of consultationsQuery.data ?? []) {
      const { header, note } = consultation;
      list.push({
        key: `consultation-${header.consultationId}`,
        at: note.updatedAt ?? note.createdAt,
        icon: Stethoscope,
        title: note.status === 'Completed' ? 'Consultation completed' : 'Consultation in progress',
        detail: header.consultantName,
      });
    }

    for (const invoice of invoicesQuery.data ?? []) {
      list.push({
        key: `invoice-${invoice.id}`,
        at: invoice.createdAt,
        icon: Receipt,
        title: `Invoice ${invoice.invoiceNumber ?? ''} created`.replace(/\s+/g, ' ').trim(),
        detail: formatCurrency(invoice.netAmount),
      });
      if (invoice.isVoided && invoice.voidedAt) {
        list.push({ key: `invoice-void-${invoice.id}`, at: invoice.voidedAt, icon: Receipt, title: `Invoice ${invoice.invoiceNumber ?? ''} voided`, detail: invoice.voidReason });
      }
    }

    for (const order of labOrdersQuery.data ?? []) {
      const tests = order.items.map((item) => item.testName).join(', ');
      list.push({ key: `lab-${order.id}`, at: order.createdAt, icon: FlaskConical, title: `Lab order ${order.labOrderNumber}`, detail: tests || undefined });
      if (order.reportReleasedAt) {
        list.push({ key: `lab-report-${order.id}`, at: order.reportReleasedAt, icon: FlaskConical, title: `Lab report released — ${order.labOrderNumber}`, detail: tests || undefined });
      }
    }

    for (const document of documentsQuery.data ?? []) {
      list.push({ key: `document-${document.id}`, at: document.createdAt, icon: FileText, title: 'Document uploaded', detail: document.originalFileName });
    }

    return list.filter((event) => Boolean(event.at)).sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
  }, [patient.createdAt, patient.uhid, visitsQuery.data, consultationsQuery.data, invoicesQuery.data, labOrdersQuery.data, documentsQuery.data]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading timeline…
      </div>
    );
  }

  return (
    <ol className="relative flex flex-col gap-4 border-l border-border pl-6">
      {events.map((event) => {
        const Icon = event.icon;
        return (
          <li key={event.key} className="relative">
            <span className="absolute -left-[33px] flex h-6 w-6 items-center justify-center rounded-full border border-border bg-background">
              <Icon className="h-3.5 w-3.5 text-primary" />
            </span>
            <p className="text-sm font-medium text-foreground">{event.title}</p>
            {event.detail && <p className="text-sm text-muted-foreground">{event.detail}</p>}
            <p className="text-xs text-muted-foreground">{formatWhen(event.at)}</p>
          </li>
        );
      })}
    </ol>
  );
}
