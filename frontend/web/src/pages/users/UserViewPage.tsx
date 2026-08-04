import { ApiError } from '@hms/shared';
import { ArrowLeft, Camera, KeyRound, Loader2, Pencil } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { env } from '@/config/env';
import {
  SetPasswordDialog,
  UploadProfilePhotoDialog,
  UserDetails,
  useSetPasswordMutation,
  useUploadProfilePhotoMutation,
  useUserQuery,
} from '../../features/users';

function initialsOf(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase();
}

export default function UserViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: user, isPending, isError } = useUserQuery(id);
  const [isSettingPassword, setIsSettingPassword] = useState(false);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const setPasswordMutation = useSetPasswordMutation();
  const uploadPhotoMutation = useUploadProfilePhotoMutation();

  function handleSetPassword(password: string) {
    if (!id) return;
    setPasswordMutation.mutate(
      { id, request: { password } },
      { onSuccess: () => setIsSettingPassword(false) },
    );
  }

  function handleUploadPhoto(file: File) {
    if (!id) return;
    uploadPhotoMutation.mutate({ id, file }, { onSuccess: () => setIsUploadingPhoto(false) });
  }

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading user…
      </div>
    );
  }

  if (isError || !user) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          User not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/users" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to users
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="absolute right-6 top-1/2 flex -translate-y-1/2 items-center gap-2">
          <Button
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
            onClick={() => setIsSettingPassword(true)}
          >
            <KeyRound className="h-4 w-4" />
            Set password
          </Button>
          <Button
            asChild
            variant="outline"
            className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`/users/${user.id}/edit`}>
              <Pencil className="h-4 w-4" />
              Edit
            </Link>
          </Button>
        </div>
        <div className="flex items-center gap-3">
          <div className="relative shrink-0">
            <Avatar className="h-12 w-12 border-2 border-page-banner-foreground/20">
              <AvatarImage
                src={user.profilePhotoUrl ? `${env.apiBaseUrl}/${user.profilePhotoUrl}` : undefined}
                alt=""
              />
              <AvatarFallback className="bg-page-banner-foreground/15 text-page-banner-foreground">
                {initialsOf(user.firstName, user.lastName)}
              </AvatarFallback>
            </Avatar>
            <button
              type="button"
              aria-label="Change profile photo"
              onClick={() => setIsUploadingPhoto(true)}
              className="absolute -bottom-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full border border-page-banner-foreground/30 bg-page-banner text-page-banner-foreground hover:bg-page-banner-foreground/20"
            >
              <Camera className="h-3 w-3" />
            </button>
          </div>
          <h1 className="text-xl font-semibold tracking-tight">
            {user.firstName} {user.lastName}
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">{user.email}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
      <UserDetails user={user} />
      </div>

      {isSettingPassword && (
        <SetPasswordDialog
          user={user}
          isSubmitting={setPasswordMutation.isPending}
          apiError={setPasswordMutation.error instanceof ApiError ? setPasswordMutation.error : null}
          onSubmit={handleSetPassword}
          onCancel={() => {
            setPasswordMutation.reset();
            setIsSettingPassword(false);
          }}
        />
      )}

      {isUploadingPhoto && (
        <UploadProfilePhotoDialog
          user={user}
          isSubmitting={uploadPhotoMutation.isPending}
          apiError={uploadPhotoMutation.error instanceof ApiError ? uploadPhotoMutation.error : null}
          onSubmit={handleUploadPhoto}
          onCancel={() => {
            uploadPhotoMutation.reset();
            setIsUploadingPhoto(false);
          }}
        />
      )}
    </div>
  );
}
