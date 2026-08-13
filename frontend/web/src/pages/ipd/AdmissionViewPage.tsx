import { ApiError, type DischargeAdmissionFormValues, type TransferBedFormValues } from '@hms/shared';
import { ArrowLeft, ClipboardList, Loader2, Repeat } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useToast } from '@/components/ui/toast-context';
import {
  BedStayHistoryPanel,
  ChargesPanel,
  DischargeForm,
  TransferBedDialog,
  TransferHistoryPanel,
  useAdmissionQuery,
  useDischargeMutation,
  useTransferBedMutation,
} from '../../features/ipd/admissions';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm text-foreground">{value}</p>
    </div>
  );
}

export default function AdmissionViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: admission, isPending, isError } = useAdmissionQuery(id);
  const [isTransferOpen, setIsTransferOpen] = useState(false);
  const transferMutation = useTransferBedMutation();
  const dischargeMutation = useDischargeMutation();
  const { toast } = useToast();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading admission…
      </div>
    );
  }

  if (isError || !admission) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Admission not found.
        </p>
      </div>
    );
  }

  function handleTransfer(values: TransferBedFormValues) {
    transferMutation.mutate(
      { id: id as string, request: values },
      {
        onSuccess: () => {
          setIsTransferOpen(false);
          toast({ title: 'Bed transferred', description: 'The patient has been moved to the new bed.' });
        },
      },
    );
  }

  function handleDischarge(values: DischargeAdmissionFormValues) {
    dischargeMutation.mutate({
      id: id as string,
      request: {
        dischargeDateTime: new Date(values.dischargeDateTime).toISOString(),
        dischargeType: values.dischargeType,
        finalDiagnosis: values.finalDiagnosis || undefined,
        dischargeNotes: values.dischargeNotes || undefined,
        followUpAdvice: values.followUpAdvice || undefined,
      },
    });
  }

  const isAdmitted = admission.status === 'Admitted';

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd/admissions" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to admissions
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <ClipboardList className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">{admission.patientName}</h1>
          <Badge variant={isAdmitted ? 'success' : 'secondary'}>{admission.status}</Badge>
        </div>
        <p className="font-mono text-sm text-page-banner-foreground/85">{admission.admissionNumber}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Patient Summary</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Field label="UHID" value={admission.uhid} />
            <Field label="Age / Gender" value={`${admission.age} / ${admission.gender}`} />
            <Field label="Consultant" value={admission.consultantName} />
            <Field label="Ward / Bed" value={`${admission.wardName} / ${admission.bedNumber}`} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base">Admission Summary</CardTitle>
            {isAdmitted && (
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setIsTransferOpen(true)}>
                <Repeat className="h-4 w-4" />
                Transfer Bed
              </Button>
            )}
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="Admission type" value={admission.admissionType} />
            <Field label="Admission date/time" value={new Date(admission.admissionDateTime).toLocaleString('en-IN')} />
            <Field label="Reason for admission" value={admission.reasonForAdmission} />
            {!isAdmitted && (
              <>
                <Field label="Discharge date/time" value={admission.dischargeDateTime ? new Date(admission.dischargeDateTime).toLocaleString('en-IN') : '—'} />
                <Field label="Discharge type" value={admission.dischargeType ?? '—'} />
                <Field label="Final diagnosis" value={admission.finalDiagnosis || '—'} />
                <Field label="Discharge notes" value={admission.dischargeNotes || '—'} />
                <Field label="Follow-up advice" value={admission.followUpAdvice || '—'} />
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Transfer History</CardTitle>
          </CardHeader>
          <CardContent>
            <TransferHistoryPanel admissionId={admission.id} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Bed Stay History</CardTitle>
          </CardHeader>
          <CardContent>
            <BedStayHistoryPanel admissionId={admission.id} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Charges</CardTitle>
          </CardHeader>
          <CardContent>
            <ChargesPanel admissionId={admission.id} />
          </CardContent>
        </Card>

        {isAdmitted && (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Discharge</CardTitle>
            </CardHeader>
            <CardContent>
              <DischargeForm
                isSubmitting={dischargeMutation.isPending}
                apiError={dischargeMutation.error instanceof ApiError ? dischargeMutation.error : null}
                onSubmit={handleDischarge}
              />
            </CardContent>
          </Card>
        )}

        {isTransferOpen && (
          <TransferBedDialog
            currentWardName={admission.wardName}
            currentBedNumber={admission.bedNumber}
            currentBedId={admission.bedId}
            isSubmitting={transferMutation.isPending}
            apiError={transferMutation.error instanceof ApiError ? transferMutation.error : null}
            onSubmit={handleTransfer}
            onCancel={() => setIsTransferOpen(false)}
          />
        )}
      </div>
    </div>
  );
}
