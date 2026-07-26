import { Card, CardContent, CardHeader, CardDescription, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { ROLE_MODULES } from '../modules';
import { PERMISSION_ACTIONS, PERMISSION_ACTION_LABELS, type ModulePermissions, type PermissionAction } from '../types';

interface PermissionMatrixProps {
  permissions: Record<string, ModulePermissions>;
  onChange: (moduleId: string, action: PermissionAction, value: boolean) => void;
  readOnly?: boolean;
}

/** Modules on the left, permission actions as columns on the right — one toggle per cell instead of a checkbox wall. */
export function PermissionMatrix({ permissions, onChange, readOnly }: PermissionMatrixProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Permissions</CardTitle>
        <CardDescription>Toggle what this role can view, create, edit, or delete in each module.</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2.5">Module</th>
                {PERMISSION_ACTIONS.map((action) => (
                  <th key={action} className="px-4 py-2.5 text-center">
                    {PERMISSION_ACTION_LABELS[action]}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {ROLE_MODULES.map((module) => {
                const modulePerms = permissions[module.id];
                return (
                  <tr key={module.id} className="hover:bg-muted/30">
                    <td className="px-4 py-2.5 font-medium text-foreground">{module.label}</td>
                    {PERMISSION_ACTIONS.map((action) => (
                      <td key={action} className="px-4 py-2.5 text-center">
                        <Switch
                          checked={modulePerms?.[action] ?? false}
                          onCheckedChange={(value) => onChange(module.id, action, value)}
                          disabled={readOnly}
                          aria-label={`${module.label} — ${PERMISSION_ACTION_LABELS[action]}`}
                        />
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
