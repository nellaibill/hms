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
    <div className={cn('flex items-center gap-4', className)}>
      <span className={cn('flex shrink-0 items-center justify-center rounded-md', invert && 'bg-white px-3 shadow-soft')}>
        {/* Fixed width (not height) so the ~3.3:1 landscape logo scales up without distortion — h-auto keeps its natural aspect ratio. */}
        <img src={logoUrl} alt={branding.hospitalName} className={cn('h-auto w-48 object-contain', imageClassName)} />
      </span>
      {showName && (
        <span
          className={cn(
            'hidden text-sm font-semibold leading-tight sm:inline',
            invert ? 'text-header-foreground/90' : 'text-muted-foreground',
          )}
        >
          {branding.systemName}
        </span>
      )}
    </div>
  );
}
