import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LAB_SAMPLE_TYPES, type CollectSampleRequest, type LabSampleType } from '../types';

interface SampleCollectionDialogProps {
  testName: string;
  /** Same dialog/action for a fresh PendingCollection item and a RecollectionRequired one —
   * the backend allows collect-sample from either status. */
  isRecollection: boolean;
  isSaving: boolean;
  onConfirm: (request: CollectSampleRequest) => void;
  onCancel: () => void;
}

export function SampleCollectionDialog({ testName, isRecollection, isSaving, onConfirm, onCancel }: SampleCollectionDialogProps) {
  const [sampleType, setSampleType] = useState<LabSampleType>('Blood');
  const [location, setLocation] = useState('');
  const [quantity, setQuantity] = useState('');
  const [remarks, setRemarks] = useState('');

  function handleConfirm() {
    onConfirm({
      sampleType,
      location: location.trim() || null,
      quantity: quantity.trim() || null,
      remarks: remarks.trim() || null,
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="collect-sample-title">
        <DialogHeader>
          <DialogTitle id="collect-sample-title">{isRecollection ? 'Recollect sample' : 'Collect sample'}</DialogTitle>
          <DialogDescription>
            Record sample collection for <strong className="text-foreground">{testName}</strong>.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="sample-type">Sample type</Label>
            <Select value={sampleType} onValueChange={(value) => setSampleType(value as LabSampleType)}>
              <SelectTrigger id="sample-type">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {LAB_SAMPLE_TYPES.map((type) => (
                  <SelectItem key={type} value={type}>
                    {type}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="sample-location">Collection location</Label>
            <Input id="sample-location" value={location} onChange={(event) => setLocation(event.target.value)} placeholder="e.g. Ward 3, OPD-2" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="sample-quantity">Quantity</Label>
            <Input id="sample-quantity" value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder="e.g. 5 mL" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="sample-remarks">Remarks</Label>
            <Input id="sample-remarks" value={remarks} onChange={(event) => setRemarks(event.target.value)} placeholder="Optional" />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} disabled={isSaving}>
            {isSaving ? 'Saving…' : isRecollection ? 'Record Recollection' : 'Record Collection'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
