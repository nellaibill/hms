import { cn } from '@/lib/utils';

function Bone({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded-md bg-muted', className)} />;
}

export function SummaryCardsSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {Array.from({ length: 4 }, (_, i) => (
        <div key={i} className="flex items-center gap-4 rounded-lg border border-border p-5">
          <Bone className="h-11 w-11 rounded-lg" />
          <div className="flex-1">
            <Bone className="h-6 w-16" />
            <Bone className="mt-2 h-3 w-24" />
          </div>
        </div>
      ))}
    </div>
  );
}

export function FilterBarSkeleton() {
  return (
    <div className="rounded-lg border border-border p-5">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {Array.from({ length: 6 }, (_, i) => (
          <div key={i} className="flex flex-col gap-1.5">
            <Bone className="h-3.5 w-20" />
            <Bone className="h-10 w-full" />
          </div>
        ))}
      </div>
    </div>
  );
}

export function TableSkeleton() {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <div className="border-b border-border bg-muted/40 p-3">
        <Bone className="h-4 w-full" />
      </div>
      {Array.from({ length: 8 }, (_, i) => (
        <div key={i} className="flex items-center gap-4 border-b border-border p-3 last:border-b-0">
          <Bone className="h-5 w-5 shrink-0 rounded" />
          <Bone className="h-4 w-40" />
          <Bone className="h-4 w-24" />
          <Bone className="ml-auto h-4 w-16" />
        </div>
      ))}
    </div>
  );
}

export function PreviewPanelSkeleton() {
  return (
    <div className="flex h-full flex-col gap-4 p-4">
      <div className="flex items-center gap-2.5 border-b border-border pb-4">
        <Bone className="h-5 w-5 rounded" />
        <Bone className="h-4 w-32" />
      </div>
      <Bone className="h-48 w-full" />
      <div className="flex flex-col gap-2">
        {Array.from({ length: 6 }, (_, i) => (
          <Bone key={i} className="h-4 w-full" />
        ))}
      </div>
    </div>
  );
}
