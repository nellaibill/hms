import type { DrugSchedule } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Download, Loader2, Printer } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { dateTimeLabel, expiryLabel, quantity, todayIso } from '@/features/pharmacy/common/format';
import { SCHEDULE_BADGE } from '@/features/pharmacy/common/labels';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { exportReportToExcel } from '@/features/reports/exportUtils';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const REGISTERS: { key: string; label: string; title: string; schedules: DrugSchedule[] }[] = [
  { key: 'h1', label: 'Schedule H1', title: 'Schedule H1 register', schedules: ['H1'] },
  { key: 'narcotic', label: 'Schedule X / NDPS', title: 'Schedule X and narcotic drugs register', schedules: ['X', 'Narcotic'] },
  { key: 'h', label: 'Schedule H', title: 'Schedule H sales', schedules: ['H'] },
];

const field = 'h-8 rounded-md border border-input bg-background px-2 text-sm';

function monthStart(): string {
  return `${todayIso().slice(0, 8)}01`;
}

/**
 * The statutory registers: every sale of a Schedule H1, X or narcotic drug, with the patient,
 * the prescriber, the batch and the quantity. They are not kept by hand — the counter refuses
 * to bill these drugs without a patient name and a doctor, so the bills are the register and
 * this screen prints it for any period an inspector asks for.
 */
export default function RegistersPage() {
  const [register, setRegister] = useState(REGISTERS[0]);
  const [fromDate, setFromDate] = useState(monthStart());
  const [toDate, setToDate] = useState(todayIso());

  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'schedule-register', register.key, fromDate, toDate],
    queryFn: () => pharmacyApi.getScheduleRegister(fromDate, toDate, register.schedules),
    enabled: Boolean(fromDate && toDate && fromDate <= toDate),
  });

  function exportRegister() {
    if (!data) return;
    void exportReportToExcel(`${register.key}-register-${fromDate}-to-${toDate}.xlsx`, [
      {
        heading: register.label,
        headers: ['Date', 'Bill no.', 'Patient', 'UHID', 'Phone', 'Prescribed by', 'Drug', 'Generic', 'Schedule', 'Batch', 'Expiry', 'Quantity', 'Returned'],
        rows: data.map((entry) => [
          dateTimeLabel(entry.saleDate),
          entry.billNo,
          entry.patientName,
          entry.patientUhid ?? '',
          entry.patientPhone ?? '',
          entry.doctorName ?? '',
          entry.productName,
          entry.genericName ?? '',
          entry.schedule,
          entry.batchNo,
          expiryLabel(entry.expiryDate),
          entry.quantity,
          entry.returnedQuantity,
        ]),
      },
    ]);
  }

  return (
    <PharmacyPage
      title="Registers"
      subtitle={data ? `${register.title} · ${data.length} ${data.length === 1 ? 'entry' : 'entries'}` : register.title}
      actions={
        <>
          <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
            From
            <input type="date" value={fromDate} max={toDate} onChange={(event) => setFromDate(event.target.value)} className={field} />
          </label>
          <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
            To
            <input type="date" value={toDate} max={todayIso()} onChange={(event) => setToDate(event.target.value)} className={field} />
          </label>
          <Button size="sm" variant="outline" onClick={exportRegister} disabled={!data || data.length === 0}>
            <Download className="h-4 w-4" />
            Excel
          </Button>
          <Button size="sm" onClick={() => window.print()} disabled={!data || data.length === 0}>
            <Printer className="h-4 w-4" />
            Print
          </Button>
        </>
      }
    >
      <div className="flex w-fit overflow-hidden rounded-md border border-border text-sm font-medium print:hidden">
        {REGISTERS.map((option) => (
          <button key={option.key} type="button" onClick={() => setRegister(option)} className={cn('px-4 py-1.5', register.key === option.key ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
            {option.label}
          </button>
        ))}
      </div>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading the register…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The register could not be loaded. Choose a period of up to a year.
        </p>
      )}

      {data && data.length === 0 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-10 text-center text-sm text-muted-foreground">No {register.label} drug was sold in this period.</p>
      )}

      {data && data.length > 0 && (
        <div className="print-target overflow-x-auto rounded-lg border border-border bg-background print:border-0">
          <p className="hidden px-3 py-2 text-sm font-semibold print:block">
            {register.title} · {fromDate.split('-').reverse().join('-')} to {toDate.split('-').reverse().join('-')}
          </p>
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground print:bg-transparent print:text-black">
              <tr>
                <th className="px-3 py-2">#</th>
                <th className="px-3 py-2">Date</th>
                <th className="px-3 py-2">Bill no.</th>
                <th className="px-3 py-2">Patient</th>
                <th className="px-3 py-2">Prescribed by</th>
                <th className="px-3 py-2">Drug</th>
                <th className="px-3 py-2">Batch · expiry</th>
                <th className="px-3 py-2 text-right">Qty</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.map((entry, index) => {
                const badge = SCHEDULE_BADGE[entry.schedule];
                return (
                  <tr key={`${entry.saleId}-${index}`}>
                    <td className="px-3 py-1.5 text-muted-foreground">{index + 1}</td>
                    <td className="whitespace-nowrap px-3 py-1.5">
                      <div>{dateTimeLabel(entry.saleDate).split(', ')[0]}</div>
                      <div className="text-xs text-muted-foreground">{dateTimeLabel(entry.saleDate).split(', ')[1]}</div>
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 font-mono text-xs">
                      <Link to={`/pharmacy/sales/${entry.saleId}`} className="font-medium text-primary hover:underline print:text-black print:no-underline">
                        {entry.billNo}
                      </Link>
                    </td>
                    <td className="min-w-[10rem] px-3 py-1.5">
                      <div className="font-medium text-foreground">{entry.patientName}</div>
                      <div className="text-xs text-muted-foreground">{[entry.patientUhid, entry.patientPhone].filter(Boolean).join(' · ')}</div>
                    </td>
                    <td className="min-w-[8rem] px-3 py-1.5">{entry.doctorName ?? '—'}</td>
                    <td className="min-w-[12rem] px-3 py-1.5">
                      <span className="font-medium text-foreground">{entry.productName}</span>
                      {badge && <span className={cn('ml-1.5 rounded px-1 py-px text-[10px] font-semibold print:hidden', badge.className)}>{badge.label}</span>}
                      {entry.genericName && <div className="text-xs text-muted-foreground">{entry.genericName}</div>}
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 font-mono text-xs">
                      {entry.batchNo} · {expiryLabel(entry.expiryDate)}
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-right tabular-nums">
                      {quantity(entry.quantity)}
                      {entry.returnedQuantity > 0 && <div className="text-xs text-muted-foreground">{quantity(entry.returnedQuantity)} returned</div>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </PharmacyPage>
  );
}
