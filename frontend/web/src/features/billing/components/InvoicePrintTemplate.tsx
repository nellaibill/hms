import { branding } from '@/config/branding';
import { useBrandLogo } from '@/features/branding/brandLogo';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { useAuth } from '@/features/auth/AuthContext';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { usePatientQuery } from '@/features/patients/hooks/usePatientQuery';
import { usePatientVisitsQuery } from '@/features/patients/hooks/usePatientVisitsQuery';
import { todayDateInputValue, toLocalDateInputValue } from '@/lib/reportDateRange';
import { describeBillingItem, formatCurrency } from '../billingCalculations';
import { useCollectionsQuery } from '../hooks/useCollectionsQuery';
import type { Billing } from '../types';
import { serverNow } from '@/lib/hospitalTime';

const PAYMENT_METHOD_LABELS: Record<string, string> = { Cash: 'Cash', Card: 'Card', Upi: 'UPI', BankTransfer: 'Bank Transfer' };

interface InvoicePrintTemplateProps {
  billing: Billing;
}

/**
 * The actual printed page — deliberately not a print stylesheet applied to InvoiceDetailCard
 * itself. That card is an on-screen UI panel (badges, action buttons, hover states, the app's
 * dark/light theme tokens); a receipt handed to a patient needs its own fixed, explicit layout
 * that reads as a real invoice regardless of what theme is active on screen, so this renders
 * completely separately and only becomes visible via the `.print-target` rule in index.css
 * (hidden on screen at all times, shown — and everything else hidden — once printing starts).
 * Explicit black/white/gray colors throughout, not the app's semantic CSS tokens, for the same
 * reason: a printed page shouldn't depend on whatever theme happened to be active.
 */
