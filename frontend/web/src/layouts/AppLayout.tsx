import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { AppSidebar } from '@/components/shell/AppSidebar';
import { TopHeader } from '@/components/shell/TopHeader';
import { Breadcrumbs } from '@/components/shell/Breadcrumbs';
import { AppFooter } from '@/components/shell/AppFooter';

// Assembles the six structural regions from docs/LayoutFramework.md:
// Sidebar · Top Navigation · Breadcrumb · Page content · Footer.
export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <div className="flex min-h-screen bg-background">
      <AppSidebar collapsed={collapsed} onToggleCollapse={() => setCollapsed((prev) => !prev)} />
      <div className="flex min-h-screen flex-1 flex-col">
        <TopHeader />
        <Breadcrumbs />
        <main className="flex flex-1 flex-col">
          <Outlet />
        </main>
        <AppFooter />
      </div>
    </div>
  );
}
