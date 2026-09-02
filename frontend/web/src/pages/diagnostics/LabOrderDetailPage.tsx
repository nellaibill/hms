import { AlertTriangle, ArrowLeft, FileCheck2, Loader2, PackageSearch, Printer, RotateCcw, SendToBack } from 'lucide-react';
import { type ReactNode, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/features/auth/AuthContext';
import {
  LabOrderAuditTrail,
  LabReportPrintTemplate,
  LabStatusBadge,
  ResultEntryForm,
  SampleCollectionDialog,
  SampleRejectionDialog,
  VerificationPanel,
  useCollectSampleMutation,
  useGenerateReportMutation,
  useLabOrderQuery,
  useReceiveSampleMutation,
  useRejectForCorrectionMutation,
  useRejectSampleMutation,
  useReleaseReportMutation,
  useRequestRecollectionMutation,
  useSaveResultDraftMutation,
  useStartProcessingMutation,
  useSubmitForVerificationMutation,
  useVerifyMutation,
  type LabOrderItem,
} from '@/features/laboratory';

function Field({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-0.5">
      <dt className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

function SectionCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5 rounded-lg border border-border bg-card p-3">
      <h2 className="text-sm font-semibold text-foreground">{title}</h2>
      {children}
    </div>
  );
}

export default function LabOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission('diagnostics.create');
  const canEdit = hasPermission('diagnostics.edit');

  const { data: order, isPending, isError } = useLabOrderQuery(id);

  const [activeTab, setActiveTab] = useState('overview');
  const [collectingItem, setCollectingItem] = useState<LabOrderItem | null>(null);
  const [rejectingItem, setRejectingItem] = useState<LabOrderItem | null>(null);

  const collectSampleMutation = useCollectSampleMutation();
  const rejectSampleMutation = useRejectSampleMutation();
  const requestRecollectionMutation = useRequestRecollectionMutation();
  const receiveSampleMutation = useReceiveSampleMutation();
  const startProcessingMutation = useStartProcessingMutation();
  const saveResultDraftMutation = useSaveResultDraftMutation();
  const submitForVerificationMutation = useSubmitForVerificationMutation();
  const verifyMutation = useVerifyMutation();
  const rejectForCorrectionMutation = useRejectForCorrectionMutation();
  const generateReportMutation = useGenerateReportMutation();
  const releaseReportMutation = useReleaseReportMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading lab order…
      </div>
    );
  }

  if (isError || !order) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Lab order not found.
        </p>
      </div>
    );
  }

  function itemActions(item: LabOrderItem) {
    switch (item.status) {
      case 'PendingCollection':
      case 'RecollectionRequired':
        return (
          canCreate && (
            <Button size="sm" onClick={() => setCollectingItem(item)}>
              {item.status === 'RecollectionRequired' ? 'Recollect Sample' : 'Collect Sample'}
            </Button>
          )
        );
      case 'Collected':
        return (
          canCreate && (
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={() => setRejectingItem(item)}>
                Reject Sample
              </Button>
              <Button size="sm" onClick={() => receiveSampleMutation.mutate(item.id)} disabled={receiveSampleMutation.isPending}>
                Receive
              </Button>
            </div>
          )
        );
      case 'Rejected':
        return (
          canCreate && (
            <Button
              size="sm"
              variant="outline"
              className="gap-1.5"
              onClick={() => requestRecollectionMutation.mutate(item.id)}
              disabled={requestRecollectionMutation.isPending}
            >
              <RotateCcw className="h-3.5 w-3.5" />
              Request Recollection
            </Button>
          )
        );
      case 'Received':
      case 'CorrectionRequired':
        return (
          canCreate && (
            <Button size="sm" onClick={() => startProcessingMutation.mutate(item.id)} disabled={startProcessingMutation.isPending}>
              {item.status === 'CorrectionRequired' ? 'Resume Processing' : 'Start Processing'}
            </Button>
          )
        );
      case 'Processing':
      case 'ResultEntryInProgress':
        return (
          canCreate && (
            <Button size="sm" variant="outline" onClick={() => setActiveTab('results')}>
              Enter Results
            </Button>
          )
        );
      case 'PendingVerification':
        return (
          canEdit && (
            <Button size="sm" variant="outline" className="gap-1.5" onClick={() => setActiveTab('verification')}>
              <FileCheck2 className="h-3.5 w-3.5" />
              Review &amp; Verify
            </Button>
          )
        );
      default:
        return null;
    }
  }

  const everyItemVerified = order.items.length > 0 && order.items.every((item) => item.status === 'Verified');
  const pendingVerificationItems = order.items.filter((item) => item.status === 'PendingVerification');

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/diagnostics/lab/worklist" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to Lab Worklist
        </Link>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h1 className="font-mono text-lg font-semibold text-foreground">{order.labOrderNumber}</h1>
            <p className="text-sm text-muted-foreground">
              {order.patientName} · {order.patientUhid}
            </p>
          </div>
          <LabStatusBadge status={order.overallStatus} />
        </div>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="tests">Tests</TabsTrigger>
            <TabsTrigger value="samples">Samples</TabsTrigger>
            <TabsTrigger value="results">Results</TabsTrigger>
            {canEdit && <TabsTrigger value="verification">Verification</TabsTrigger>}
            <TabsTrigger value="report">Report</TabsTrigger>
            <TabsTrigger value="audit">Audit History</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-4">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <SectionCard title="Patient">
                <dl className="grid grid-cols-1 gap-x-3">
                  <Field label="Name" value={order.patientName} />
                  <Field label="UHID" value={order.patientUhid} />
                </dl>
              </SectionCard>
              <SectionCard title="Billing">
                <dl className="grid grid-cols-1 gap-x-3">
                  <Field
                    label="Invoice"
                    value={
                      <Link to={`/finance/accounts/${order.invoiceId}`} className="text-primary hover:underline">
                        View Invoice
                      </Link>
                    }
                  />
                </dl>
              </SectionCard>
              <SectionCard title="Request">
                <dl className="grid grid-cols-1 gap-x-3">
                  <Field label="Order No." value={order.labOrderNumber} />
                  <Field label="Source" value={order.source ?? '—'} />
                  <Field label="Priority" value={order.priority} />
                  <Field label="Created" value={new Date(order.createdAt).toLocaleString('en-IN')} />
                  <Field label="Overall Status" value={<LabStatusBadge status={order.overallStatus} />} />
                </dl>
              </SectionCard>
            </div>
          </TabsContent>

          <TabsContent value="tests" className="mt-4">
            <div className="flex flex-col divide-y divide-border rounded-md border border-border">
              {order.items.map((item) => (
                <div key={item.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                  <div className="flex flex-col gap-0.5">
                    <span className="inline-flex items-center gap-1.5 text-sm font-medium text-foreground">
                      {item.testName}
                      {item.packageId && (
                        <span title="Part of a package">
                          <PackageSearch className="h-3.5 w-3.5 text-muted-foreground" />
                        </span>
                      )}
                    </span>
                    {item.sampleType && <span className="text-xs text-muted-foreground">Sample: {item.sampleType}</span>}
                  </div>
                  <div className="flex items-center gap-3">
                    <LabStatusBadge status={item.status} />
                    {itemActions(item)}
                  </div>
                </div>
              ))}
            </div>
          </TabsContent>

          <TabsContent value="samples" className="mt-4">
            <div className="flex flex-col divide-y divide-border rounded-md border border-border">
              {order.items.map((item) => (
                <div key={item.id} className="flex flex-col gap-2 px-4 py-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium text-foreground">{item.testName}</span>
                    <LabStatusBadge status={item.status} />
                  </div>
                  <dl className="grid grid-cols-2 gap-x-3 sm:grid-cols-4">
                    <Field label="Sample Type" value={item.sampleType ?? '—'} />
                    <Field label="Collected At" value={item.collectedAt ? new Date(item.collectedAt).toLocaleString('en-IN') : '—'} />
                    <Field label="Location" value={item.collectionLocation ?? '—'} />
                    <Field label="Quantity" value={item.sampleQuantity ?? '—'} />
                  </dl>
                  {item.collectionRemarks && <p className="text-xs text-muted-foreground">Remarks: {item.collectionRemarks}</p>}
                  {item.rejectionReason && (
                    <div className="flex flex-col gap-1 rounded-md border border-destructive/30 bg-destructive/5 p-2">
                      <span className="inline-flex items-center gap-1.5 text-xs font-medium text-destructive">
                        <AlertTriangle className="h-3.5 w-3.5" />
                        Rejected — {item.rejectionReason}
                      </span>
                      {item.rejectionRemarks && <span className="text-xs text-muted-foreground">{item.rejectionRemarks}</span>}
                      <span className="text-xs text-muted-foreground">{item.rejectedAt ? new Date(item.rejectedAt).toLocaleString('en-IN') : ''}</span>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </TabsContent>

          <TabsContent value="results" className="mt-4">
            <div className="flex flex-col divide-y divide-border rounded-md border border-border">
              {order.items.map((item) => (
                <div key={item.id} className="flex flex-col gap-2 px-4 py-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium text-foreground">{item.testName}</span>
                    <LabStatusBadge status={item.status} />
                  </div>
                  {item.correctionReason && (
                    <p className="rounded-md border border-warning/30 bg-warning/10 px-2 py-1.5 text-xs text-warning">
                      Sent back for correction: {item.correctionReason}
                    </p>
                  )}
                  <ResultEntryForm
                    item={item}
                    isSavingDraft={saveResultDraftMutation.isPending && saveResultDraftMutation.variables?.itemId === item.id}
                    isSubmitting={submitForVerificationMutation.isPending && submitForVerificationMutation.variables === item.id}
                    onSaveDraft={(parameters) => saveResultDraftMutation.mutate({ itemId: item.id, request: { parameters } })}
                    onSubmitForVerification={async (parameters) => {
                      await saveResultDraftMutation.mutateAsync({ itemId: item.id, request: { parameters } });
                      await submitForVerificationMutation.mutateAsync(item.id);
                    }}
                  />
                </div>
              ))}
            </div>
          </TabsContent>

          {canEdit && (
            <TabsContent value="verification" className="mt-4">
              {pendingVerificationItems.length === 0 ? (
                <p className="rounded-lg border border-dashed border-border py-10 text-center text-sm text-muted-foreground">
                  No items awaiting verification.
                </p>
              ) : (
                <div className="flex flex-col gap-4">
                  {pendingVerificationItems.map((item) => (
                    <div key={item.id} className="flex flex-col gap-2 rounded-lg border border-border p-3">
                      <span className="text-sm font-medium text-foreground">{item.testName}</span>
                      <VerificationPanel
                        item={item}
                        isVerifying={verifyMutation.isPending && verifyMutation.variables === item.id}
                        isRejecting={rejectForCorrectionMutation.isPending && rejectForCorrectionMutation.variables?.itemId === item.id}
                        onVerify={() => verifyMutation.mutate(item.id)}
                        onRejectForCorrection={(reason) => rejectForCorrectionMutation.mutate({ itemId: item.id, request: { reason } })}
                      />
                    </div>
                  ))}
                </div>
              )}
            </TabsContent>
          )}

          <TabsContent value="report" className="mt-4">
            <div className="flex flex-col gap-4">
              <div className="flex flex-wrap items-center gap-2">
                <Button variant="outline" className="gap-1.5" onClick={() => window.print()}>
                  <Printer className="h-4 w-4" />
                  Print
                </Button>
                {canEdit && (
                  <Button
                    className="gap-1.5"
                    disabled={!everyItemVerified || Boolean(order.reportGeneratedAt) || generateReportMutation.isPending}
                    onClick={() => generateReportMutation.mutate(order.id)}
                  >
                    <FileCheck2 className="h-4 w-4" />
                    {generateReportMutation.isPending ? 'Generating…' : 'Generate Report'}
                  </Button>
                )}
                {canEdit && (
                  <Button
                    variant="success"
                    className="gap-1.5"
                    disabled={!order.reportGeneratedAt || Boolean(order.reportReleasedAt) || releaseReportMutation.isPending}
                    onClick={() => releaseReportMutation.mutate(order.id)}
                  >
                    <SendToBack className="h-4 w-4" />
                    {releaseReportMutation.isPending ? 'Releasing…' : 'Release Report'}
                  </Button>
                )}
                {!everyItemVerified && !order.reportGeneratedAt && (
                  <span className="text-xs text-muted-foreground">Every test must be verified before a report can be generated.</span>
                )}
                {order.reportReleasedAt && <Badge variant="success">Released {new Date(order.reportReleasedAt).toLocaleString('en-IN')}</Badge>}
              </div>

              <div className="rounded-lg border border-border bg-muted/20 p-4">
                <p className="mb-3 text-xs text-muted-foreground">Print preview (visible on the printed page only):</p>
                <LabReportPrintTemplate order={order} />
              </div>
            </div>
          </TabsContent>

          <TabsContent value="audit" className="mt-4">
            <LabOrderAuditTrail order={order} />
          </TabsContent>
        </Tabs>
      </div>

      {collectingItem && (
        <SampleCollectionDialog
          testName={collectingItem.testName}
          isRecollection={collectingItem.status === 'RecollectionRequired'}
          isSaving={collectSampleMutation.isPending}
          onConfirm={(request) =>
            collectSampleMutation.mutate({ itemId: collectingItem.id, request }, { onSuccess: () => setCollectingItem(null) })
          }
          onCancel={() => setCollectingItem(null)}
        />
      )}

      {rejectingItem && (
        <SampleRejectionDialog
          testName={rejectingItem.testName}
          isSaving={rejectSampleMutation.isPending}
          onConfirm={(request) => rejectSampleMutation.mutate({ itemId: rejectingItem.id, request }, { onSuccess: () => setRejectingItem(null) })}
          onCancel={() => setRejectingItem(null)}
        />
      )}
    </div>
  );
}
