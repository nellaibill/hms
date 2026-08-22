import { ID_PROOF_TYPES, type IdProofType } from '@hms/shared';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

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
  /** Shown under the ID proof number field — surfaced by the parent only after a submit attempt finds it empty. */
  idProofNumberError?: string;
}

/**
 * Same fields as PatientDocumentUpload, but for the create flow — there's no patient id to
 * attach an upload to yet, so files are just held here and the caller
 * (PatientRegistrationCreatePage) uploads them right after the new patient is created.
 */
export function DocumentUploadStaging({ value, onChange, idProofNumberError }: DocumentUploadStagingProps) {
  function handlePhotoChange(event: React.ChangeEvent<HTMLInputElement>) {
    onChange({ ...value, photo: event.target.files?.[0] ?? null });
  }

  function handleIdProofChange(event: React.ChangeEvent<HTMLInputElement>) {
    onChange({ ...value, idProofFile: event.target.files?.[0] ?? null });
  }

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="photo-upload">Patient photo (JPG/PNG, max 5MB)</Label>
        <input
          id="photo-upload"
          type="file"
          accept="image/jpeg,image/png"
          onChange={handlePhotoChange}
          className="text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-primary-foreground hover:file:bg-primary/90"
        />
        {value.photo && <p className="text-xs text-muted-foreground">Selected: {value.photo.name}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="id-proof-type">ID proof type</Label>
        <Select value={value.idProofType} onValueChange={(type) => onChange({ ...value, idProofType: type as IdProofType })}>
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

        <Label htmlFor="id-proof-number" className="mt-2">
          {ID_PROOF_NUMBER_LABELS[value.idProofType]}
        </Label>
        <Input
          id="id-proof-number"
          className="w-56"
          value={value.idProofNumber}
          onChange={(event) => onChange({ ...value, idProofNumber: event.target.value })}
        />
        {idProofNumberError && (
          <p role="alert" className="text-xs text-destructive">
            {idProofNumberError}
          </p>
        )}

        <Label htmlFor="id-proof-upload" className="mt-2">
          ID proof file (JPG/PNG/PDF, max 5MB)
        </Label>
        <input
          id="id-proof-upload"
          type="file"
          accept="image/jpeg,image/png,application/pdf"
          onChange={handleIdProofChange}
          className="text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-primary-foreground hover:file:bg-primary/90"
        />
        {value.idProofFile && <p className="text-xs text-muted-foreground">Selected: {value.idProofFile.name}</p>}
      </div>
    </>
  );
}
