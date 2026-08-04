import { ApiError, type User } from '@hms/shared';
import { useEffect, useState } from 'react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { env } from '@/config/env';

// Mirrors HMS.Modules.Identity.Application.UserService's AllowedProfilePhotoContentTypes /
// MaxProfilePhotoSizeBytes — client-side convenience only, the backend remains authoritative.
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_SIZE_BYTES = 2 * 1024 * 1024;

interface UploadProfilePhotoDialogProps {
  user: User;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (file: File) => void;
  onCancel: () => void;
}

function initialsOf(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase();
}

export function UploadProfilePhotoDialog({ user, isSubmitting, apiError, onSubmit, onCancel }: UploadProfilePhotoDialogProps) {
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  // Object URLs are only valid until revoked — release the previous one whenever the
  // selected file changes or the dialog unmounts, so we don't leak blob URLs.
  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    };
  }, [previewUrl]);

  function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const selected = event.target.files?.[0];
    if (!selected) return;

    if (!ALLOWED_TYPES.includes(selected.type)) {
      setValidationError('Photo must be a JPG, PNG, or WEBP file.');
      setFile(null);
      return;
    }
    if (selected.size > MAX_SIZE_BYTES) {
      setValidationError('Photo must be 2MB or smaller.');
      setFile(null);
      return;
    }

    setValidationError(null);
    setFile(selected);
    setPreviewUrl((previous) => {
      if (previous) URL.revokeObjectURL(previous);
      return URL.createObjectURL(selected);
    });
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!file) {
      setValidationError('Choose a photo to upload.');
      return;
    }
    onSubmit(file);
  }

  const generalError = validationError ?? apiError?.message ?? null;
  const currentPhotoUrl = user.profilePhotoUrl ? `${env.apiBaseUrl}/${user.profilePhotoUrl}` : undefined;

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="upload-photo-title">
        <DialogHeader>
          <DialogTitle id="upload-photo-title">Change profile photo</DialogTitle>
          <DialogDescription>
            Upload a new photo for{' '}
            <strong className="text-foreground">
              {user.firstName} {user.lastName}
            </strong>{' '}
            ({user.username}). JPG, PNG, or WEBP, max 2MB.
          </DialogDescription>
        </DialogHeader>

        <form id="upload-photo-form" onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
          {generalError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {generalError}
            </p>
          )}

          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16">
              <AvatarImage src={previewUrl ?? currentPhotoUrl} alt="" />
              <AvatarFallback className="text-base">{initialsOf(user.firstName, user.lastName)}</AvatarFallback>
            </Avatar>

            <div className="flex flex-1 flex-col gap-1.5">
              <Label htmlFor="profile-photo">Photo file</Label>
              <Input id="profile-photo" type="file" accept="image/jpeg,image/png,image/webp" onChange={handleFileChange} />
            </div>
          </div>
        </form>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" form="upload-photo-form" disabled={isSubmitting}>
            {isSubmitting ? 'Uploading…' : 'Upload photo'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
