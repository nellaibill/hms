import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { getHideableMenus } from '@/config/navigation';
import { cn } from '@/lib/utils';
import { ROLE_MODULES } from '../modules';
import type { ModulePermissions } from '../types';

interface MenuVisibilityCardProps {
  permissions: Record<string, ModulePermissions>;
  hiddenMenus: string[];
  onChange: (path: string, shown: boolean) => void;
  readOnly?: boolean;
}

const MODULE_LABELS = new Map(ROLE_MODULES.map((module) => [module.id, module.label]));

/**
 * The role's sidebar menus, one switch each (ADR-128). Turning a menu off hides it and blocks its
 * pages for this role without taking away the permission behind it, so other screens that need
 * that data keep working (e.g. OPD needs Clinical Care, which IPD and OT also use). Dashboard
 * and Settings are not listed: they can't be turned off.
 */
export function MenuVisibilityCard({ permissions, hiddenMenus, onChange, readOnly }: MenuVisibilityCardProps) {
  const hidden = new Set(hiddenMenus);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Menus</CardTitle>
        <CardDescription>
          Turn a menu off to hide it and its pages from this role. Its permission stays, so other screens that use the same data keep
          working. A menu also needs its permission to show.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-x-6 gap-y-1 sm:grid-cols-2">
          {getHideableMenus().map((menu) => {
            const Icon = menu.icon;
            const hasPermission = !menu.permission || (permissions[menu.permission]?.view ?? false);
            const shown = !hidden.has(menu.path);
            return (
              <div key={menu.path} className="flex items-center justify-between gap-3 border-b border-border py-2 last:border-b-0">
                <div className="flex min-w-0 items-center gap-3">
                  <Icon className={cn('h-4 w-4 shrink-0', menu.iconColor)} />
                  <div className="min-w-0">
                    <p className={cn('text-sm font-medium', hasPermission ? 'text-foreground' : 'text-muted-foreground')}>{menu.label}</p>
                    {!hasPermission && menu.permission && (
                      <p className="text-xs text-muted-foreground">Not shown: no {MODULE_LABELS.get(menu.permission) ?? menu.permission} view permission</p>
                    )}
                  </div>
                </div>
                <Switch
                  checked={shown}
                  onCheckedChange={(value) => onChange(menu.path, value)}
                  disabled={readOnly}
                  aria-label={`Show ${menu.label}`}
                />
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
