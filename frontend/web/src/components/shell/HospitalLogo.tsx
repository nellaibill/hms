import { branding } from '@/config/branding';
import { cn } from '@/lib/utils';

export function HospitalLogo({ className, showName = true }: { className?: string; showName?: boolean }) {
  const Icon = branding.logoIcon;

  return (
    <div className={cn('flex items-center gap-2.5', className)}>
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground shadow-soft">
        <Icon className="h-5 w-5" />
      </span>
      {showName && (
        <span className="flex flex-col leading-tight">
          <span className="text-sm font-semibold tracking-tight text-foreground">{branding.hospitalName}</span>
          <span className="text-[11px] text-muted-foreground">{branding.systemName}</span>
        </span>
      )}
    </div>
  );
}
