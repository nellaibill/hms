import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { cn } from '@/lib/utils';

interface HospitalLogoProps {
  className?: string;
  /** Show the configured app title next to the logo image (the bundled default image already has "Lakshmi Hospitals" baked in). */
  showName?: boolean;
  /** Use on a solid `bg-primary` surface (e.g. the top header) — lightens the system-name text to read on that background. */
  invert?: boolean;
  /** Overrides the logo-box height/max-width (defaults to `h-10 max-w-32`) — pass Tailwind height/width utilities only; every other constraint (object-contain, overflow-hidden, centering) is fixed and not meant to be overridden per call site. */
  imageClassName?: string;
}

/**
 * Tenants upload logos of wildly different dimensions/aspect ratios/shapes (wide, tall,
 * square, circular) — the box below is a slot every logo scales *inside* of, never the other
 * way around. `max-h-full max-w-full` + `object-contain` (never `object-cover`) means the
 * image is clamped down to fit without ever being stretched, cropped, or allowed to grow the
 * box — a tiny logo just renders at its natural size, centered, rather than being blown up
 * and blurred. `overflow-hidden` on the box is a safety net for any image whose intrinsic
 * sizing tries to escape the clamp anyway (e.g. an SVG with a `width`/`height` attribute of
 * its own).
 *
 * Width is `w-auto max-w-32`, not a fixed `w-32` — the box hugs whatever the image actually
 * renders at, up to that cap, rather than always claiming the full slot width.
 *
 * Deliberately no background/chip behind the image (tried a white one, then a rounded one —
 * both ended up fighting whatever shape/color the uploaded logo actually was: a visible box
 * around a logo that already carries its own backing, a color clash against a differently
 * colored one, corners of a non-transparent image poking past a rounded edge). The bundled
 * default asset (assets/logo.png) already bakes its own opaque white canvas into the image
 * itself, so it and every other opaque upload render correctly with nothing added behind
 * them; only a genuinely transparent logo with light-colored artwork could end up hard to
 * read against the blue header, which is a property of that specific file, not something a
 * generic wrapper here can fix for every possible upload without breaking some other one.
 */
const LOGO_BOX = 'flex h-10 w-auto max-w-32 shrink-0 items-center justify-center overflow-hidden';

export function HospitalLogo({ className, showName = true, invert = false, imageClassName }: HospitalLogoProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  // Admin-uploaded logo wins; otherwise fall back to the bundled default artwork.
  const logoUrl = brandingConfig?.logoUrl ?? defaultLogoUrl;

  return (
    <div className={cn('flex items-center gap-4', className)}>
      <span className={cn(LOGO_BOX, imageClassName)}>
        <img src={logoUrl} alt={hospitalName} className="max-h-full max-w-full object-contain" />
      </span>
      {showName && (
        <span
          className={cn(
            'hidden text-sm font-semibold leading-tight sm:inline',
            invert ? 'text-header-foreground/90' : 'text-muted-foreground',
          )}
        >
          {appTitle}
        </span>
      )}
    </div>
  );
}
