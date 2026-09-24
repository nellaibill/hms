import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { SidebarNav } from '@/components/shell/SidebarNav';
import { cn } from '@/lib/utils';

interface AppSidebarProps {
  collapsed: boolean;
  onToggleCollapse: () => void;
}

// Widths (80px collapsed / 280px expanded) — a Material "permanent drawer"
// sized a touch wider than the 256px spec default for breathing room.
// Sits below the full-width TopHeader (h-16 = 4rem) rather than beside it.
export function AppSidebar({ collapsed, onToggleCollapse }: AppSidebarProps) {
  return (
    <aside
      className={cn(
        'sticky top-16 hidden h-[calc(100vh-4rem)] shrink-0 flex-col border-r border-sidebar-border bg-sidebar shadow-soft transition-[width] duration-200 md:flex',
        collapsed ? 'w-20' : 'w-[280px]',
      )}
    >
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
