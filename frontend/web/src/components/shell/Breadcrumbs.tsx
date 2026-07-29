import { Link, useLocation } from 'react-router-dom';
import { ChevronRight, Home } from 'lucide-react';
import { findLeafByPath } from '@/config/navigation';
import { cn } from '@/lib/utils';

// /users* isn't in the nav tree (it's reachable via the Settings page's "User
// Accounts" card, not the sidebar itself — see routes.tsx), so its breadcrumb
// trail is defined explicitly here rather than derived from the nav config.
function usersTrailSegments(pathname: string): string[] | undefined {
  if (pathname === '/users') return ['Settings', 'Users'];
  if (pathname === '/users/new') return ['Settings', 'Users', 'New'];
  if (/^\/users\/[^/]+\/edit$/.test(pathname)) return ['Settings', 'Users', 'Edit'];
  if (/^\/users\/[^/]+$/.test(pathname)) return ['Settings', 'Users', 'Details'];
  return undefined;
}

// Location-based breadcrumb (Home > Module) per docs/InformationArchitecture.md
// §Breadcrumb Strategy — reflects the nav tree, never the click history.
export function Breadcrumbs() {
  const location = useLocation();
  const leaf = findLeafByPath(location.pathname);
  const usersTrail = usersTrailSegments(location.pathname);

  if (!leaf && !usersTrail) return null;

  const isHome = leaf?.path === '/dashboard';
  const trail = usersTrail ?? [leaf!.label];

  return (
    <nav aria-label="Breadcrumb" className="flex h-8 items-center gap-1.5 border-b border-border px-6 text-sm text-muted-foreground">
      <Link
        to="/dashboard"
        aria-current={isHome ? 'page' : undefined}
        className={cn('flex items-center gap-1 transition-colors hover:text-primary', isHome && 'font-medium text-foreground')}
      >
        <Home className="h-3.5 w-3.5" />
        Home
      </Link>
      {!isHome &&
        trail.map((segment, index) => (
          <span key={segment} className="flex items-center gap-1.5">
            <ChevronRight className="h-3.5 w-3.5 text-muted-foreground/50" />
            <span
              aria-current={index === trail.length - 1 ? 'page' : undefined}
              className={index === trail.length - 1 ? 'font-medium text-foreground' : undefined}
            >
              {segment}
            </span>
          </span>
        ))}
    </nav>
  );
}
