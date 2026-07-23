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

  const linkClasses = (isActive: boolean) =>
    cn(
      'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
      isActive
        ? 'bg-sidebar-accent text-primary'
        : 'text-sidebar-foreground/80 hover:bg-sidebar-accent hover:text-sidebar-foreground',
    );

  return (
    <nav className="flex flex-col gap-0.5 px-2">
      {nodes.map((node) => {
        if (node.type === 'leaf') {
          const Icon = node.icon;
          return (
            <NavLink
              key={node.path}
              to={node.path}
              onClick={onNavigate}
              className={({ isActive }) => linkClasses(isActive)}
              title={collapsed ? node.label : undefined}
            >
              <Icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span className="truncate">{node.label}</span>}
            </NavLink>
          );
        }

        return <SidebarGroup key={node.label} node={node} collapsed={collapsed} open={!!openGroups[node.label]} onToggle={() => toggleGroup(node.label)} onNavigate={onNavigate} linkClasses={linkClasses} />;
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
        className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-sidebar-foreground/80 transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground"
      >
        <Icon className="h-5 w-5 shrink-0" />
        <span className="flex-1 truncate text-left">{node.label}</span>
        <ChevronDown className={cn('h-4 w-4 shrink-0 transition-transform', open && 'rotate-180')} />
      </button>
      {open && (
        <div className="ml-4 flex flex-col gap-0.5 border-l border-sidebar-border pl-3">
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
