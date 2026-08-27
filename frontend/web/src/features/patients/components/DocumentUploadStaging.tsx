import { ID_PROOF_TYPES, type IdProofType } from '@hms/shared';
import { useState } from 'react';
import { FileChooserButton } from '@/components/ui/file-chooser-button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { validateUploadFile } from '@/lib/fileValidation';
import { Field } from './FormSection';
import { MAX_PATIENT_DOCUMENT_SIZE_BYTES, MAX_PATIENT_DOCUMENT_SIZE_MB } from '../documentUploadLimits';

export interface StagedDocuments {
  photo: File | null;
  idProofType: IdProofType;
  idProofNumber: string;
  idProofFile: File | null;
}

export const emptyStagedDocuments: StagedDocuments = { photo: null, idProofType: 'Aadhaar', idProofNumber: '', idProofFile: null };

// The number field's label follows whichever ID proof type is selected, so the receptionist
// is asked for "Passport number" vs. "Driving License number" rather than one generic label
// that doesn't say which document's number is wanted.
const ID_PROOF_NUMBER_LABELS: Record<IdProofType, string> = {
  Aadhaar: 'Aadhaar number',
  Passport: 'Passport number',
  DrivingLicense: 'Driving License number',
  VoterId: 'Voter ID number',
  Other: 'ID number',
};

interface DocumentUploadStagingProps {
  value: StagedDocuments;
  onChange: (value: StagedDocuments) => void;
}

/**
 * Same fields as PatientDocumentUpload, but for the create flow — there's no patient id to
 * attach an upload to yet, so files are just held here and the caller
 * (PatientRegistrationCreatePage) uploads them right after the new patient is created.
 * ID proof number's own validation errors surface only via the tab-level error summary (see
 * idProofNumberError in PatientRegistrationForm) — not inline here, consistent with every
 * other field on this wizard. A bad photo/ID-proof file *is* shown inline (see
 * photoError/idProofFileError below) since there's no equivalent tab-level summary field for
 * it to fold into, and rejecting it here (never staging it) is what stops it from ever
 * reaching the upload call this component's caller makes after the patient is created.
 */
export function DocumentUploadStaging({ value, onChange }: DocumentUploadStagingProps) {
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [idProofFileError, setIdProofFileError] = useState<string | null>(null);

  async function handlePhotoChange(file: File) {
    const error = await validateUploadFile(file, ['jpeg', 'png'], MAX_PATIENT_DOCUMENT_SIZE_BYTES);
    setPhotoError(error);
    if (!error) {
      onChange({ ...value, photo: file });
    }
  }

  async function handleIdProofChange(file: File) {
    const error = await validateUploadFile(file, ['jpeg', 'png', 'pdf'], MAX_PATIENT_DOCUMENT_SIZE_BYTES);
    setIdProofFileError(error);
    if (!error) {
      onChange({ ...value, idProofFile: file });
    }
  }

  return (
    <>
      <Field
        label={`Patient photo (JPG/PNG, max ${MAX_PATIENT_DOCUMENT_SIZE_MB}MB)`}
        htmlFor="photo-upload"
        error={photoError ?? undefined}
        className="flex min-w-[220px] max-w-sm flex-col gap-1.5"
      >
        <FileChooserButton
          id="photo-upload"
          accept="image/jpeg,image/png"
          onFileSelected={handlePhotoChange}
          status={value.photo && !photoError ? `Selected: ${value.photo.name}` : undefined}
        />
      </Field>

      <div className="flex flex-wrap gap-3">
        <Field label="ID proof type" htmlFor="id-proof-type" className="flex w-full flex-col gap-1 sm:w-48">
          <Select value={value.idProofType} onValueChange={(type) => onChange({ ...value, idProofType: type as IdProofType })}>
            <SelectTrigger id="id-proof-type" aria-label="ID proof type">
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
        </Field>

        <Field
          label={ID_PROOF_NUMBER_LABELS[value.idProofType]}
          htmlFor="id-proof-number"
          className="flex min-w-[160px] flex-1 flex-col gap-1"
        >
          <Input id="id-proof-number" value={value.idProofNumber} onChange={(event) => onChange({ ...value, idProofNumber: event.target.value })} />
        </Field>

        <Field
          label={`ID proof file (JPG/PNG/PDF, max ${MAX_PATIENT_DOCUMENT_SIZE_MB}MB)`}
          htmlFor="id-proof-upload"
          error={idProofFileError ?? undefined}
          className="flex min-w-[220px] flex-1 flex-col gap-1"
        >
          <FileChooserButton
            id="id-proof-upload"
            accept="image/jpeg,image/png,application/pdf"
            onFileSelected={handleIdProofChange}
            status={value.idProofFile && !idProofFileError ? `Selected: ${value.idProofFile.name}` : undefined}
          />
        </Field>
      </div>
    </>
  );
}
