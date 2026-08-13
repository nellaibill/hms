import { Loader2 } from 'lucide-react';
import { useBedStayHistoryQuery } from '../hooks/useBedStayHistory';

interface BedStayHistoryPanelProps {
  admissionId: string;
}

export function BedStayHistoryPanel({ admissionId }: BedStayHistoryPanelProps) {
  const { data: stays, isPending, isError } = useBedStayHistoryQuery(admissionId);

  if (isPending) {
    return (
      <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading bed stay history…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        Failed to load bed stay history.
      </p>
    );
  }

  if (!stays || stays.length === 0) {
    return <p className="py-4 text-sm text-muted-foreground">No bed stay history yet.</p>;
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Bed</th>
            <th className="px-4 py-2.5">Ward</th>
            <th className="px-4 py-2.5">From</th>
            <th className="px-4 py-2.5">To</th>
            <th className="px-4 py-2.5 text-right">Daily Charge</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {stays.map((stay) => (
            <tr key={stay.id}>
              <td className="px-4 py-3 text-sm text-foreground">{stay.bedNumber}</td>
              <td className="px-4 py-3 text-sm text-foreground">{stay.wardName}</td>
              <td className="px-4 py-3 text-sm text-muted-foreground">{new Date(stay.fromDateTime).toLocaleString('en-IN')}</td>
              <td className="px-4 py-3 text-sm text-muted-foreground">
                {stay.toDateTime ? new Date(stay.toDateTime).toLocaleString('en-IN') : '—'}
              </td>
              <td className="px-4 py-3 text-right font-mono text-sm text-foreground">₹{stay.dailyCharge.toLocaleString('en-IN')}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
