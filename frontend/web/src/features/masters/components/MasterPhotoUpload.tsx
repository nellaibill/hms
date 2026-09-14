import { Camera, User as UserIcon } from 'lucide-react';
import { useRef, useState } from 'react';
import { env } from '@/config/env';
import { validateUploadFile } from '@/lib/fileValidation';
import { useUploadMasterPhotoMutation } from '../hooks/useMasterMutations';
import type { MasterEntityConfig } from '../engine/types';

const MAX_PHOTO_SIZE_BYTES = 2 * 1024 * 1024;

interface MasterPhotoUploadProps {
  config: MasterEntityConfig;
  mode: 'create' | 'edit' | 'view';
  recordId?: string;
  /** The record's current stored photo path (config.photo.urlField's value), if any. */
  initialUrl?: string | null;
  /** Create mode only — reports the picked file so MasterForm can upload it once the record
   * actually exists (there's no id to attach to yet). Ignored in edit/view mode, where this
   * widget uploads immediately on its own instead. */
  onFileStaged?: (file: File | null) => void;
}

/**
 * A single photo upload/preview widget — see MasterEntityConfig.photo's own doc comment for
 * when this renders and why create vs. edit mode behave differently. Modeled directly on
 * Users' own profile-photo upload (UploadProfilePhotoDialog.tsx): same validation constants,
 * same "upload immediately, independent of the surrounding form's Save button" behavior in
 * edit mode, just inline here instead of a separate dialog (to match this feature's own
 * screenshot reference, which shows the photo inline in the form).
 */
export function MasterPhotoUpload({ config, mode, recordId, initialUrl, onFileStaged }: MasterPhotoUploadProps) {
  const photoConfig = config.photo;
  const inputRef = useRef<HTMLInputElement>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null | undefined>(initialUrl);
  const [stagedObjectUrl, setStagedObjectUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const uploadMutation = useUploadMasterPhotoMutation(config.key);
  const readOnly = mode === 'view';

  if (!photoConfig) {
    return null;
  }

  async function handleFileSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    setError(null);
    const validationError = await validateUploadFile(file, ['jpeg', 'png'], MAX_PHOTO_SIZE_BYTES);
    if (validationError) {
      setError(validationError);
      return;
    }

    if (mode === 'create' || !recordId) {
      // No record to attach to yet — stage a local preview and hand the file up to MasterForm,
      // which uploads it once the create call this file is part of actually succeeds.
      setStagedObjectUrl((previous) => {
        if (previous) URL.revokeObjectURL(previous);
        return URL.createObjectURL(file);
      });
      onFileStaged?.(file);
      return;
    }

    uploadMutation.mutate(
      { id: recordId, file },
      {
        onSuccess: (updated) => setPreviewUrl((updated[photoConfig!.urlField] as string | null | undefined) ?? null),
        onError: () => setError('Failed to upload photo — please try again.'),
      },
    );
  }

  const displayUrl = stagedObjectUrl ?? (previewUrl ? `${env.apiBaseUrl}/${previewUrl}` : undefined);

  return (
    <div className="flex w-36 shrink-0 flex-col items-center gap-2 text-center">
      <div className="flex h-28 w-28 items-center justify-center overflow-hidden rounded-lg border border-border bg-muted">
        {displayUrl ? (
          <img src={displayUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <UserIcon className="h-10 w-10 text-muted-foreground" />
        )}
      </div>

      {!readOnly && (
        <>
          <input ref={inputRef} type="file" accept="image/jpeg,image/png" className="sr-only" onChange={handleFileSelected} />
          <button
            type="button"
            disabled={uploadMutation.isPending}
            onClick={() => inputRef.current?.click()}
            className="inline-flex items-center gap-1.5 rounded-md border border-input bg-background px-3 py-1.5 text-xs font-medium text-foreground shadow-sm transition-colors hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Camera className="h-3.5 w-3.5" />
            {uploadMutation.isPending ? 'Uploading…' : 'Change Photo'}
          </button>
          <p className="text-xs text-muted-foreground">{photoConfig.helpText ?? 'JPG, PNG (Max 2MB)'}</p>
          {error && <p className="text-xs text-destructive">{error}</p>}
        </>
      )}
    </div>
  );
}
