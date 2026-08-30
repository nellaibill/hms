import { ALLERGY_TYPES, ALLERGY_SEVERITIES, type Allergy, type AllergySeverity, type AllergyType, type Patient } from '@hms/shared';
import {
  CalendarClock,
  ClipboardList,
  Download,
  Eye,
  FileText,
  HeartPulse,
  Loader2,
  MapPin,
  Plus,
  Stethoscope,
  User,
  X,
} from 'lucide-react';
import { type ReactNode, useState } from 'react';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { ConsultantName } from '@/components/ConsultantName';
import { DepartmentName } from '@/components/DepartmentName';
import { DistrictName } from '@/components/DistrictName';
import { StateName } from '@/components/StateName';
import { describeBillingItem, formatCurrency, usePatientInvoicesQuery, type BillingItem } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { documentsApi } from '../../../services/apiClient';
import { useAuth } from '../../auth/AuthContext';
import { humanize } from '../humanize';
import { maritalStatusLabel } from '../maritalStatusLabel';
import { maskIdNumber } from '../maskIdNumber';
import { useAddPatientAllergyMutation, useRemovePatientAllergyMutation } from '../hooks/usePatientMutations';
import { usePatientDocumentsQuery } from '../hooks/usePatientDocumentsQuery';
import { usePatientDocumentUrl } from '../hooks/usePatientDocumentUrl';
import { usePatientVisitsQuery } from '../hooks/usePatientVisitsQuery';
import { PatientDocumentUpload } from './PatientDocumentUpload';
import type { DocumentResponse, PatientVisit, VisitConsultation } from '@hms/shared';

interface PatientDetailsProps {
  patient: Patient;
  activeTab: string;
  onActiveTabChange: (value: string) => void;
}

