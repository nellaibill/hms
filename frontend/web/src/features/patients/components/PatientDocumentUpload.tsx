import { ID_PROOF_TYPES, type IdProofType } from '@hms/shared';
import { useState } from 'react';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useUploadPatientIdProofMutation, useUploadPatientPhotoMutation } from '../hooks/usePatientMutations';

interface PatientDocumentUploadProps {
  patientId: string;
  /** Skip the component's own card border + heading — use when it's already inside a FormSection (the registration/edit forms). */
  bare?: boolean;
}

/**
 * Plain file inputs for Photo and ID Proof (docs/PatientRegistrationModule.md §12) — no
 * drag-drop zone, no webcam capture (§13 is deferred, see docs/DecisionLog.md). Uploaded
 * only after the patient record exists, since both endpoints attach to a patient id.
 */
export function PatientDocumentUpload({ patientId, bare = false }: PatientDocumentUploadProps) {
  const [idProofType, setIdProofType] = useState<IdProofType>('Aadhaar');
  const photoMutation = useUploadPatientPhotoMutation();
  const idProofMutation = useUploadPatientIdProofMutation();

  function handlePhotoChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) {
      photoMutation.mutate({ id: patientId, file });
    }
    event.target.value = '';
  }

  function handleIdProofChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) {
      idProofMutation.mutate({ id: patientId, idProofType, file });
    }
    event.target.value = '';
  }

  const fields = (
    <>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="photo-upload">Patient photo (JPG/PNG, max 5MB)</Label>
        <input
          id="photo-upload"
          type="file"
          accept="image/jpeg,image/png"
          onChange={handlePhotoChange}
          disabled={photoMutation.isPending}
          className="text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-primary-foreground hover:file:bg-primary/90"
        />
        {photoMutation.isPending && <p className="text-xs text-muted-foreground">Uploading…</p>}
        {photoMutation.isError && <p className="text-sm text-destructive">Failed to upload photo.</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="id-proof-type">ID proof type</Label>
        <Select value={idProofType} onValueChange={(value) => setIdProofType(value as IdProofType)}>
          <SelectTrigger id="id-proof-type" className="w-56" aria-label="ID proof type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {ID_PROOF_TYPES.map((type) => (
              <SelectItem key={type} value={type}>
                {type}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Label htmlFor="id-proof-upload" className="mt-2">
          ID proof file (JPG/PNG/PDF, max 5MB)
        </Label>
        <input
          id="id-proof-upload"
          type="file"
          accept="image/jpeg,image/png,application/pdf"
          onChange={handleIdProofChange}
          disabled={idProofMutation.isPending}
          className="text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-primary-foreground hover:file:bg-primary/90"
        />
        {idProofMutation.isPending && <p className="text-xs text-muted-foreground">Uploading…</p>}
        {idProofMutation.isError && <p className="text-sm text-destructive">Failed to upload ID proof.</p>}
      </div>
    </>
  );

  if (bare) {
    return <div className="flex flex-col gap-4">{fields}</div>;
  }

  return (
    <div className="flex flex-col gap-4 rounded-lg border border-border p-4">
      <h2 className="text-sm font-semibold text-foreground">Document Upload</h2>
      {fields}
    </div>
  );
}
