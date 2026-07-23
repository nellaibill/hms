import { HeartPulse } from 'lucide-react';
import { cn } from '@/lib/utils';

export function HospitalLogo({ className, showName = true }: { className?: string; showName?: boolean }) {
  return (
    <div className={cn('flex items-center gap-2.5', className)}>
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground">
        <HeartPulse className="h-5 w-5" />
      </span>
      {showName && (
        <span className="flex flex-col leading-tight">
          <span className="text-sm font-semibold tracking-tight">Lakshmi Hospitals</span>
          <span className="text-[11px] text-muted-foreground">Hospital Management Information System</span>
        </span>
      )}
    </div>
  );
}
