import logoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { cn } from '@/lib/utils';

interface HospitalLogoProps {
  className?: string;
  /** Show branding.systemName next to the logo image (the image itself already has "Lakshmi Hospitals" baked in). */
  showName?: boolean;
  /** Use on a solid `bg-primary` surface (e.g. the top header) — wraps the logo in a white chip (its artwork assumes a light backing) and lightens the system-name text. */
  invert?: boolean;
  imageClassName?: string;
}

export function HospitalLogo({ className, showName = true, invert = false, imageClassName }: HospitalLogoProps) {
  return (
    <div className={cn('flex items-center gap-2.5', className)}>
      <span className={cn('flex shrink-0 items-center rounded-md', invert && 'bg-white px-2 py-1 shadow-soft')}>
        <img src={logoUrl} alt={branding.hospitalName} className={cn('h-8 w-auto', imageClassName)} />
      </span>
      {showName && (
        <span
          className={cn(
            'hidden text-xs font-medium leading-tight sm:inline',
            invert ? 'text-primary-foreground/85' : 'text-muted-foreground',
          )}
        >
          {branding.systemName}
        </span>
      )}
    </div>
  );
}
