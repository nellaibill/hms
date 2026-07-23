import { Suspense } from 'react';
import { Link, Outlet } from 'react-router-dom';

export function AppLayout() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/users" className="app-title">
          HMS
        </Link>
      </header>
      <main className="app-main">
        <Suspense fallback={<p>Loading…</p>}>
          <Outlet />
        </Suspense>
      </main>
    </div>
  );
}
