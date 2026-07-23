import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { HospitalLogo } from '@/components/shell/HospitalLogo';
import { SidebarNav } from '@/components/shell/SidebarNav';
import { cn } from '@/lib/utils';

interface AppSidebarProps {
  collapsed: boolean;
  onToggleCollapse: () => void;
}

// Widths (72px collapsed / 240px expanded) follow docs/LayoutFramework.md §2.
export function AppSidebar({ collapsed, onToggleCollapse }: AppSidebarProps) {
  return (
    <aside
      className={cn(
        'sticky top-0 hidden h-screen shrink-0 flex-col border-r border-sidebar-border bg-sidebar shadow-soft transition-[width] duration-200 md:flex',
        collapsed ? 'w-[72px]' : 'w-[240px]',
      )}
    >
      <div className={cn('flex h-14 items-center border-b border-sidebar-border px-3', collapsed && 'justify-center px-0')}>
        <HospitalLogo showName={!collapsed} />
      </div>

      <ScrollArea className="flex-1 py-3">
        <SidebarNav collapsed={collapsed} />
      </ScrollArea>

      <Separator />
      <div className="p-2">
        <Button variant="ghost" size="sm" className="w-full justify-center" onClick={onToggleCollapse} aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}>
          {collapsed ? <ChevronsRight className="h-4 w-4" /> : <ChevronsLeft className="h-4 w-4" />}
        </Button>
      </div>
    </aside>
  );
}
