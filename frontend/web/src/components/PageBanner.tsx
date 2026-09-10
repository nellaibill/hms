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
 * given, renders inside this same bar at the far left rather than as a separate row above it —
 * absolutely positioned (like `rightActions` on the opposite edge) so neither ever shifts the
 * centered title block off the bar's true center, regardless of the back label's length or how
 * many right-side buttons there are.
 */
export function PageBanner({ icon: Icon, leading, title, subtitle, titleExtra, rightActions, backTo, backLabel = 'Back', className }: PageBannerProps) {
  return (
    <div
      className={cn(
        'relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground',
        className,
      )}
    >
      {backTo && (
        <div className="absolute left-4 top-1/2 flex -translate-y-1/2 items-center gap-3 sm:left-6">
          <Link
            to={backTo}
            className="inline-flex items-center gap-1.5 rounded-md bg-page-banner-foreground/15 px-3 py-2 text-sm font-semibold text-page-banner-foreground hover:bg-page-banner-foreground/25"
          >
            <ArrowLeft className="h-4 w-4" />
            <span className="hidden sm:inline">{backLabel}</span>
          </Link>
          <div className="hidden h-8 w-px bg-page-banner-foreground/30 sm:block" />
        </div>
      )}

      {rightActions && (
        <div className="absolute right-4 top-1/2 flex -translate-y-1/2 items-center gap-2 sm:right-6">{rightActions}</div>
      )}

      <div className="flex items-center gap-3">
        {leading ??
          (Icon && (
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <Icon className="h-5 w-5" />
            </span>
          ))}
        <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        {titleExtra}
      </div>
      {subtitle && <p className="max-w-2xl text-sm text-page-banner-foreground/85">{subtitle}</p>}
    </div>
  );
}