export function InvoicePrintTemplate({ billing }: InvoicePrintTemplateProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const { user } = useAuth();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  // The configured Print logo, else the Primary logo, else the bundled default.
  const printLogo = useBrandLogo('print');

  // Same reference-cache priming InvoiceDetailCard does — kept here too so this template
  // resolves real names even if it's ever rendered without that card mounted alongside it.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultationType');
  useMasterOptionsQuery('consultant');
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  // What a patient's receipt needs beyond the bill itself: who they are (age, gender, phone),
  // the visit's OP/IP No, and how the money was paid. The bill doesn't carry these, so they come
  // from the patient record, the visit list and the payments taken since the bill was made.
  const { data: patient } = usePatientQuery(billing.patientId);
  const { data: visits } = usePatientVisitsQuery(billing.patientId);
  const visitNumber = billing.opNumber ?? visits?.find((visit) => visit.visitId === billing.visitId)?.visitNumber ?? undefined;
  const { data: collections } = useCollectionsQuery({ from: toLocalDateInputValue(new Date(billing.createdAt)), to: todayDateInputValue() });
  const payments = (collections ?? []).filter((payment) => payment.invoiceId === billing.id);
  const paidBy = Object.entries(
    payments.reduce<Record<string, number>>((byMethod, payment) => ({ ...byMethod, [payment.method]: (byMethod[payment.method] ?? 0) + payment.amount }), {}),
  ).map(([method, amount]) => `${PAYMENT_METHOD_LABELS[method] ?? method} ${formatCurrency(amount)}`);
  const references = [...new Set(payments.map((payment) => payment.referenceNumber).filter(Boolean))];
  const hospitalContact = [brandingConfig?.address, brandingConfig?.phoneNumber ? `Ph: ${brandingConfig.phoneNumber}` : ''].filter(Boolean).join(' · ');

  const statusLabel = billing.isVoided
    ? billing.isReverted
      ? 'CANCELLED'
      : 'VOIDED'
    : billing.items.length > 0 && billing.items.every((item) => item.paymentStatus === 'Paid')
      ? 'PAID'
      : 'PENDING';
  // Charge and Discount columns only when the bill has an approved discount (ADR-109/111), so
  // the patient can see what each line cost before it; an undiscounted bill keeps one Amount column.
  const hasDiscount = billing.totalDiscount > 0;

  return (
    <div
      className="print-target hidden bg-white p-10 text-black print:block"
      style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}
    >
      <PrintPageStyle document="invoice" />
      <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-4 text-center">
        <img src={printLogo.url} alt={hospitalName} className="max-w-full" style={{ width: printLogo.display.width, height: printLogo.display.height, objectFit: printLogo.display.fit }} />
        <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
        <span className="text-xs text-gray-600">{hospitalContact || appTitle}</span>
      </div>

      <div className="mt-6 flex items-start justify-between gap-6">
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-gray-500">
            Billed to
          </span>
          <span className="text-base font-semibold">{billing.patientName}</span>
          <span className="text-sm text-gray-700">UHID: {billing.patientUhid}</span>
          {patient && (
            <span className="text-sm text-gray-700">
              {patient.age} yrs · {patient.gender}
              {patient.primaryPhone ? ` · Ph: ${patient.primaryPhone}` : ''}
            </span>
          )}
          {visitNumber && <span className="text-sm text-gray-700">Visit No: {visitNumber}</span>}
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1 text-right">
          <span className="text-lg font-bold uppercase tracking-widest">Invoice</span>
          <span className="whitespace-nowrap text-sm">
            <span className="text-gray-600">No.</span> {billing.invoiceNumber ?? billing.id}
          </span>
          <span className="whitespace-nowrap text-sm text-gray-600">
            {new Date(billing.createdAt).toLocaleString('en-IN')}
          </span>
          <span
            className="mt-1 border-2 px-3 py-1 text-sm font-bold uppercase tracking-widest"
            style={{ borderColor: 'black', color: statusLabel === 'PENDING' ? '#92400e' : 'black' }}
          >
            {statusLabel}
          </span>
          {billing.isVoided && billing.voidReason && (
            <span className="max-w-xs text-right text-xs text-gray-600">{billing.voidReason}</span>
          )}
        </div>
      </div>

      <table className="mt-6 w-full border-collapse text-sm">
        <thead>
          <tr className="border-y-2 border-black">
            <th className="py-2 pr-2 text-left font-semibold">#</th>
            <th className="py-2 pr-2 text-left font-semibold">Item / Service Details</th>
            {hasDiscount && <th className="py-2 pr-2 text-right font-semibold">Charge</th>}
            {hasDiscount && <th className="py-2 pr-2 text-right font-semibold">Discount</th>}
            <th className="py-2 text-right font-semibold">Amount</th>
          </tr>
        </thead>
        <tbody>
          {billing.items.map((item, index) => {
            const { serviceLabel, consultantName } = describeBillingItem(item);
            return (
              <tr key={item.id} className="border-b border-gray-300">
                <td className="py-2 pr-2 align-top text-gray-600">{index + 1}</td>
                <td className="py-2 pr-2 align-top">
                  {item.billingType} — {serviceLabel}
                  {consultantName !== '—' && <div className="text-xs text-gray-600">{consultantName}</div>}
                </td>
                {hasDiscount && <td className="py-2 pr-2 text-right align-top">{formatCurrency(item.quantity * item.unitPrice)}</td>}
                {hasDiscount && (
                  <td className="py-2 pr-2 text-right align-top">{item.discount > 0 ? `− ${formatCurrency(item.discount)}` : '—'}</td>
                )}
                <td className="py-2 text-right align-top font-medium">
                  {formatCurrency(item.total)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      <div className="mt-4 flex justify-end">
        <div className="flex w-64 flex-col gap-1.5 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-gray-600">Gross total</span>
            <span>{formatCurrency(billing.grossAmount)}</span>
          </div>
          {hasDiscount && (
            <div className="flex items-center justify-between">
              <span className="text-gray-600">Discount</span>
              <span>− {formatCurrency(billing.totalDiscount)}</span>
            </div>
          )}
          <div className="flex items-center justify-between border-t-2 border-black pt-1.5 text-base font-bold">
            <span>Net Payable</span>
            <span>{formatCurrency(billing.netAmount)}</span>
          </div>
          {paidBy.length > 0 && (
            <div className="flex items-start justify-between gap-3 text-gray-700">
              <span className="text-gray-600">Paid by</span>
              <span className="text-right">
                {paidBy.join(' + ')}
                {references.length > 0 && <span className="block text-xs text-gray-600">Ref: {references.join(', ')}</span>}
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="mt-10 flex items-end justify-between border-t border-gray-300 pt-3 text-xs text-gray-600">
        <span>
          Billed by {user?.name ?? 'Front Desk'}
          {user?.roleName ? ` · ${user.roleName}` : ''}
        </span>
        <span>Printed {serverNow().toLocaleString('en-IN')}</span>
      </div>
      <p className="mt-6 text-center text-sm italic text-gray-700">
        Thank you for choosing {hospitalName}. Wishing you a speedy recovery.
      </p>
    </div>
  );
}
