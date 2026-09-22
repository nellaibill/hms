import type { DocumentResponse, XrayAiAnalysisResponse } from '@hms/shared';
import { ApiError } from '@hms/shared';
import { ImageOff, Loader2, ScanSearch } from 'lucide-react';
import { useCallback, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { radiologyApi } from '../../../services/apiClient';
import { useDocumentImageUrl } from '../hooks/useDocumentImageUrl';
import { usePatientXrayImagesQuery } from '../hooks/usePatientXrayImagesQuery';
import { XrayAnalysisResult } from './XrayAnalysisResult';

type AnalysisState = { status: 'running' } | { status: 'done'; result: XrayAiAnalysisResponse } | { status: 'error'; message: string };

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return 'The analysis could not be completed. Please try again.';
}

function XrayImageCard({
  document,
  state,
  busy,
  onAnalyze,
}: {
  document: DocumentResponse;
  state: AnalysisState | undefined;
  busy: boolean;
  onAnalyze: () => void;
}) {
  const { url, failed } = useDocumentImageUrl(document.id);
  const running = state?.status === 'running';

  return (
    <Card>
      <CardContent className="grid gap-4 p-4 md:grid-cols-[260px_1fr]">
        <div className="flex aspect-square items-center justify-center overflow-hidden rounded-lg bg-black">
          {url ? (
            <img src={url} alt={document.originalFileName} className="h-full w-full object-contain" />
          ) : failed ? (
            <ImageOff className="h-8 w-8 text-muted-foreground" />
          ) : (
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          )}
        </div>

        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-foreground">{document.originalFileName}</p>
              <p className="text-xs text-muted-foreground">{new Date(document.createdAt).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' })}</p>
            </div>
            <Button size="sm" variant={state?.status === 'done' ? 'outline' : 'default'} onClick={onAnalyze} disabled={busy}>
              {running ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ScanSearch className="mr-2 h-4 w-4" />}
              {running ? 'Analyzing…' : state?.status === 'done' ? 'Re-analyze' : 'Analyze with AI'}
            </Button>
          </div>

          {running && <p className="text-sm text-muted-foreground">The AI is reading this image. This can take a minute or two.</p>}
          {state?.status === 'error' && <p className="text-sm text-destructive">{state.message}</p>}
          {state?.status === 'done' && <XrayAnalysisResult result={state.result} />}
        </div>
      </CardContent>
    </Card>
  );
}

/** A patient's stored images with an on-demand AI read of each. Images are analyzed one at a time
 * (a hosted or CPU model handles one request at a time far better than four at once). Results live
 * only in this view — nothing is saved to the patient's record. */
export function PatientXrayImages({ patientId }: { patientId: string }) {
  const { data: images, isPending, isError } = usePatientXrayImagesQuery(patientId);
  const [states, setStates] = useState<Record<string, AnalysisState>>({});
  const [busy, setBusy] = useState(false);
  const runningRef = useRef(false);

  const analyzeOne = useCallback(async (documentId: string) => {
    setStates((current) => ({ ...current, [documentId]: { status: 'running' } }));
    try {
      const result = await radiologyApi.analyzeDocument(documentId);
      setStates((current) => ({ ...current, [documentId]: { status: 'done', result } }));
    } catch (error) {
      setStates((current) => ({ ...current, [documentId]: { status: 'error', message: errorMessage(error) } }));
    }
  }, []);

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

  if (isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-12 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading patient images…
      </div>
    );
  }

  if (isError) {
    return <p className="py-12 text-center text-sm text-destructive">Couldn’t load this patient’s images.</p>;
  }

  if (images.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-border py-12 text-center text-sm text-muted-foreground">
        No JPEG, PNG or WebP images are stored in this patient’s documents. Upload X-rays from the patient’s Documents tab.
      </div>
    );
  }

  const pending = images.filter((image) => states[image.id]?.status !== 'done').map((image) => image.id);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm text-muted-foreground">
          {images.length} image{images.length === 1 ? '' : 's'} · AI output is a draft for the doctor’s review, not a diagnosis.
        </p>
        <Button size="sm" onClick={() => void run(pending)} disabled={busy || pending.length === 0}>
          {busy ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ScanSearch className="mr-2 h-4 w-4" />}
          Analyze all{pending.length > 0 && pending.length !== images.length ? ` (${pending.length} remaining)` : ''}
        </Button>
      </div>

      {images.map((image) => (
        <XrayImageCard key={image.id} document={image} state={states[image.id]} busy={busy} onAnalyze={() => void run([image.id])} />
      ))}
    </div>
  );
}
