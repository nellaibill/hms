import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { humanize } from '@/features/patients/humanize';
import type { LabOrder } from '../types';

interface LabReportPrintTemplateProps {
  order: LabOrder;
}

/**
 * The actual printed lab report — mirrors InvoicePrintTemplate.tsx's exact pattern: a hidden
 * div shown only via the `.print-target`/`print:block` rule (index.css) once printing starts,
 * with its own fixed black/white/serif layout independent of the app's on-screen theme. No PDF
 * library — browser print only, per this repo's established convention.
 */
export function LabReportPrintTemplate({ order }: LabReportPrintTemplateProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  const logoUrl = brandingConfig?.logoUrl ?? defaultLogoUrl;

  return (
    <div className="print-target hidden bg-white p-10 text-black print:block" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
      <div className="flex flex-col items-center gap-1 border-b-2 border-black pb-4 text-center">
        <img src={logoUrl} alt={hospitalName} className="h-20 w-auto object-contain" />
        <span className="text-2xl font-bold tracking-tight">{hospitalName}</span>
        <span className="text-xs text-gray-600">{appTitle}</span>
        <span className="mt-1 text-sm font-semibold uppercase tracking-widest">Laboratory Report</span>
      </div>

      <div className="mt-6 flex items-start justify-between gap-6">
        <div className="flex flex-col gap-0.5">
          <span className="text-xs font-semibold uppercase tracking-wide text-gray-500">Patient</span>
          <span className="text-base font-semibold">{order.patientName}</span>
          <span className="text-sm text-gray-700">UHID: {order.patientUhid}</span>
          {order.source && <span className="text-sm text-gray-700">Source: {order.source}</span>}
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1 text-right">
          <span className="whitespace-nowrap text-sm">
            <span className="text-gray-600">Order No.</span> {order.labOrderNumber}
          </span>
          <span className="whitespace-nowrap text-sm text-gray-600">Ordered {new Date(order.createdAt).toLocaleString('en-IN')}</span>
          {order.reportGeneratedAt && (
            <span className="whitespace-nowrap text-sm text-gray-600">Generated {new Date(order.reportGeneratedAt).toLocaleString('en-IN')}</span>
          )}
          {order.reportReleasedAt && (
            <span className="whitespace-nowrap text-sm text-gray-600">Released {new Date(order.reportReleasedAt).toLocaleString('en-IN')}</span>
          )}
        </div>
      </div>

      <div className="mt-8 flex flex-col gap-6">
        {order.items.map((item) => (
          <div key={item.id} className="flex flex-col gap-2">
            <div className="flex items-baseline justify-between border-b border-black pb-1">
              <span className="text-base font-bold">{item.testName}</span>
              {item.sampleType && <span className="text-xs text-gray-600">Sample: {item.sampleType}</span>}
            </div>
            {item.parameters.length === 0 ? (
              <p className="text-sm text-gray-600">No result parameters recorded.</p>
            ) : (
              <table className="w-full border-collapse text-sm">
                <thead>
                  <tr className="border-b-2 border-black">
                    <th className="py-1.5 pr-2 text-left font-semibold">Parameter</th>
                    <th className="py-1.5 pr-2 text-left font-semibold">Result</th>
                    <th className="py-1.5 pr-2 text-left font-semibold">Unit</th>
                    <th className="py-1.5 pr-2 text-left font-semibold">Reference Range</th>
                    <th className="py-1.5 text-left font-semibold">Flag</th>
                  </tr>
                </thead>
                <tbody>
                  {item.parameters.map((parameter) => (
                    <tr key={parameter.id} className="border-b border-gray-300">
                      <td className="py-1.5 pr-2">{parameter.parameterName}</td>
                      <td className="py-1.5 pr-2 font-semibold">{parameter.resultValue}</td>
                      <td className="py-1.5 pr-2 text-gray-600">{parameter.unit ?? '—'}</td>
                      <td className="py-1.5 pr-2 text-gray-600">{parameter.referenceRange ?? '—'}</td>
                      <td className="py-1.5">{parameter.flag && parameter.flag !== 'Normal' ? `${humanize(parameter.flag)} *` : parameter.flag ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        ))}
      </div>

      <div className="mt-10 flex items-end justify-between border-t border-gray-300 pt-3 text-xs text-gray-600">
        <span>Every result verified prior to report generation.</span>
        <span>Printed {new Date().toLocaleString('en-IN')}</span>
      </div>
      <p className="mt-6 text-center text-sm italic text-gray-700">This report is generated electronically and is valid without a signature.</p>
    </div>
  );
}
