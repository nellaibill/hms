import { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { ChevronDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { filterNavigationForRole, findGroupForPath, type NavGroup } from '@/config/navigation';
import { useAuth } from '@/features/auth/AuthContext';

interface SidebarNavProps {
  collapsed?: boolean;
  onNavigate?: () => void;
}

export function SidebarNav({ collapsed = false, onNavigate }: SidebarNavProps) {
  const { user } = useAuth();
  const location = useLocation();
  const nodes = filterNavigationForRole(user?.role ?? 'admin');
  const activeGroup = findGroupForPath(location.pathname);

  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() =>
    activeGroup ? { [activeGroup.label]: true } : {},
  );

  const toggleGroup = (label: string) => setOpenGroups((prev) => ({ ...prev, [label]: !prev[label] }));

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

        const sectionHeader = showSectionHeader ? (
          <p
            key={`section-${node.section}`}
            className="mb-1 mt-4 truncate px-3 text-[11px] font-semibold uppercase tracking-wider text-sidebar-foreground/45 first:mt-1"
          >
            {node.section}
          </p>
        ) : null;

        if (node.type === 'leaf') {
          const Icon = node.icon;
          return (
            <div key={node.path}>
              {sectionHeader}
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
        }

        return (
          <div key={node.label}>
            {sectionHeader}
            <SidebarGroup
              node={node}
              collapsed={collapsed}
              open={!!openGroups[node.label]}
              onToggle={() => toggleGroup(node.label)}
              onNavigate={onNavigate}
              linkClasses={linkClasses}
            />
          </div>
        );
      })}
    </nav>
  );
}

function SidebarGroup({
  node,
  collapsed,
  open,
  onToggle,
  onNavigate,
  linkClasses,
}: {
  node: NavGroup;
  collapsed: boolean;
  open: boolean;
  onToggle: () => void;
  onNavigate?: () => void;
  linkClasses: (isActive: boolean) => string;
}) {
  const Icon = node.icon;
  const location = useLocation();
  const hasActiveChild = node.children.some((child) => child.path === location.pathname);

  if (collapsed) {
    return (
      <div className="flex flex-col gap-0.5">
        {node.children.map((child) => {
          const ChildIcon = child.icon;
          return (
            <NavLink key={child.path} to={child.path} onClick={onNavigate} className={({ isActive }) => linkClasses(isActive)} title={child.label}>
              <ChildIcon className="h-5 w-5 shrink-0" />
            </NavLink>
          );
        })}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-0.5">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={open}
        className={cn(
          'flex items-center gap-3 rounded-md border-l-[3px] border-transparent px-3 py-2 text-sm font-medium transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground',
          hasActiveChild ? 'text-sidebar-foreground' : 'text-sidebar-foreground/75',
        )}
      >
        <Icon className="h-5 w-5 shrink-0" />
        <span className="flex-1 truncate text-left">{node.label}</span>
        <ChevronDown className={cn('h-4 w-4 shrink-0 transition-transform', open && 'rotate-180')} />
      </button>
      {open && (
        <div className="ml-[18px] flex flex-col gap-0.5 border-l border-sidebar-border pl-2">
          {node.children.map((child) => {
            const ChildIcon = child.icon;
            return (
              <NavLink key={child.path} to={child.path} onClick={onNavigate} className={({ isActive }) => linkClasses(isActive)}>
                <ChildIcon className="h-4 w-4 shrink-0" />
                <span className="truncate">{child.label}</span>
              </NavLink>
            );
          })}
        </div>
      )}
    </div>
  );
}
