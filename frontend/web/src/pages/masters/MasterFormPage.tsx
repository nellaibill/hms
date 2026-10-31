import { ApiError } from '@hms/shared';
import { Loader2, Pencil } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import {
  LoginAccessCard,
  loginErrorsFromApiError,
  useConsultantLoginQuery,
  useLoginAccessState,
  useSaveConsultantLoginMutation,
} from '@/features/users';
import {
  getDisplayLabel,
  getMasterConfig,
  MasterForm,
  useCreateMasterMutation,
  useMasterQuery,
  useUpdateMasterMutation,
  useUploadMasterPhotoMutation,
} from '@/features/masters';

interface MasterFormPageProps {
  mode: 'create' | 'edit' | 'view';
}

export default function MasterFormPage({ mode }: MasterFormPageProps) {
  const { entityKey, id } = useParams<{ entityKey: string; id: string }>();
  const config = getMasterConfig(entityKey);
  const navigate = useNavigate();
  const isNew = mode === 'create';

  const { data: record, isPending, isError } = useMasterQuery(entityKey ?? '', isNew ? undefined : id);
  const createMutation = useCreateMasterMutation(entityKey ?? '');
  const updateMutation = useUpdateMasterMutation(entityKey ?? '');
  const uploadPhotoMutation = useUploadMasterPhotoMutation(entityKey ?? '');

  // Consultant only (config.consultantLogin) — the Login access card, ADR-108. On by default
  // for a new consultant; the login is saved right after the consultant itself.
  const { hasPermission } = useAuth();
  const { toast } = useToast();
  const withLogin = Boolean(config?.consultantLogin);
  const loginAccess = useLoginAccessState('consultant', isNew && withLogin);
  const canSeeLogins = withLogin && hasPermission('identity-administration.view');
  const linkedLoginQuery = useConsultantLoginQuery(isNew ? undefined : id, canSeeLogins);
  const saveLoginMutation = useSaveConsultantLoginMutation();
  const linkedUserId = linkedLoginQuery.data?.id ?? null;

  if (!config) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Unknown Masters entity "{entityKey}".
        </p>
        <Link to="/admin/masters" className="mt-3 inline-block text-sm text-primary hover:underline">
          Back to Masters
        </Link>
      </div>
    );
  }

  const listPath = `/admin/masters/${config.key}`;

  if (!isNew && isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading {config.label.toLowerCase()}…
      </div>
    );
  }

  if (!isNew && (isError || !record)) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {config.label} not found.
        </p>
        <Link to={listPath} className="mt-3 inline-block text-sm text-primary hover:underline">
          Back to {config.labelPlural}
        </Link>
      </div>
    );
  }

  // Runs once the consultant itself is saved. If the login is then rejected (say the email is
  // already used by another login), the consultant stays saved and the user lands on its Edit
  // page, where the Login access card shows what to fix.
  function saveLoginThen(recordId: string, recordName: string, done: () => void) {
    if (!withLogin || linkedUserId || !loginAccess.draft.enabled) {
      done();
      return;
    }
    const isExisting = loginAccess.draft.mode === 'existing';
    saveLoginMutation.mutate(
      { consultantId: recordId, consultantName: recordName, draft: loginAccess.draft },
      {
        onSuccess: (user) => {
          toast({ title: isExisting ? 'Login linked' : `${config!.label} and login saved`, description: `They can log in as ${user.username}.`, variant: 'success' });
          done();
        },
        onError: (error) => {
          loginAccess.setErrors(loginErrorsFromApiError(error instanceof ApiError ? error : null));
          toast({ title: `${config!.label} saved, but the login wasn't`, description: error.message, variant: 'error' });
          navigate(`${listPath}/${recordId}/edit`);
        },
      },
    );
  }

  function handleSubmit(values: Record<string, unknown>, photoFile?: File) {
    if (isNew) {
      createMutation.mutate(values, {
        onSuccess: (created) => {
          // A staged photo (see MasterPhotoUpload) only ever comes from a config.photo entity
          // — the created record's real id now exists, so upload it before navigating away.
          // Best-effort: the record itself is already saved either way, so a failed photo
          // upload here shouldn't block navigation or need its own separate retry flow.
          if (photoFile) {
            uploadPhotoMutation.mutate({ id: created.id, file: photoFile });
          }
          saveLoginThen(created.id, String(values.name ?? ''), () => navigate(`${listPath}/${created.id}`));
        },
      });
    } else {
      updateMutation.mutate(
        { id: id as string, values },
        { onSuccess: () => saveLoginThen(id as string, String(values.name ?? ''), () => navigate(`${listPath}/${id}`)) },
      );
    }
  }

  const recordLabel = record ? getDisplayLabel(config, record) : '';
  const heading = isNew ? `New ${config.label}` : mode === 'view' ? recordLabel : `Edit ${recordLabel}`;
  const subtitle = isNew
    ? `Add a new ${config.label.toLowerCase()} to the Masters catalog.`
    : mode === 'view'
      ? `${config.label} details.`
      : `Update this ${config.label.toLowerCase()}'s details.`;
  const backTo = isNew || mode === 'view' ? listPath : `${listPath}/${id}`;
  const Icon = config.icon;

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Icon}
        title={heading}
        subtitle={subtitle}
        backTo={backTo}
        backLabel={`Back to ${config.labelPlural.toLowerCase()}`}
        rightActions={
          mode === 'view' && (
            <Button
              asChild
              variant="outline"
              className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
            >
              <Link to={`${listPath}/${id}/edit`}>
                <Pencil className="h-4 w-4" />
                Edit
              </Link>
            </Button>
          )
        }
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <MasterForm
          config={config}
          mode={mode}
          recordId={isNew ? undefined : id}
          defaultValues={isNew ? {} : (record as Record<string, unknown>)}
          isSubmitting={createMutation.isPending || updateMutation.isPending || saveLoginMutation.isPending}
          apiError={(createMutation.error ?? updateMutation.error) as ApiError | null}
          onSubmit={handleSubmit}
          onCancel={() => navigate(backTo)}
          onValidateExtra={withLogin && !linkedUserId ? loginAccess.validate : undefined}
          renderExtraSection={
            withLogin
              ? (values) => (
                  <LoginAccessCard
                    {...loginAccess.cardProps}
                    personName={String(values.name ?? '')}
                    linkedUserId={linkedUserId}
                    isLoadingLink={canSeeLogins && !isNew && linkedLoginQuery.isPending}
                    readOnly={mode === 'view'}
                  />
                )
              : undefined
          }
        />
      </div>
    </div>
  );
}
