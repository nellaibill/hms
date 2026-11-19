import { ApiError, type DiagnosticServiceFormValues } from '@hms/shared';
import { FlaskConical } from 'lucide-react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { RequirePermission } from '@/features/auth/RequirePermission';
import { DiagnosticServiceForm, toDiagnosticServiceRequest, useCreateDiagnosticServiceMutation } from '@/features/diagnostics';
import { priceListPath, priceListTabFor } from '@/features/priceList/priceListRows';

const KINDS = {
  lab: { title: 'New lab test', defaults: { serviceType: 'Laboratory', isOutsourced: false } },
  outsourced: { title: 'New outsourced test', defaults: { serviceType: 'Laboratory', isOutsourced: true } },
  radiology: { title: 'New radiology service', defaults: { serviceType: 'Radiology', isOutsourced: false } },
} as const satisfies Record<string, { title: string; defaults: Pick<DiagnosticServiceFormValues, 'serviceType' | 'isOutsourced'> }>;

/** Opened from the Price List's Add buttons (ADR-140) — `?kind=lab|outsourced|radiology` presets the form. */
export default function DiagnosticServiceCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const mutation = useCreateDiagnosticServiceMutation();
  const kindKey = searchParams.get('kind');
  const kind = kindKey && kindKey in KINDS ? KINDS[kindKey as keyof typeof KINDS] : KINDS.lab;
  const backTo = priceListPath(priceListTabFor(kind.defaults));

  function handleSubmit(values: DiagnosticServiceFormValues) {
    mutation.mutate(toDiagnosticServiceRequest(values), { onSuccess: () => navigate(priceListPath(priceListTabFor(values))) });
  }

  return (
    <RequirePermission permission="diagnostics.create">
      <div className="flex flex-1 flex-col">
        <PageBanner
          icon={FlaskConical}
          title={kind.title}
          subtitle="Set what the patient pays and what it costs us — the profit is worked out for you."
          backTo={backTo}
          backLabel="Back to Price List"
        />

        <div className="flex flex-1 flex-col gap-6 p-4 sm:p-6 lg:p-8">
          <DiagnosticServiceForm
            mode="create"
            submitLabel="Create"
            isSubmitting={mutation.isPending}
            apiError={mutation.error instanceof ApiError ? mutation.error : null}
            defaultValues={kind.defaults}
            onSubmit={handleSubmit}
          />
        </div>
      </div>
    </RequirePermission>
  );
}