function Field({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-0.5">
      <dt className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

/** The compact bordered card every Overview section is built from. */
function SectionCard({
  title,
  icon: Icon,
  action,
  centerTitle,
  children,
}: {
  title: string;
  icon: React.ElementType;
  action?: ReactNode;
  /** Centers the title within the header row instead of the default left alignment — used by
   * the Overview tab's top row of cards (Personal & Contact, Address & Emergency, Registration
   * Details, Allergy Details). These cards are narrow (4 to a row), so an action button (e.g.
   * Allergy Details' "+ Add Allergy") can't sit inline beside a truly centered title without
   * overlapping it — it's stacked on its own centered row underneath instead. */
  centerTitle?: boolean;
  children: ReactNode;
}) {
  const heading = (
    <h2 className="flex items-center gap-1.5 text-sm font-semibold text-foreground">
      <Icon className="h-4 w-4 text-primary" />
      {title}
    </h2>
  );

  return (
    <div className="flex flex-col gap-1.5 rounded-lg border border-border bg-card p-3">
      {centerTitle ? (
        <div className="flex flex-col items-center gap-1.5">
          {heading}
          {action}
        </div>
      ) : (
        <div className="flex items-center justify-between gap-2">
          {heading}
          {action}
        </div>
      )}
      {children}
    </div>
  );
}

function EmptyState({ icon: Icon, message }: { icon: React.ElementType; message: string }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
      <Icon className="h-6 w-6" />
      {message}
    </div>
  );
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function severityBadgeVariant(severity: AllergySeverity): BadgeProps['variant'] {
  if (severity === 'Severe') return 'destructive';
  if (severity === 'Moderate') return 'warning';
  return 'secondary';
}

function documentStatusBadgeVariant(status: DocumentResponse['status']): BadgeProps['variant'] {
  if (status === 'Available') return 'success';
  if (status === 'Pending') return 'warning';
  return 'destructive';
}

/* ---------------------------------------------------------------------- Personal & Contact */

function PersonalContactCard({ patient }: { patient: Patient }) {
  return (
    <SectionCard title="Personal & Contact" icon={User} centerTitle>
      <dl className="grid grid-cols-2 gap-x-3">
        <Field label="Title" value={patient.title} />
        <Field label="First name" value={patient.firstName} />
        <Field label="Last name" value={patient.lastName} />
        <Field label="Marital status" value={maritalStatusLabel(patient.maritalStatus)} />
        <Field label="Profession" value={patient.profession || '—'} />
        <Field label="Email" value={patient.email || '—'} />
        <Field label="Secondary phone" value={patient.secondaryPhone || '—'} />
      </dl>
    </SectionCard>
  );
}

/* ------------------------------------------------------------------- Address & Emergency */

function AddressEmergencyCard({ patient }: { patient: Patient }) {
  const [showAll, setShowAll] = useState(false);
  const visibleContacts = showAll ? patient.emergencyContacts : patient.emergencyContacts.slice(0, 2);

  return (
    <SectionCard title="Address & Emergency" icon={MapPin} centerTitle>
      <div className="text-sm text-foreground">
        <p>{[patient.address.addressLine1, patient.address.addressLine2, patient.address.addressLine3].filter(Boolean).join(', ')}</p>
        <p className="text-muted-foreground">
          <DistrictName stateId={patient.address.stateId} districtId={patient.address.districtId} />, <StateName stateId={patient.address.stateId} /> —{' '}
          {patient.address.pincode}
        </p>
      </div>

      <div className="mt-1 flex flex-col gap-1 border-t border-border pt-2">
        <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Emergency contacts</span>
        {patient.emergencyContacts.length === 0 ? (
          <span className="text-sm text-muted-foreground">—</span>
        ) : (
          <>
            {visibleContacts.map((contact) => (
              <div key={contact.id} className="flex items-center justify-between text-sm">
                <span className="text-foreground">{contact.name}</span>
                <span className="text-muted-foreground">
                  {humanize(contact.relationship)} · {contact.phone}
                </span>
              </div>
            ))}
            {patient.emergencyContacts.length > 2 && (
              <button
                type="button"
                onClick={() => setShowAll((prev) => !prev)}
                className="w-fit text-xs font-medium text-primary hover:underline"
              >
                {showAll ? 'Show less' : `View all (${patient.emergencyContacts.length})`}
              </button>
            )}
          </>
        )}
      </div>
    </SectionCard>
  );
}

/* ----------------------------------------------------------------- Registration Details */

function RegistrationDetailsCard({ patient }: { patient: Patient }) {
  return (
    <SectionCard title="Registration Details" icon={ClipboardList} centerTitle>
      <dl className="grid grid-cols-2 gap-x-3">
        <Field label="Registration source" value={humanize(patient.modeOfArrivalSource)} />
        {patient.modeOfArrivalChannel && <Field label="Arrival channel" value={humanize(patient.modeOfArrivalChannel)} />}
        {patient.modeOfArrivalSpecify && <Field label="Arrival details" value={patient.modeOfArrivalSpecify} />}
        <Field label="ID proof type" value={patient.idProofType ?? '—'} />
        <Field label="ID proof number" value={patient.idProofNumber ? maskIdNumber(patient.idProofNumber) : '—'} />
      </dl>
    </SectionCard>
  );
}

/* --------------------------------------------------------------------- Allergy Details */

function AddAllergyForm({ patientId, onDone }: { patientId: string; onDone: () => void }) {
  const addAllergyMutation = useAddPatientAllergyMutation();
  const [draft, setDraft] = useState<{ allergyType: AllergyType | ''; specify: string; severity: AllergySeverity | '' }>({
    allergyType: '',
    specify: '',
    severity: '',
  });
  const [error, setError] = useState<string | null>(null);

  function handleAdd() {
    if (!draft.allergyType || !draft.severity) {
      setError('Type and severity are required.');
      return;
    }
    setError(null);
    addAllergyMutation.mutate(
      { id: patientId, request: { allergyType: draft.allergyType, specify: draft.specify.trim() || undefined, severity: draft.severity } },
      { onSuccess: onDone },
    );
  }

  return (
    <div className="flex flex-col gap-2 rounded-md border border-dashed border-border p-2.5">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
        <Select value={draft.allergyType || undefined} onValueChange={(value) => setDraft((prev) => ({ ...prev, allergyType: value as AllergyType }))}>
          <SelectTrigger aria-label="Allergy type" className="h-8 text-xs">
            <SelectValue placeholder="Type" />
          </SelectTrigger>
          <SelectContent>
            {ALLERGY_TYPES.map((type) => (
              <SelectItem key={type} value={type}>
                {type}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          placeholder="Specify (e.g. Penicillin)"
          value={draft.specify}
          onChange={(event) => setDraft((prev) => ({ ...prev, specify: event.target.value }))}
          className="h-8 text-xs"
        />
        <Select value={draft.severity || undefined} onValueChange={(value) => setDraft((prev) => ({ ...prev, severity: value as AllergySeverity }))}>
          <SelectTrigger aria-label="Allergy severity" className="h-8 text-xs">
            <SelectValue placeholder="Severity" />
          </SelectTrigger>
          <SelectContent>
            {ALLERGY_SEVERITIES.map((severity) => (
              <SelectItem key={severity} value={severity}>
                {severity}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
      {addAllergyMutation.isError && <p className="text-xs text-destructive">Failed to add allergy — please try again.</p>}
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onDone}>
          Cancel
        </Button>
        <Button type="button" size="sm" onClick={handleAdd} disabled={addAllergyMutation.isPending}>
          {addAllergyMutation.isPending ? 'Adding…' : 'Add'}
        </Button>
      </div>
    </div>
  );
}

function AllergyRow({ allergy, patientId, canEdit }: { allergy: Allergy; patientId: string; canEdit: boolean }) {
  const removeAllergyMutation = useRemovePatientAllergyMutation();

  return (
    <div className="flex items-start justify-between gap-2 rounded-md border border-border/70 bg-accent/30 px-2.5 py-2">
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">{allergy.specify || allergy.allergyType}</p>
        <p className="text-xs text-muted-foreground">{allergy.allergyType}</p>
      </div>
      <div className="flex shrink-0 items-center gap-1.5">
        <Badge variant={severityBadgeVariant(allergy.severity)} className="text-[10px]">
          {allergy.severity}
        </Badge>
        {canEdit && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            aria-label={`Remove ${allergy.allergyType} allergy`}
            disabled={removeAllergyMutation.isPending}
            onClick={() => removeAllergyMutation.mutate({ id: patientId, allergyId: allergy.id })}
          >
            <X className="h-3.5 w-3.5" />
          </Button>
        )}
      </div>
    </div>
  );
}

function AllergyDetailsCard({ patient }: { patient: Patient }) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('patient-management.edit');
  const [showAddForm, setShowAddForm] = useState(false);

  return (
    <SectionCard
      title="Allergy Details"
      icon={HeartPulse}
      centerTitle
      action={
        canEdit &&
        !showAddForm && (
          <Button type="button" variant="ghost" size="sm" className="h-7 gap-1 px-2 text-xs" onClick={() => setShowAddForm(true)}>
            <Plus className="h-3.5 w-3.5" />
            Add Allergy
          </Button>
        )
      }
    >
      <div className="flex flex-col gap-1.5">
        {patient.allergies.length === 0 && !showAddForm && <p className="text-sm text-muted-foreground">No known allergies recorded.</p>}
        {patient.allergies.map((allergy) => (
          <AllergyRow key={allergy.id} allergy={allergy} patientId={patient.id} canEdit={canEdit} />
        ))}
        {showAddForm && <AddAllergyForm patientId={patient.id} onDone={() => setShowAddForm(false)} />}
      </div>

      <div className="mt-1 flex flex-col gap-1 border-t border-border pt-2">
        <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Important medical information</span>
        <p className="text-sm text-muted-foreground">No additional medical notes have been recorded for this patient.</p>
      </div>
    </SectionCard>
  );
}

/* ------------------------------------------------------------------------ At a Glance */

function StatItem({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-0.5 px-2 py-1.5 text-center">
      <span className="text-base font-semibold tabular-nums text-foreground">{value}</span>
      <span className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</span>
    </div>
  );
}

const VISIT_STAT_TYPES = ['OP', 'IP', 'Emergency', 'DayCare'] as const;
const VISIT_STAT_LABELS: Record<(typeof VISIT_STAT_TYPES)[number], string> = {
  OP: 'OP Visits',
  IP: 'IP Admissions',
  Emergency: 'Emergency',
  DayCare: 'Day Care',
};

function AtAGlanceStrip({ patient }: { patient: Patient }) {
  const { data: visits, isPending: visitsPending } = usePatientVisitsQuery(patient.id);
  const { data: documents, isPending: documentsPending } = usePatientDocumentsQuery(patient.id);
  const { data: billings, isPending: billingsPending } = usePatientInvoicesQuery(patient.id);

  const dash = <Loader2 className="mx-auto h-3.5 w-3.5 animate-spin text-muted-foreground" />;

  const visitCountByType = (type: (typeof VISIT_STAT_TYPES)[number]) => visits?.filter((v) => v.visitType === type).length ?? 0;
  const lastVisit = visits?.[0];
  const prescriptionCount = documents?.filter((doc) => doc.documentType === 'Prescription').length ?? 0;
  const totalBills = billings?.reduce((sum, billing) => sum + billing.netAmount, 0) ?? 0;
  const outstanding =
    billings?.reduce((sum, billing) => sum + billing.items.filter((item) => item.paymentStatus === 'Pending').reduce((s, item) => s + item.total, 0), 0) ?? 0;

  return (
    <div className="flex flex-wrap items-stretch divide-x divide-border rounded-lg border border-border bg-card">
      <StatItem label="Total Visits" value={visitsPending ? dash : (visits?.length ?? 0)} />
      {VISIT_STAT_TYPES.map((type) => (
        <StatItem key={type} label={VISIT_STAT_LABELS[type]} value={visitsPending ? dash : visitCountByType(type)} />
      ))}
      <StatItem
        label="Last Visit"
        value={visitsPending ? dash : lastVisit ? new Date(lastVisit.createdAt).toLocaleDateString('en-IN') : '—'}
      />
      <StatItem label="Total Prescriptions" value={documentsPending ? dash : prescriptionCount} />
      <StatItem label="Total Bills" value={billingsPending ? dash : formatCurrency(totalBills)} />
      <StatItem label="Outstanding" value={billingsPending ? dash : formatCurrency(outstanding)} />
    </div>
  );
}

/* ------------------------------------------------------------------------ Recent Visits */

/** The primary consultant is shown inline; any additional ones (a visit can have more than one —
 * e.g. a referring and a consulting doctor) used to collapse into a dead-end "+N" label with no
 * way to see who they were. The badge is now a tooltip trigger that lists every extra consultant
 * by name, resolved the same way the primary one is. */
function ConsultantsCell({ consultations }: { consultations: VisitConsultation[] }) {
  const [primary, ...rest] = consultations;
  if (!primary) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <span className="inline-flex items-center gap-1.5">
      <ConsultantName consultantId={primary.consultantId} />
      {rest.length > 0 && (
        <Tooltip>
          <TooltipTrigger asChild>
            <button
              type="button"
              className="rounded-full bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground hover:bg-accent hover:text-accent-foreground"
            >
              +{rest.length}
            </button>
          </TooltipTrigger>
          <TooltipContent>
            <ul className="flex flex-col gap-0.5">
              {rest.map((consultation) => (
                <li key={consultation.consultantId}>
                  <ConsultantName consultantId={consultation.consultantId} />
                </li>
              ))}
            </ul>
          </TooltipContent>
        </Tooltip>
      )}
    </span>
  );
}

function VisitsTable({ visits, limit }: { visits: PatientVisit[]; limit?: number }) {
  const rows = limit ? visits.slice(0, limit) : visits;

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/60 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-3 py-2">Date</th>
            <th className="px-3 py-2">Visit Type</th>
            <th className="px-3 py-2">Department</th>
            <th className="px-3 py-2">Consultant</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((visit) => {
            const primary = visit.consultations[0];
            return (
              <tr key={visit.visitId} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-3 py-2 text-muted-foreground">{new Date(visit.createdAt).toLocaleDateString('en-IN')}</td>
                <td className="px-3 py-2">
                  <Badge variant="outline" className="text-[10px]">
                    {visit.visitType}
                  </Badge>
                </td>
                <td className="px-3 py-2 text-foreground">{primary ? <DepartmentName departmentId={primary.departmentId} /> : '—'}</td>
                <td className="px-3 py-2 text-foreground">
                  <ConsultantsCell consultations={visit.consultations} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function RecentVisitsCard({ patient, onViewAll }: { patient: Patient; onViewAll: () => void }) {
  const { data: visits, isPending } = usePatientVisitsQuery(patient.id);

  return (
    <SectionCard
      title="Recent Visits"
      icon={Stethoscope}
      action={
        visits &&
        visits.length > 0 && (
          <button type="button" onClick={onViewAll} className="text-xs font-medium text-primary hover:underline">
            View All Visits
          </button>
        )
      }
    >
      {isPending ? (
        <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading visits…
        </div>
      ) : !visits || visits.length === 0 ? (
        <p className="py-2 text-sm text-muted-foreground">No visits recorded for this patient yet.</p>
      ) : (
        <VisitsTable visits={visits} limit={4} />
      )}
    </SectionCard>
  );
}

/* --------------------------------------------------------------------- Recent Documents */

async function openDocument(document: DocumentResponse, mode: 'view' | 'download') {
  const blob = await documentsApi.getDocumentContent(document.id);
  const url = URL.createObjectURL(blob);
  if (mode === 'view') {
    window.open(url, '_blank', 'noopener,noreferrer');
    return;
  }
  const link = window.document.createElement('a');
  link.href = url;
  link.download = document.originalFileName;
  link.click();
  URL.revokeObjectURL(url);
}

function DocumentRow({ document }: { document: DocumentResponse }) {
  return (
    <div className="flex items-center justify-between gap-2 py-1.5 text-sm">
      <div className="min-w-0">
        <p className="truncate font-medium text-foreground">{document.originalFileName}</p>
        <p className="text-xs text-muted-foreground">
          {humanize(document.documentType)} · {formatFileSize(document.sizeBytes)}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-1.5">
        <Badge variant={documentStatusBadgeVariant(document.status)} className="text-[10px]">
          {document.status}
        </Badge>
        {document.status === 'Available' && (
          <>
            <Button type="button" variant="ghost" size="icon" className="h-7 w-7" aria-label={`View ${document.originalFileName}`} onClick={() => openDocument(document, 'view')}>
              <Eye className="h-3.5 w-3.5" />
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              aria-label={`Download ${document.originalFileName}`}
              onClick={() => openDocument(document, 'download')}
            >
              <Download className="h-3.5 w-3.5" />
            </Button>
          </>
        )}
      </div>
    </div>
  );
}

function RecentDocumentsCard({ patient, onViewAll }: { patient: Patient; onViewAll: () => void }) {
  const { data: documents, isPending } = usePatientDocumentsQuery(patient.id);
  const recent = [...(documents ?? [])].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  return (
    <SectionCard
      title="Recent Documents"
      icon={FileText}
      action={
        documents &&
        documents.length > 0 && (
          <button type="button" onClick={onViewAll} className="text-xs font-medium text-primary hover:underline">
            View All Documents ({documents.length})
          </button>
        )
      }
    >
      {isPending ? (
        <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading documents…
        </div>
      ) : recent.length === 0 ? (
        <p className="py-2 text-sm text-muted-foreground">No documents uploaded for this patient yet.</p>
      ) : (
        <div className="flex flex-col divide-y divide-border">
          {recent.slice(0, 4).map((document) => (
            <DocumentRow key={document.id} document={document} />
          ))}
        </div>
      )}
    </SectionCard>
  );
}

/* ------------------------------------------------------------------------------ Overview */

function OverviewTab({ patient, onNavigateToTab }: { patient: Patient; onNavigateToTab: (tab: string) => void }) {
  return (
    <div className="flex flex-col gap-2.5">
      <div className="grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-4">
        <PersonalContactCard patient={patient} />
        <AddressEmergencyCard patient={patient} />
        <RegistrationDetailsCard patient={patient} />
        <AllergyDetailsCard patient={patient} />
      </div>

      <AtAGlanceStrip patient={patient} />

      <div className="grid grid-cols-1 gap-2.5 lg:grid-cols-2">
        <RecentVisitsCard patient={patient} onViewAll={() => onNavigateToTab('visits')} />
        <RecentDocumentsCard patient={patient} onViewAll={() => onNavigateToTab('documents')} />
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------------- Visits */

function PatientVisitsTab({ patient }: { patient: Patient }) {
  const { data: visits, isPending } = usePatientVisitsQuery(patient.id);

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading visits…
      </div>
    );
  }

  if (!visits || visits.length === 0) {
    return <EmptyState icon={CalendarClock} message="No visits have been recorded for this patient yet." />;
  }

  return <VisitsTable visits={visits} />;
}

/* ---------------------------------------------------------------------------- Billing */

function BillingLineItem({ item }: { item: BillingItem }) {
  const { serviceLabel, consultantName } = describeBillingItem(item);
  return (
    <div className="flex items-start justify-between gap-3 py-2.5 text-sm">
      <div className="min-w-0">
        <p className="font-medium text-foreground">
          {item.billingType} — {serviceLabel}
        </p>
        <p className="truncate text-xs text-muted-foreground">
          {consultantName}
          {item.discount > 0 ? ` · Discount ${formatCurrency(item.discount)}` : ''}
        </p>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-1">
        <span className="font-medium tabular-nums text-foreground">{formatCurrency(item.total)}</span>
        <Badge variant={item.paymentStatus === 'Paid' ? 'success' : 'warning'} className="text-[10px]">
          {item.paymentStatus}
        </Badge>
      </div>
    </div>
  );
}

/** Billing is its own bounded context (see features/billing) — this reads it read-only for display via the real Billing API, the same way it reads DocumentUpload's storage elsewhere. A patient can have zero billing records (every category is optional at registration) — that's shown explicitly rather than hiding the section, so "no charges were entered" reads as a fact, not a missing feature. */
function PatientBillingTab({ patientId }: { patientId: string }) {
  const { data: billings, isPending } = usePatientInvoicesQuery(patientId);
  // Primes the Masters reference cache describeBillingItem reads from in BillingLineItem
  // below, so every line item resolves to its real name instead of a raw id.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  // Radiology/Laboratory now read the new typed DiagnosticService/DiagnosticPackage catalogs
  // (see billingCalculations.ts's describeBillingItem) — these prime that cache the same way.
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading billing…
      </div>
    );
  }

  if (!billings || billings.length === 0) {
    return <EmptyState icon={FileText} message="No billing recorded for this patient yet." />;
  }

  return (
    <div className="rounded-lg border border-border p-3">
      <div className="flex flex-col gap-3">
        {billings.map((billing) => (
          <div key={billing.id} className="flex flex-col divide-y divide-border">
            {billing.items.map((item) => (
              <BillingLineItem key={item.id} item={item} />
            ))}
            <div className="flex flex-wrap items-center justify-between gap-2 pt-2.5 text-sm">
              <span className="text-muted-foreground">
                Gross {formatCurrency(billing.grossAmount)}
                {billing.totalDiscount > 0 ? ` · Discount −${formatCurrency(billing.totalDiscount)}` : ''}
              </span>
              <span className="text-base font-semibold text-primary">Net {formatCurrency(billing.netAmount)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- Documents */

function PatientDocumentsTab({ patient }: { patient: Patient }) {
  const photoUrl = usePatientDocumentUrl(patient.id, 'Other');
  const idProofUrl = usePatientDocumentUrl(patient.id, 'IdProof');
  const { data: documents, isPending } = usePatientDocumentsQuery(patient.id);
  const sorted = [...(documents ?? [])].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  return (
    <div className="flex flex-col gap-2.5">
      <div className="grid grid-cols-1 gap-2.5 lg:grid-cols-2">
        <div className="flex flex-col gap-2.5 rounded-lg border border-border bg-card p-3">
          <h2 className="text-sm font-semibold text-foreground">On file</h2>
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Patient photo</span>
            {photoUrl ? (
              <img
                src={photoUrl}
                alt={`${patient.firstName} ${patient.lastName}`}
                className="mt-1 h-28 w-28 rounded-md border border-border object-cover"
              />
            ) : (
              <span className="text-sm text-muted-foreground">Not uploaded.</span>
            )}
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">ID proof</span>
            {idProofUrl ? (
              <a href={idProofUrl} target="_blank" rel="noreferrer" className="text-sm text-primary hover:underline">
                {patient.idProofType} on file — view
              </a>
            ) : (
              <span className="text-sm text-muted-foreground">Not uploaded.</span>
            )}
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-3">
          <h2 className="mb-3 text-sm font-semibold text-foreground">Upload new</h2>
          <PatientDocumentUpload patientId={patient.id} bare />
        </div>
      </div>

      <div className="rounded-lg border border-border bg-card p-3">
        <h2 className="mb-2 text-sm font-semibold text-foreground">All documents</h2>
        {isPending ? (
          <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading documents…
          </div>
        ) : sorted.length === 0 ? (
          <p className="py-2 text-sm text-muted-foreground">No documents uploaded for this patient yet.</p>
        ) : (
          <div className="flex flex-col divide-y divide-border">
            {sorted.map((document) => (
              <DocumentRow key={document.id} document={document} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------------------- Root */

export function PatientDetails({ patient, activeTab, onActiveTabChange }: PatientDetailsProps) {
  return (
    <Tabs value={activeTab} onValueChange={onActiveTabChange}>
      <TabsList>
        <TabsTrigger value="overview">Overview</TabsTrigger>
        <TabsTrigger value="visits">Visits</TabsTrigger>
        <TabsTrigger value="medical-information">Medical Information</TabsTrigger>
        <TabsTrigger value="documents">Documents</TabsTrigger>
        <TabsTrigger value="billing">Billing</TabsTrigger>
        <TabsTrigger value="timeline">Timeline</TabsTrigger>
      </TabsList>

      <TabsContent value="overview" className="pt-2.5">
        <OverviewTab patient={patient} onNavigateToTab={onActiveTabChange} />
      </TabsContent>

      <TabsContent value="visits" className="pt-2.5">
        <PatientVisitsTab patient={patient} />
      </TabsContent>

      <TabsContent value="medical-information" className="pt-2.5">
        <EmptyState
          icon={HeartPulse}
          message="Detailed medical history (diagnoses, medications, past procedures) isn't tracked in this system yet — see the Allergy Details card on the Overview tab for what is captured today."
        />
      </TabsContent>

      <TabsContent value="documents" className="pt-2.5">
        <PatientDocumentsTab patient={patient} />
      </TabsContent>

      <TabsContent value="billing" className="pt-2.5">
        <PatientBillingTab patientId={patient.id} />
      </TabsContent>

      <TabsContent value="timeline" className="pt-2.5">
        <EmptyState icon={ClipboardList} message="No timeline activity has been recorded for this patient yet." />
      </TabsContent>
    </Tabs>
  );
}
