import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { useAuth } from '@/features/auth/AuthContext';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { describeBillingItem, formatCurrency } from '../billingCalculations';
import type { Billing } from '../types';

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
  const logoUrl = brandingConfig?.logoUrl ?? defaultLogoUrl;

  // Same reference-cache priming InvoiceDetailCard does — kept here too so this template
  // resolves real names even if it's ever rendered without that card mounted alongside it.
  useMasterOptionsQuery('diagnosticTest');
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const statusLabel = billing.isVoided
    ? 'VOIDED'
    : billing.items.length > 0 && billing.items.every((item) => item.paymentStatus === 'Paid')
      ? 'PAID'
      : 'PENDING';

  return (
    <div
      className="print-target hidden bg-white p-10 text-black print:block"
      style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}
    >
      <div className="flex items-start justify-between gap-6 border-b-2 border-black pb-4">
        <div className="flex items-center gap-4">
          <img src={logoUrl} alt={hospitalName} className="h-16 w-auto object-contain" />
          <div className="flex flex-col">
            <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
            <span className="text-xs text-gray-600">{appTitle}</span>
          </div>
        </div>
        <div className="flex flex-col items-end gap-1 text-right">
          <span className="text-lg font-bold uppercase tracking-widest">Invoice</span>
          <span className="text-sm">
            <span className="text-gray-600">No.</span> {billing.invoiceNumber ?? billing.id}
          </span>
          <span className="text-sm text-gray-600">
            {new Date(billing.createdAt).toLocaleString('en-IN')}
          </span>
        </div>
      </div>

      <div className="mt-6 flex items-start justify-between gap-6">
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-gray-500">
            Billed to
          </span>
          <span className="text-base font-semibold">{billing.patientName}</span>
          <span className="text-sm text-gray-700">UHID: {billing.patientUhid}</span>
        </div>
        <div className="flex flex-col items-end gap-1">
          <span
            className="border-2 px-3 py-1 text-sm font-bold uppercase tracking-widest"
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
            <th className="py-2 pr-2 text-left font-semibold">Description</th>
            <th className="py-2 pr-2 text-left font-semibold">Rendered by</th>
            <th className="py-2 pr-2 text-right font-semibold">Qty</th>
            <th className="py-2 pr-2 text-right font-semibold">Unit Price</th>
            <th className="py-2 pr-2 text-right font-semibold">Discount</th>
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
                </td>
                <td className="py-2 pr-2 align-top text-gray-600">{consultantName}</td>
                <td className="py-2 pr-2 text-right align-top">{item.quantity}</td>
                <td className="py-2 pr-2 text-right align-top">{formatCurrency(item.unitPrice)}</td>
                <td className="py-2 pr-2 text-right align-top">
                  {item.discount > 0 ? formatCurrency(item.discount) : '—'}
                </td>
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
          <div className="flex items-center justify-between">
            <span className="text-gray-600">Discount</span>
            <span>
              {billing.totalDiscount > 0
                ? `- ${formatCurrency(billing.totalDiscount)}`
                : formatCurrency(0)}
            </span>
          </div>
          <div className="flex items-center justify-between border-t-2 border-black pt-1.5 text-base font-bold">
            <span>Net Payable</span>
            <span>{formatCurrency(billing.netAmount)}</span>
          </div>
        </div>
      </div>

      <div className="mt-10 flex items-end justify-between border-t border-gray-300 pt-3 text-xs text-gray-600">
        <span>
          Billed by {user?.name ?? 'Front Desk'}
          {user?.roleName ? ` · ${user.roleName}` : ''}
        </span>
        <span>Printed {new Date().toLocaleString('en-IN')}</span>
      </div>
      <p className="mt-6 text-center text-sm italic text-gray-700">
        Thank you for choosing {hospitalName}. Wishing you a speedy recovery.
      </p>
    </div>
  );
}
