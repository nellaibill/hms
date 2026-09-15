import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { filterNavigationForPermissions } from '@/config/navigation';
import { useAuth } from '@/features/auth/AuthContext';

interface SidebarNavProps {
  collapsed?: boolean;
  onNavigate?: () => void;
}

export function SidebarNav({ collapsed = false, onNavigate }: SidebarNavProps) {
  const { hasPermission, hasFeature } = useAuth();
  const nodes = filterNavigationForPermissions(hasPermission, hasFeature);

  // Every row gets a 3px transparent left border by default so the active
  // row's accent bar doesn't shift layout when it appears — per
  // docs/DesignSystem.md's Status Chips/active-indicator convention.
  const linkClasses = (isActive: boolean) =>
    cn(
      'flex items-center gap-3 rounded-r-md rounded-l-sm border-l-[3px] px-3 py-2 text-sm font-medium transition-colors',
      isActive
        ? 'border-primary bg-sidebar-active text-sidebar-active-foreground'
        : 'border-transparent text-sidebar-foreground/75 hover:bg-sidebar-accent hover:text-sidebar-foreground',
    );

  let lastSection: string | undefined;

  return (
    <nav className="flex flex-col gap-1 px-2">
      {nodes.map((node, index) => {
        const showSectionHeader = !collapsed && node.section && node.section !== lastSection;
        lastSection = node.section ?? lastSection;

        const Icon = node.icon;
        // Odd/even rows (1-based) get their own admin-configurable background — Theme &
        // Branding's Left nav tab (--sidebar-odd-bg/--sidebar-even-bg) — for a clean striped
        // appearance. Skipped on the active row so it doesn't compete with that row's own
        // highlight/border-accent treatment.
        const isAlternate = index % 2 === 1;

        return (
          <div key={node.path}>
            {showSectionHeader && (
              <p className="mb-1 mt-4 truncate px-3 text-[11px] font-semibold uppercase tracking-wider text-sidebar-foreground/45 first:mt-1">
                {node.section}
              </p>
            )}
            <NavLink
              to={node.path}
              onClick={onNavigate}
              className={({ isActive }) => cn(linkClasses(isActive), !isActive && (isAlternate ? 'bg-sidebar-even' : 'bg-sidebar-odd'))}
              title={collapsed ? node.label : undefined}
            >
              {({ isActive }) => (
                <>
                  <Icon className={cn('h-5 w-5 shrink-0', !isActive && node.iconColor)} />
                  {!collapsed && <span className="truncate">{node.label}</span>}
                </>
              )}
            </NavLink>
          </div>
        );
      })}
    </nav>
  );
}
