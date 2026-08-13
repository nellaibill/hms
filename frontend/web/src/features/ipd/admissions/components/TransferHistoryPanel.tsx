import { ArrowRight, Loader2 } from 'lucide-react';
import { useTransferHistoryQuery } from '../hooks/useTransferHistory';

interface TransferHistoryPanelProps {
  admissionId: string;
}

export function TransferHistoryPanel({ admissionId }: TransferHistoryPanelProps) {
  const { data: history, isPending, isError } = useTransferHistoryQuery(admissionId);

  if (isPending) {
    return (
      <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading transfer history…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
        Failed to load transfer history.
      </p>
    );
  }

  if (!history || history.length === 0) {
    return <p className="py-4 text-sm text-muted-foreground">This patient has not been transferred yet.</p>;
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">From</th>
            <th className="px-4 py-2.5" />
            <th className="px-4 py-2.5">To</th>
            <th className="px-4 py-2.5">Reason</th>
            <th className="px-4 py-2.5">When</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {history.map((entry) => (
            <tr key={entry.id}>
              <td className="px-4 py-3 text-sm text-foreground">
                {entry.oldWardName} / {entry.oldBedNumber}
              </td>
              <td className="px-4 py-3 text-muted-foreground">
                <ArrowRight className="h-4 w-4" />
              </td>
              <td className="px-4 py-3 text-sm text-foreground">
                {entry.newWardName} / {entry.newBedNumber}
              </td>
              <td className="px-4 py-3 text-sm text-muted-foreground">{entry.transferReason || '—'}</td>
              <td className="px-4 py-3 text-sm text-muted-foreground">{new Date(entry.transferredAt).toLocaleString('en-IN')}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
