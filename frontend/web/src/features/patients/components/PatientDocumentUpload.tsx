import { ID_PROOF_TYPES, type IdProofType } from '@hms/shared';
import { CheckCircle2 } from 'lucide-react';
import { useState } from 'react';
import { FileChooserButton } from '@/components/ui/file-chooser-button';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { validateUploadFile } from '@/lib/fileValidation';
import { useUploadPatientIdProofMutation, useUploadPatientPhotoMutation } from '../hooks/usePatientMutations';
import { MAX_PATIENT_DOCUMENT_SIZE_BYTES, MAX_PATIENT_DOCUMENT_SIZE_MB } from '../documentUploadLimits';

interface PatientDocumentUploadProps {
  patientId: string;
  /** Skip the component's own card border + heading — use when it's already inside a FormSection (the registration/edit forms). */
  bare?: boolean;
}

/**
 * Plain file pickers for Photo and ID Proof (docs/PatientRegistrationModule.md §12) — no
 * drag-drop zone, no webcam capture (§13 is deferred, see docs/DecisionLog.md). Uploaded
 * immediately on selection (unlike DocumentUploadStaging's create-flow deferral, this always
 * has a real patient id to attach to).
 *
 * The chosen filename is tracked here in state rather than read back off a native file
 * input's own display — see FileChooserButton's own doc comment for why that native text
 * can't be trusted once the input is reset for re-selection.
 */
export function PatientDocumentUpload({ patientId, bare = false }: PatientDocumentUploadProps) {
  const [idProofType, setIdProofType] = useState<IdProofType>('Aadhaar');
  const [photoFileName, setPhotoFileName] = useState<string | null>(null);
  const [idProofFileName, setIdProofFileName] = useState<string | null>(null);
  const [photoValidationError, setPhotoValidationError] = useState<string | null>(null);
  const [idProofValidationError, setIdProofValidationError] = useState<string | null>(null);
  const photoMutation = useUploadPatientPhotoMutation();
  const idProofMutation = useUploadPatientIdProofMutation();

  async function handlePhotoChange(file: File) {
    const error = await validateUploadFile(file, ['jpeg', 'png'], MAX_PATIENT_DOCUMENT_SIZE_BYTES);
    setPhotoValidationError(error);
    if (error) return;
    setPhotoFileName(file.name);
    photoMutation.mutate({ id: patientId, file });
  }

  async function handleIdProofChange(file: File) {
    const error = await validateUploadFile(file, ['jpeg', 'png', 'pdf'], MAX_PATIENT_DOCUMENT_SIZE_BYTES);
    setIdProofValidationError(error);
    if (error) return;
    setIdProofFileName(file.name);
    idProofMutation.mutate({ id: patientId, file });
  }

  const photoStatus =
    photoFileName && !photoValidationError ? (
      <>
        {photoMutation.isPending && `Uploading ${photoFileName}…`}
        {photoMutation.isSuccess && (
          <span className="inline-flex items-center gap-1 text-success">
            <CheckCircle2 className="h-3.5 w-3.5" />
            Uploaded {photoFileName}
          </span>
        )}
        {photoMutation.isIdle && `Selected: ${photoFileName}`}
      </>
    ) : undefined;

  const idProofStatus =
    idProofFileName && !idProofValidationError ? (
      <>
        {idProofMutation.isPending && `Uploading ${idProofFileName}…`}
        {idProofMutation.isSuccess && (
          <span className="inline-flex items-center gap-1 text-success">
            <CheckCircle2 className="h-3.5 w-3.5" />
            Uploaded {idProofFileName}
          </span>
        )}
        {idProofMutation.isIdle && `Selected: ${idProofFileName}`}
      </>
    ) : undefined;

  const fields = (
    <>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="photo-upload">Patient photo (JPG/PNG, max {MAX_PATIENT_DOCUMENT_SIZE_MB}MB)</Label>
        <FileChooserButton
          id="photo-upload"
          accept="image/jpeg,image/png"
          disabled={photoMutation.isPending}
          onFileSelected={handlePhotoChange}
          status={photoStatus}
        />
        {photoValidationError && <p className="text-sm text-destructive">{photoValidationError}</p>}
        {photoMutation.isError && <p className="text-sm text-destructive">Failed to upload {photoFileName ?? 'photo'} — please try again.</p>}
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
          ID proof file (JPG/PNG/PDF, max {MAX_PATIENT_DOCUMENT_SIZE_MB}MB)
        </Label>
        <FileChooserButton
          id="id-proof-upload"
          accept="image/jpeg,image/png,application/pdf"
          disabled={idProofMutation.isPending}
          onFileSelected={handleIdProofChange}
          status={idProofStatus}
        />
        {idProofValidationError && <p className="text-sm text-destructive">{idProofValidationError}</p>}
        {idProofMutation.isError && (
          <p className="text-sm text-destructive">Failed to upload {idProofFileName ?? 'ID proof'} — please try again.</p>
        )}
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
