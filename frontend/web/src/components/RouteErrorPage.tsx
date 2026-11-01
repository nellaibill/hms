import { Loader2, RefreshCw, TriangleAlert } from 'lucide-react';
import { useEffect } from 'react';
import { isRouteErrorResponse, useRouteError } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { canReloadForNewBuild, isReloadingForNewBuild, isStaleBuildError, reloadForNewBuild } from '@/lib/staleBuild';

function errorMessage(error: unknown): string {
  if (isRouteErrorResponse(error)) return `${error.status} ${error.statusText}`;
  if (error instanceof Error) return error.message;
  return typeof error === 'string' ? error : 'Unknown error';
}

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-10">
      <div className="flex w-full max-w-md flex-col items-center gap-4 rounded-lg border border-border bg-card p-6 text-center shadow-sm">{children}</div>
    </div>
  );
}

/**
 * The router's error screen, in place of React Router's bare "Unexpected Application Error!".
 * A page file that failed to load because HMS was redeployed while the tab was open (see
 * lib/staleBuild.ts) reloads the page once to pick up the new version; if that was already tried
 * moments ago, it asks the user to reload. Any other error shows its message with Reload and Home.
 */
export function RouteErrorPage() {
  const error = useRouteError();
  const staleBuild = isStaleBuildError(error);
  const autoReload = staleBuild && (isReloadingForNewBuild() || canReloadForNewBuild());

  useEffect(() => {
    if (autoReload) reloadForNewBuild();
  }, [autoReload]);

  if (autoReload) {
    return (
      <Shell>
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
        <p className="text-sm text-muted-foreground">HMS was updated — loading the latest version…</p>
      </Shell>
    );
  }

  const reload = () => window.location.reload();

  if (staleBuild) {
    return (
      <Shell>
        <RefreshCw className="h-6 w-6 text-primary" />
        <div className="flex flex-col gap-1">
          <h1 className="text-lg font-semibold text-foreground">A new version of HMS is available</h1>
          <p className="text-sm text-muted-foreground">This page couldn’t load because HMS was updated while it was open. Reload to continue.</p>
        </div>
        <Button onClick={reload}>Reload</Button>
      </Shell>
    );
  }

  return (
    <Shell>
      <TriangleAlert className="h-6 w-6 text-destructive" />
      <div className="flex flex-col gap-1">
        <h1 className="text-lg font-semibold text-foreground">Something went wrong</h1>
        <p className="break-words text-sm text-muted-foreground">{errorMessage(error)}</p>
      </div>
      <div className="flex flex-wrap justify-center gap-2">
        <Button onClick={reload}>Reload</Button>
        <Button variant="outline" onClick={() => window.location.assign(import.meta.env.BASE_URL)}>
          Home
        </Button>
      </div>
    </Shell>
  );
}
