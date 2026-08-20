import { useEffect, useState } from 'react';
import { ApiError, type TenantListItemResponse } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { featureLabel } from '../featureCatalog';
import { useHospitalFeaturesQuery } from '../hooks/useHospitalFeaturesQuery';
import { useUpdateHospitalFeaturesMutation } from '../hooks/useUpdateHospitalFeaturesMutation';

interface HospitalFeaturesDialogProps {
  hospital: TenantListItemResponse;
  onClose: () => void;
}

export function HospitalFeaturesDialog({ hospital, onClose }: HospitalFeaturesDialogProps) {
  const featuresQuery = useHospitalFeaturesQuery(hospital.id);
  const updateMutation = useUpdateHospitalFeaturesMutation();

  const [enabledFeatures, setEnabledFeatures] = useState<Set<string>>(new Set());

  // Seeds local editable state once the current features load — a ref-less "sync once"
  // since the dialog only ever mounts for one hospital at a time.
  useEffect(() => {
    if (featuresQuery.data) {
      setEnabledFeatures(new Set(featuresQuery.data.enabledFeatures));
    }
  }, [featuresQuery.data]);

  function toggleFeature(key: string) {
    setEnabledFeatures((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  function handleSave() {
    updateMutation.mutate(
      { id: hospital.id, request: { enabledFeatures: Array.from(enabledFeatures) } },
      { onSuccess: onClose },
    );
  }

  const apiError = updateMutation.error instanceof ApiError ? updateMutation.error.message : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="hospital-features-title">
        <DialogHeader>
          <DialogTitle id="hospital-features-title">Manage Features — {hospital.hospitalName}</DialogTitle>
          <DialogDescription>
            Controls which modules this hospital actually has — mandatory modules can't be disabled. Enabling a new
            module provisions its schema immediately; disabling one only revokes access, it never deletes data.
          </DialogDescription>
        </DialogHeader>

        {featuresQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}

        {featuresQuery.data && (
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label>Features</Label>
              <div className="grid grid-cols-2 gap-2">
                {featuresQuery.data.allFeatures.map((key: string) => {
                  const isMandatory = featuresQuery.data.mandatoryFeatures.includes(key);
                  return (
                    <label key={key} className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={isMandatory || enabledFeatures.has(key)}
                        disabled={isMandatory}
                        onChange={() => toggleFeature(key)}
                        className="h-3.5 w-3.5 rounded border-input text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                      />
                      {featureLabel(key)}
                      {isMandatory && <span className="text-xs text-muted-foreground">(required)</span>}
                    </label>
                  );
                })}
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
          <Button onClick={handleSave} disabled={!featuresQuery.data || updateMutation.isPending}>
            {updateMutation.isPending ? 'Saving…' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
