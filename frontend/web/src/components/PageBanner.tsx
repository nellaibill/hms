import { ArrowLeft } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

interface PageBannerProps {
  /** Renders inside the default rounded icon chip. Omit and pass `leading` instead for a
   * fully custom leading element (e.g. a user's Avatar on a profile page). */
  icon?: React.ElementType;
  /** Overrides the default icon chip with custom content — used by the handful of pages
   * whose banner leads with something other than a plain icon (an Avatar, for instance). */
  leading?: ReactNode;
  title: ReactNode;
  subtitle?: ReactNode;
  /** Extra content on the title row, after the title itself — e.g. a status Badge. */
  titleExtra?: ReactNode;
  /** Buttons pinned to the banner's right edge (Edit, Set password, etc.) — mirrors how the
   * back link pins to the left, so both float outside the centered title block instead of
   * disturbing its centering. */
  rightActions?: ReactNode;
  /** Route for the back link. Omit entirely on pages with no "back" concept (hub/dashboard
   * pages reached straight from the sidebar, not drilled into from another page). */
  backTo?: string;
  backLabel?: string;
  className?: string;
}

/**
 * The brand-colored page banner every module page opens with (icon + title + subtitle,
 * centered) — shared so every page renders it identically instead of each hand-rolling its
 * own copy (previously ~90 near-duplicate copies, some subtly different). The back link, when
 * given, renders inside this same bar at the far left rather than as a separate row above it,
 * with `rightActions` on the opposite edge.
 *
 * Laid out as a three-column grid (`1fr | fit-content | 1fr`) rather than absolutely positioning
 * the side slots: the equal side tracks keep the title block centered, but each side track can't
 * shrink below its own content, so a long back label or a wide right-side button always reserves
 * its space instead of sliding over the title/subtitle. The earlier absolute layout overlapped
 * whenever the content area (viewport minus the sidebar) was narrower than the viewport-based
 * `sm:`/`md:` breakpoints assumed.
 */
export function PageBanner({ icon: Icon, leading, title, subtitle, titleExtra, rightActions, backTo, backLabel = 'Back', className }: PageBannerProps) {
  const hasSideSlots = Boolean(backTo || rightActions);

  const centerBlock = (
    <div className="flex min-w-0 flex-col items-center gap-1 text-center">
      <div className="flex min-w-0 items-center gap-3">
        {leading ??
          (Icon && (
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <Icon className="h-5 w-5" />
            </span>
          ))}
        <h1 className="min-w-0 text-xl font-semibold tracking-tight">{title}</h1>
        {titleExtra}
      </div>
      {subtitle && <p className="max-w-2xl text-sm text-page-banner-foreground/85">{subtitle}</p>}
    </div>
  );

  if (!hasSideSlots) {
    return (
      <div className={cn('flex flex-col items-center bg-page-banner px-6 py-5 text-page-banner-foreground', className)}>{centerBlock}</div>
    );
  }

  return (
    <div
      className={cn(
        'grid grid-cols-[1fr_fit-content(42rem)_1fr] items-center gap-x-4 bg-page-banner px-4 py-5 text-page-banner-foreground sm:px-6',
        className,
      )}
    >
      <div className="flex items-center gap-3 justify-self-start">
        {backTo && (
          <>
            <Link
              to={backTo}
              title={backLabel}
              aria-label={backLabel}
              className="inline-flex items-center gap-1.5 rounded-md bg-page-banner-foreground/15 px-3 py-2 text-sm font-semibold text-page-banner-foreground hover:bg-page-banner-foreground/25"
            >
              <ArrowLeft className="h-4 w-4 shrink-0" />
              {/* Label only from xl: below that the sidebar leaves too little room beside a
                  centered title for a long label like "Back to Accounts and Finance". */}
              <span className="hidden whitespace-nowrap xl:inline">{backLabel}</span>
            </Link>
            <div className="hidden h-8 w-px bg-page-banner-foreground/30 xl:block" />
          </>
        )}
      </div>

      {centerBlock}

      <div className="flex flex-wrap items-center justify-end gap-2 justify-self-end">{rightActions}</div>
    </div>
  );
}
