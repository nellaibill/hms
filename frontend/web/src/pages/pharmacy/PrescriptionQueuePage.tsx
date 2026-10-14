import { useQuery } from '@tanstack/react-query';
import { Loader2, ReceiptText, RefreshCw } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { dateTimeLabel, money, quantity, todayIso } from '@/features/pharmacy/common/format';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

type View = 'waiting' | 'all';

/**
 * Prescriptions written in OPD, as they reach the pharmacy. Each completed consultation with
 * medicines on it appears here the moment the doctor finishes; "Bill" opens the counter with
 * the patient, the doctor and the prescribed drugs already loaded. A prescription that has
 * been billed stays on the list, marked with its bill.
 */
export default function PrescriptionQueuePage() {
  const { hasPermission } = useAuth();
  const [date, setDate] = useState(todayIso());
  const [view, setView] = useState<View>('waiting');

  const { data, isPending, isError, isFetching, refetch } = useQuery({
    queryKey: ['pharmacy', 'prescriptions', date],
    queryFn: () => pharmacyApi.getPrescriptionQueue(date, date),
    // Patients walk from the consulting room to the counter in a couple of minutes.
    refetchInterval: date === todayIso() ? 30_000 : false,
  });

  const waiting = data?.filter((item) => item.bills.length === 0) ?? [];
  const shown = view === 'waiting' ? waiting : (data ?? []);

  return (
    <PharmacyPage
      title="Prescriptions"
      subtitle={data ? `${waiting.length} waiting · ${data.length - waiting.length} billed` : 'Prescriptions written in OPD.'}
      actions={
        <>
          <input type="date" value={date} max={todayIso()} onChange={(event) => setDate(event.target.value)} aria-label="Date" className="h-8 rounded-md border border-input bg-background px-2 text-sm" />
          <div className="flex overflow-hidden rounded-md border border-border text-xs font-medium">
            {(['waiting', 'all'] as View[]).map((value) => (
              <button key={value} type="button" onClick={() => setView(value)} className={cn('px-3 py-1.5', view === value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
                {value === 'waiting' ? 'Waiting' : 'All'}
              </button>
            ))}
          </div>
          <Button size="sm" variant="outline" onClick={() => void refetch()} disabled={isFetching}>
            <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} />
            Refresh
          </Button>
        </>
      }
    >
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading prescriptions…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Prescriptions could not be loaded.
        </p>
      )}

      {data && shown.length === 0 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-10 text-center text-sm text-muted-foreground">
          {data.length === 0 ? 'No prescriptions were written in OPD on this day.' : 'Every prescription from this day has been billed.'}
        </p>
      )}

      <div className="grid gap-3 xl:grid-cols-2">
        {shown.map((item) => (
          <article key={item.consultationId} className={cn('flex flex-col rounded-lg border bg-background', item.bills.length === 0 ? 'border-primary/30' : 'border-border')}>
            <header className="flex flex-wrap items-start justify-between gap-3 border-b border-border px-4 py-3">
              <div className="min-w-0">
                <p className="truncate font-medium text-foreground">{item.patientName}</p>
                <p className="truncate text-xs text-muted-foreground">
                  {item.uhid}
                  {item.phoneNumber ? ` · ${item.phoneNumber}` : ''} · {item.doctorName}
                  {item.departmentName ? `, ${item.departmentName}` : ''}
                </p>
                <p className="text-xs text-muted-foreground">{dateTimeLabel(item.prescribedAt)}</p>
              </div>
              <div className="flex flex-col items-end gap-1.5">
                {item.bills.length === 0 ? (
                  <Badge variant="warning">Waiting</Badge>
                ) : (
                  item.bills.map((bill) => (
                    <Link key={bill.saleId} to={`/pharmacy/sales/${bill.saleId}`} className="text-xs font-medium text-primary hover:underline">
                      {bill.billNo} · ₹{money(bill.netAmount)}
                    </Link>
                  ))
                )}
                {hasPermission('pharmacy.create') && (
                  <Button size="sm" variant={item.bills.length === 0 ? 'default' : 'outline'} asChild>
                    <Link to={`/pharmacy/counter?rx=${item.consultationId}&date=${date}`}>
                      <ReceiptText className="h-4 w-4" />
                      {item.bills.length === 0 ? 'Bill' : 'Bill again'}
                    </Link>
                  </Button>
                )}
              </div>
            </header>
            <ul className="divide-y divide-border text-sm">
              {item.drugs.map((drug, index) => (
                <li key={index} className="flex items-baseline justify-between gap-3 px-4 py-1.5">
                  <span className="min-w-0">
                    <span className="font-medium text-foreground">{drug.drugName}</span>
                    <span className="ml-2 text-xs text-muted-foreground">
                      {[drug.dose, drug.frequency, drug.durationDays ? `${drug.durationDays} days` : null, drug.route, drug.instructions].filter(Boolean).join(' · ')}
                    </span>
                  </span>
                  {drug.suggestedQuantity != null && <span className="shrink-0 tabular-nums text-muted-foreground">× {quantity(drug.suggestedQuantity)}</span>}
                </li>
              ))}
            </ul>
          </article>
        ))}
      </div>
    </PharmacyPage>
  );
}
