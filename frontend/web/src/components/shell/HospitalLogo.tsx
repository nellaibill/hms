import { branding } from '@/config/branding';
import { useBrandLogo } from '@/features/branding/brandLogo';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import type { LogoSlot } from '@/features/branding/types';
import { cn } from '@/lib/utils';

interface HospitalLogoProps {
  className?: string;
  /** Show the configured app title next to the logo image (the bundled default image already has "Lakshmi Hospitals" baked in). */
  showName?: boolean;
  /** Use on a solid `bg-primary` surface (e.g. the top header) — lightens the system-name text to read on that background. */
  invert?: boolean;
  /** Caps on the logo box (defaults to `max-h-16 max-w-32`) — pass Tailwind max-height/max-width utilities only. The box's actual size is the slot's configured width × height (Theme & Branding → Logo Configuration), clamped by these caps, so a surface with a fixed size (the top bar) can never be grown by a configured size. */
  imageClassName?: string;
  /** Which configured logo to show — see LOGO_SLOT_META. Falls back to the Primary logo, then the bundled default. */
  slot?: LogoSlot;
}

/**
 * Tenants upload logos of wildly different dimensions/aspect ratios/shapes (wide, tall,
 * square, circular) — the box below is a slot every logo scales *inside* of, never the other
 * way around. The box is the slot's configured width × height, capped by `imageClassName`'s
 * max-h/max-w; the image fills it with the configured object-fit ("contain" — the default —
 * never distorts or crops; "cover"/"fill" are the admin's explicit choice) and can never grow
 * the box. `overflow-hidden` on the box is a safety net for any image whose intrinsic
 * sizing tries to escape the clamp anyway (e.g. an SVG with a `width`/`height` attribute of
 * its own).
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
const LOGO_BOX = 'flex shrink-0 items-center justify-center overflow-hidden';

export function HospitalLogo({ className, showName = true, invert = false, imageClassName = 'max-h-16 max-w-32', slot = 'primary' }: HospitalLogoProps) {
  const { data: brandingConfig } = useBrandingQuery();
  const hospitalName = brandingConfig?.hospitalName ?? branding.hospitalName;
  const appTitle = brandingConfig?.appTitle ?? branding.systemName;
  // The slot's own upload, else the Primary logo (if enabled as fallback), else the bundled default.
  const { url: logoUrl, display } = useBrandLogo(slot);

  return (
    <div className={cn('flex items-center gap-4', className)}>
      <span className={cn(LOGO_BOX, imageClassName)} style={{ width: display.width, height: display.height }}>
        <img src={logoUrl} alt={hospitalName} className="h-full w-full" style={{ objectFit: display.fit }} />
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
