import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { filterNavigationForPermissions } from '@/config/navigation';
import { useAuth } from '@/features/auth/AuthContext';

interface SidebarNavProps {
  collapsed?: boolean;
  onNavigate?: () => void;
}

export function SidebarNav({ collapsed = false, onNavigate }: SidebarNavProps) {
  const { hasPermission } = useAuth();
  const nodes = filterNavigationForPermissions(hasPermission);

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
    <nav className="flex flex-col gap-0.5 px-2">
      {nodes.map((node) => {
        const showSectionHeader = !collapsed && node.section && node.section !== lastSection;
        lastSection = node.section ?? lastSection;

        const Icon = node.icon;

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
              className={({ isActive }) => linkClasses(isActive)}
              title={collapsed ? node.label : undefined}
            >
              <Icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span className="truncate">{node.label}</span>}
            </NavLink>
          </div>
        );
      })}
    </nav>
  );
}
