import type { XrayAiAnalysisResponse } from '@hms/shared';
import { ApiError } from '@hms/shared';
import { useQueryClient } from '@tanstack/react-query';
import { Loader2, ScanSearch } from 'lucide-react';
import { useCallback, useMemo, useRef, useState } from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import { useToast } from '@/components/ui/toast-context';
import { Button } from '@/components/ui/button';
import { radiologyApi } from '../../../services/apiClient';
import { useMarkAnalysisReviewedMutation } from '../hooks/useMarkAnalysisReviewedMutation';
import { patientXrayAnalysesQueryKey, usePatientXrayAnalysesQuery } from '../hooks/usePatientXrayAnalysesQuery';
import { usePatientXrayImagesQuery } from '../hooks/usePatientXrayImagesQuery';
import { summarizeXrayAnalysis } from '../parseXrayAnalysis';
import { XrayImageCard, type RunState } from './XrayImageCard';

/** LoginTypes the backend lets sign off an AI draft (RadiologyAiService.ReviewerLoginTypes) — used
 * only to hide a button that would 403; the server remains the real check. */
const REVIEWER_ROLES = new Set(['doctor', 'radiologist', 'admin', 'superAdmin']);

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return 'The analysis could not be completed. Please try again.';
}

function StatTile({ label, value, tone }: { label: string; value: number; tone?: 'warn' | 'good' }) {
  const toneClass = tone === 'warn' && value > 0 ? 'text-amber-700 dark:text-amber-300' : tone === 'good' && value > 0 ? 'text-emerald-700 dark:text-emerald-300' : 'text-foreground';
  return (
    <div className="rounded-lg border border-border bg-card px-4 py-3">
      <p className={`text-2xl font-semibold tabular-nums ${toneClass}`}>{value}</p>
      <p className="text-xs text-muted-foreground">{label}</p>
    </div>
  );
}

/** A patient's stored images with a saved, reviewable AI read of each. Analyses are written to the
 * patient's record by the API and read back from it, so they survive leaving the page; a re-analysis
 * adds to the history rather than replacing it. Images are analyzed one at a time (a hosted or CPU
 * model handles one request far better than several at once). */
export function PatientXrayImages({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const images = usePatientXrayImagesQuery(patientId);
  const analyses = usePatientXrayAnalysesQuery(patientId);
  const reviewMutation = useMarkAnalysisReviewedMutation(patientId);

  const [runStates, setRunStates] = useState<Record<string, RunState>>({});
  const [busy, setBusy] = useState(false);
  const runningRef = useRef(false);

  const canReview = user ? REVIEWER_ROLES.has(user.role) : false;

  const analysesByDocument = useMemo(() => {
    const grouped = new Map<string, XrayAiAnalysisResponse[]>();
    for (const analysis of analyses.data ?? []) {
      const list = grouped.get(analysis.documentId) ?? [];
      list.push(analysis);
      grouped.set(analysis.documentId, list);
    }
    return grouped;
  }, [analyses.data]);

  const analyzeOne = useCallback(
    async (documentId: string) => {
      setRunStates((current) => ({ ...current, [documentId]: { status: 'running' } }));
      try {
        const saved = await radiologyApi.analyzeDocument(documentId);
        queryClient.setQueryData<XrayAiAnalysisResponse[]>(patientXrayAnalysesQueryKey(patientId), (current) => [saved, ...(current ?? [])]);
        setRunStates((current) => {
          const next = { ...current };
          delete next[documentId];
          return next;
        });
      } catch (error) {
        setRunStates((current) => ({ ...current, [documentId]: { status: 'error', message: errorMessage(error) } }));
      }
    },
    [patientId, queryClient],
  );

  const run = useCallback(
    async (documentIds: string[]) => {
      if (runningRef.current) return;
      runningRef.current = true;
      setBusy(true);
      try {
        for (const id of documentIds) {
          await analyzeOne(id);
        }
      } finally {
        runningRef.current = false;
        setBusy(false);
      }
    },
    [analyzeOne],
  );

  const review = (analysisId: string) =>
    reviewMutation.mutate(analysisId, {
      onError: (error) => toast({ title: 'Could not mark as reviewed', description: errorMessage(error), variant: 'error' }),
    });

  if (images.isPending || analyses.isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-12 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading patient images…
      </div>
    );
  }

  if (images.isError || analyses.isError) {
    return <p className="py-12 text-center text-sm text-destructive">Couldn’t load this patient’s images.</p>;
  }

  if (images.data.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-border py-12 text-center text-sm text-muted-foreground">
        No JPEG, PNG or WebP images are stored in this patient’s documents. Upload X-rays from the patient’s Documents tab.
      </div>
    );
  }

  const latestByImage = images.data.map((image) => analysesByDocument.get(image.id)?.[0]);
  const analyzedCount = latestByImage.filter(Boolean).length;
  const reviewedCount = latestByImage.filter((analysis) => analysis?.isReviewed).length;
  const flaggedCount = latestByImage.filter((analysis) => {
    const verdict = analysis && summarizeXrayAnalysis(analysis.analysis).verdict;
    return verdict === 'possible' || verdict === 'identified';
  }).length;
  const pendingIds = images.data.filter((image) => !analysesByDocument.has(image.id)).map((image) => image.id);

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <StatTile label="Images" value={images.data.length} />
        <StatTile label="Analyzed by AI" value={analyzedCount} />
        <StatTile label="Possible fracture flagged" value={flaggedCount} tone="warn" />
        <StatTile label="Reviewed by a clinician" value={reviewedCount} tone="good" />
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">AI output is a draft for the doctor’s review, not a diagnosis. Analyses are saved to the patient’s record.</p>
        <Button size="sm" onClick={() => void run(pendingIds)} disabled={busy || pendingIds.length === 0}>
          {busy ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ScanSearch className="mr-2 h-4 w-4" />}
          {pendingIds.length === 0 ? 'All images analyzed' : `Analyze ${pendingIds.length} unanalyzed image${pendingIds.length === 1 ? '' : 's'}`}
        </Button>
      </div>

      <div className="space-y-4">
        {images.data.map((image) => (
          <XrayImageCard
            key={image.id}
            document={image}
            analyses={analysesByDocument.get(image.id) ?? []}
            runState={runStates[image.id]}
            busy={busy}
            canReview={canReview}
            reviewingId={reviewMutation.isPending ? (reviewMutation.variables ?? null) : null}
            onAnalyze={() => void run([image.id])}
            onReview={review}
          />
        ))}
      </div>
    </div>
  );
}
