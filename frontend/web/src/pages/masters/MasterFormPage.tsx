import type { ApiError } from '@hms/shared';
import { ArrowLeft, Loader2, Pencil } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import {
  getDisplayLabel,
  getMasterConfig,
  MasterForm,
  useCreateMasterMutation,
  useMasterQuery,
  useUpdateMasterMutation,
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

  function handleSubmit(values: Record<string, unknown>) {
    if (isNew) {
      createMutation.mutate(values, { onSuccess: (created) => navigate(`${listPath}/${created.id}`) });
    } else {
      updateMutation.mutate({ id: id as string, values }, { onSuccess: () => navigate(`${listPath}/${id}`) });
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to={backTo} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to {config.labelPlural.toLowerCase()}
        </Link>
      </div>

      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        {mode === 'view' && (
          <Button
            asChild
            variant="outline"
            className="absolute right-6 top-1/2 -translate-y-1/2 gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
          >
            <Link to={`${listPath}/${id}/edit`}>
              <Pencil className="h-4 w-4" />
              Edit
            </Link>
          </Button>
        )}
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Icon className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">{heading}</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">{subtitle}</p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <MasterForm
          config={config}
          mode={mode}
          recordId={isNew ? undefined : id}
          defaultValues={isNew ? {} : (record as Record<string, unknown>)}
          isSubmitting={createMutation.isPending || updateMutation.isPending}
          apiError={(createMutation.error ?? updateMutation.error) as ApiError | null}
          onSubmit={handleSubmit}
          onCancel={() => navigate(backTo)}
        />
      </div>
    </div>
  );
}
