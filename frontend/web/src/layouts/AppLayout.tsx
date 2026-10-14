import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { AppSidebar } from '@/components/shell/AppSidebar';
import { TopHeader } from '@/components/shell/TopHeader';
import { AppFooter } from '@/components/shell/AppFooter';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';

// Assembles the six structural regions from docs/LayoutFramework.md, in a
// full-width-header-on-top / drawer-below arrangement (Material Design's
// "top app bar + permanent nav drawer" pattern) rather than a full-height
// sidebar beside a header that only spans the remaining width.
export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* The hospital's default print page (paper size, orientation, margin). Rendered before
          the page content, so a printout's own PrintPageStyle further down overrides it. */}
      <PrintPageStyle />
      <TopHeader />
      <div className="flex flex-1">
        <AppSidebar collapsed={collapsed} onToggleCollapse={() => setCollapsed((prev) => !prev)} />
        <div className="flex min-w-0 flex-1 flex-col">
          <main className="flex flex-1 flex-col">
            <Outlet />
          </main>
          <AppFooter />
        </div>
      </div>
    </div>
  );
}
