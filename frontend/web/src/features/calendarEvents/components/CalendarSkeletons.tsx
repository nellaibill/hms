import { cn } from '@/lib/utils';

function Bone({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded-md bg-muted', className)} />;
}

export function SidebarSkeleton() {
  return (
    <div className="flex w-full flex-col gap-5 p-4 sm:w-[280px] sm:shrink-0 sm:border-r sm:border-border">
      <Bone className="h-10 w-full" />
      <Bone className="h-9 w-full" />
      <div className="flex flex-col gap-2">
        <Bone className="h-4 w-24" />
        <Bone className="h-40 w-full" />
      </div>
      <div className="flex flex-col gap-2">
        {Array.from({ length: 5 }, (_, i) => (
          <Bone key={i} className="h-6 w-full" />
        ))}
      </div>
    </div>
  );
}

export function CalendarGridSkeleton() {
  return (
    <div className="flex flex-col gap-3 p-4 sm:p-6">
      <div className="flex items-center gap-3">
        <Bone className="h-8 w-24" />
        <Bone className="h-6 w-40" />
      </div>
      <div className="overflow-hidden rounded-lg border border-border">
        <div className="grid grid-cols-7 gap-px bg-border">
          {Array.from({ length: 42 }, (_, i) => (
            <div key={i} className="min-h-[104px] bg-card p-2">
              <Bone className="h-5 w-5 rounded-full" />
              {i % 5 === 0 && <Bone className="mt-2 h-4 w-full" />}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export function EventDrawerSkeleton() {
  return (
    <div className="flex w-full flex-col gap-5 p-6 sm:max-w-md">
      <Bone className="h-6 w-2/3" />
      <Bone className="h-4 w-full" />
      <Bone className="h-4 w-5/6" />
      <div className="grid grid-cols-2 gap-3">
        <Bone className="h-16 w-full" />
        <Bone className="h-16 w-full" />
      </div>
      <Bone className="h-24 w-full" />
    </div>
  );
}
