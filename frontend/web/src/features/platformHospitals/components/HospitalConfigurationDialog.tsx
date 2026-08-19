import { useEffect, useState } from 'react';
import { ApiError, type TenantListItemResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ROLE_MODULES } from '@/features/roles/modules';
import { useHospitalConfigurationQuery } from '../hooks/useHospitalConfigurationQuery';
import { useUpdateHospitalConfigurationMutation } from '../hooks/useUpdateHospitalConfigurationMutation';

interface HospitalConfigurationDialogProps {
  hospital: TenantListItemResponse;
  onClose: () => void;
}

export function HospitalConfigurationDialog({ hospital, onClose }: HospitalConfigurationDialogProps) {
  const configQuery = useHospitalConfigurationQuery(hospital.id);
  const updateMutation = useUpdateHospitalConfigurationMutation();

  const [enabledModules, setEnabledModules] = useState<Set<string>>(new Set());
  const [subscriptionTier, setSubscriptionTier] = useState('');

  // Seeds local editable state once the current configuration loads — a ref-less "sync once"
  // since the dialog only ever mounts for one hospital at a time (key={hospital.id} at the
  // call site would also work, but this avoids re-fetching on every keystroke).
  useEffect(() => {
    if (configQuery.data) {
      setEnabledModules(new Set(configQuery.data.enabledModules));
      setSubscriptionTier(configQuery.data.subscriptionTier);
    }
  }, [configQuery.data]);

  function toggleModule(moduleId: string) {
    setEnabledModules((current) => {
      const next = new Set(current);
      if (next.has(moduleId)) {
        next.delete(moduleId);
      } else {
        next.add(moduleId);
      }
      return next;
    });
  }

  function handleSave() {
    updateMutation.mutate(
      { id: hospital.id, request: { enabledModules: Array.from(enabledModules), subscriptionTier: subscriptionTier.trim() } },
      { onSuccess: onClose },
    );
  }

  const apiError = updateMutation.error instanceof ApiError ? updateMutation.error.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="hospital-configuration-title">
        <DialogHeader>
          <DialogTitle id="hospital-configuration-title">Configure {hospital.hospitalName}</DialogTitle>
          <DialogDescription>
            Controls which modules this hospital's staff can use, independent of any individual user's role. Takes
            effect the next time each user signs in.
          </DialogDescription>
        </DialogHeader>

        {configQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}

        {configQuery.data && (
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="subscription-tier">Subscription tier</Label>
              <Input
                id="subscription-tier"
                value={subscriptionTier}
                onChange={(event) => setSubscriptionTier(event.target.value)}
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label>Enabled modules</Label>
              <div className="grid grid-cols-2 gap-2">
                {ROLE_MODULES.map((module) => (
                  <label key={module.id} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={enabledModules.has(module.id)}
                      onChange={() => toggleModule(module.id)}
                      className="h-3.5 w-3.5 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    />
                    {module.label}
                  </label>
                ))}
              </div>
            </div>

            {apiError && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {apiError}
              </p>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={updateMutation.isPending}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={!configQuery.data || !subscriptionTier.trim() || updateMutation.isPending}>
            {updateMutation.isPending ? 'Saving…' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
