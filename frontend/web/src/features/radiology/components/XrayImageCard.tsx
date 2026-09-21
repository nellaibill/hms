import type { DocumentResponse, XrayAiAnalysisResponse } from '@hms/shared';
import { ChevronDown, ImageOff, Loader2, Maximize2, ScanSearch } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useDocumentImageUrl } from '../hooks/useDocumentImageUrl';
import { XrayAnalysisResult } from './XrayAnalysisResult';
import { XrayViewerDialog } from './XrayViewerDialog';

export type RunState = { status: 'running' } | { status: 'error'; message: string };

function useElapsedSeconds(active: boolean): number {
  const [seconds, setSeconds] = useState(0);
  useEffect(() => {
    if (!active) {
      setSeconds(0);
      return;
    }
    const startedAt = Date.now();
    const timer = window.setInterval(() => setSeconds(Math.floor((Date.now() - startedAt) / 1000)), 1000);
    return () => window.clearInterval(timer);
  }, [active]);
  return seconds;
}

interface XrayImageCardProps {
  document: DocumentResponse;
  /** Saved analyses of this image, newest first. */
  analyses: XrayAiAnalysisResponse[];
  runState: RunState | undefined;
  /** Any image is being analyzed — analyses run one at a time. */
  busy: boolean;
  canReview: boolean;
  reviewingId: string | null;
  onAnalyze: () => void;
  onReview: (analysisId: string) => void;
}

export function XrayImageCard({ document, analyses, runState, busy, canReview, reviewingId, onAnalyze, onReview }: XrayImageCardProps) {
  const { url, failed } = useDocumentImageUrl(document.id);
  const [viewerOpen, setViewerOpen] = useState(false);
  const [showHistory, setShowHistory] = useState(false);

  const running = runState?.status === 'running';
  const elapsed = useElapsedSeconds(running);
  const [latest, ...earlier] = analyses;

  return (
    <Card>
      <CardContent className="grid gap-5 p-4 md:grid-cols-[300px_1fr]">
        <button
          type="button"
          onClick={() => url && setViewerOpen(true)}
          disabled={!url}
          className="group relative flex aspect-[4/5] items-center justify-center overflow-hidden rounded-lg bg-black text-left focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={`Open ${document.originalFileName} full size`}
        >
          {url ? (
            <>
              <img src={url} alt={document.originalFileName} className="h-full w-full object-contain" />
              <span className="absolute bottom-2 right-2 inline-flex items-center gap-1 rounded-md bg-black/70 px-2 py-1 text-xs text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100">
                <Maximize2 className="h-3 w-3" />
                Full size
              </span>
            </>
          ) : failed ? (
            <ImageOff className="h-8 w-8 text-muted-foreground" />
          ) : (
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          )}
        </button>

        <div className="min-w-0 space-y-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="min-w-0 space-y-1.5">
              <p className="truncate text-sm font-semibold text-foreground">{document.originalFileName}</p>
              <p className="text-xs text-muted-foreground">
                Uploaded {new Date(document.createdAt).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' })}
              </p>
              {!latest ? (
                <Badge variant="outline">Not analyzed</Badge>
              ) : latest.isReviewed ? (
                <Badge variant="success">Reviewed</Badge>
              ) : (
                <Badge variant="warning">AI draft · awaiting review</Badge>
              )}
            </div>
            <Button size="sm" variant={latest ? 'outline' : 'default'} onClick={onAnalyze} disabled={busy}>
              {running ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ScanSearch className="mr-2 h-4 w-4" />}
              {running ? `Analyzing… ${elapsed}s` : latest ? 'Re-analyze' : 'Analyze with AI'}
            </Button>
          </div>

          {running && (
            <p className="rounded-lg border border-dashed border-border px-3 py-6 text-center text-sm text-muted-foreground">
              The AI is reading this image. This can take a minute or two.
            </p>
          )}
          {runState?.status === 'error' && (
            <p role="alert" className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              {runState.message}
            </p>
          )}

          {latest && !running && (
            <XrayAnalysisResult analysis={latest} canReview={canReview} reviewing={reviewingId === latest.id} onReview={() => onReview(latest.id)} />
          )}

          {earlier.length > 0 && (
            <div>
              <button
                type="button"
                onClick={() => setShowHistory((value) => !value)}
                className="inline-flex items-center gap-1 text-xs font-medium text-muted-foreground hover:text-foreground"
                aria-expanded={showHistory}
              >
                <ChevronDown className={`h-3.5 w-3.5 transition-transform ${showHistory ? 'rotate-180' : ''}`} />
                {showHistory ? 'Hide' : 'Show'} {earlier.length} earlier analys{earlier.length === 1 ? 'is' : 'es'}
              </button>
              {showHistory && (
                <div className="mt-2 space-y-3">
                  {earlier.map((analysis) => (
                    <XrayAnalysisResult key={analysis.id} analysis={analysis} compact />
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </CardContent>

      <XrayViewerDialog open={viewerOpen} onOpenChange={setViewerOpen} url={url} title={document.originalFileName} />
    </Card>
  );
}
