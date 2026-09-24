import { Loader2 } from 'lucide-react';

/** Loading/error/empty placeholder that fills a ChartCard's plot area. */
export function ChartState({ status, emptyMessage }: { status: 'loading' | 'error' | 'empty'; emptyMessage?: string }) {
  return (
    <div className="flex h-full items-center justify-center gap-2 text-sm text-muted-foreground">
      {status === 'loading' && <Loader2 className="h-4 w-4 animate-spin" />}
      {status === 'loading' ? 'Loading…' : status === 'error' ? "Couldn't load this chart — please refresh." : (emptyMessage ?? 'No data yet.')}
    </div>
  );
}
