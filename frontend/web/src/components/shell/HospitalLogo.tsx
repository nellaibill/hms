import defaultLogoUrl from '@/assets/logo.png';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { cn } from '@/lib/utils';

interface HospitalLogoProps {
  className?: string;
  /** Show the configured app title next to the logo image (the bundled default image already has "Lakshmi Hospitals" baked in). */
  showName?: boolean;
  /** Use on a solid `bg-primary` surface (e.g. the top header) — wraps the logo in a white chip (its artwork assumes a light backing) and lightens the system-name text. */
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
 * Width is `w-auto max-w-32`, not a fixed `w-32` — the box (and the `invert` chip's white
 * background wrapped around it) hugs whatever the image actually renders at, up to that cap.
 * A fixed width was originally used here and looked fine for the ~3.3:1 landscape default
 * logo, but left a wide, obviously-rectangular white margin around anything narrower — most
 * visibly a square/circular logo, which already carries its own solid backing and doesn't
 * need (or want) a boxy white halo around it.
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
      {/* overflow-hidden lives on this SAME element as the rounding and the white background —
          not just on the inner LOGO_BOX — so whatever's visible is always clipped flush with
          the chip's own shape. Splitting those across two nested boxes (padding+round on the
          outer, clip only on the inner) let a squarish image's corners poke past the rounded
          white background into the bare header color behind it, which is exactly the "doesn't
          look right" case a square/near-square upload hit. rounded-lg (not rounded-full): a
          full circle/pill clips real content off a wide wordmark's corners, which a small
          fixed radius doesn't — it still reads as an intentional shape against a round logo
          without cropping a landscape one. */}
      <span className={cn('flex shrink-0 items-center justify-center overflow-hidden rounded-lg', invert && 'bg-white p-1.5 shadow-soft')}>
        <span className={cn(LOGO_BOX, imageClassName)}>
          <img src={logoUrl} alt={hospitalName} className="max-h-full max-w-full object-contain" />
        </span>
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
