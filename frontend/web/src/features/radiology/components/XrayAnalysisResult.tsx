import type { XrayAiAnalysisResponse } from '@hms/shared';
import { CheckCircle2, Loader2, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { fractureHeadline, parseBullets, parseFractureDetails, parseXrayAnalysis, summarizeXrayAnalysis, type XraySectionTitle } from '../parseXrayAnalysis';
import { VerdictBadge } from './VerdictBadge';

const CONFIDENCE_STYLES = {
  High: 'border-emerald-200 text-emerald-700 dark:border-emerald-500/30 dark:text-emerald-300',
  Moderate: 'border-amber-200 text-amber-800 dark:border-amber-500/30 dark:text-amber-300',
  Low: 'border-red-200 text-red-700 dark:border-red-500/30 dark:text-red-300',
} as const;

const formatDateTime = (iso: string) => new Date(iso).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' });

function SectionLabel({ children }: { children: string }) {
  return <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{children}</p>;
}

interface XrayAnalysisResultProps {
  analysis: XrayAiAnalysisResponse;
  /** Shown as a "Mark as reviewed" button while the analysis is still an unreviewed draft. */
  canReview?: boolean;
  reviewing?: boolean;
  onReview?: () => void;
  /** Older analyses of the same image render muted and without the review action. */
  compact?: boolean;
}

/** One saved AI analysis: verdict, confidence and region up top, then the findings. The disclaimer
 * is always shown — this is never a diagnosis — and an unreviewed draft is visibly distinct from a
 * clinician-confirmed one. */
export function XrayAnalysisResult({ analysis, canReview, reviewing, onReview, compact }: XrayAnalysisResultProps) {
  const summary = summarizeXrayAnalysis(analysis.analysis);
  const sections = parseXrayAnalysis(analysis.analysis);
  const body = (title: XraySectionTitle) => sections.find((section) => section.title === title)?.body ?? '';

  const findings = parseBullets(body('KEY FINDINGS'));
  const fractureBody = body('FRACTURE ASSESSMENT');
  const fractureDetails = parseFractureDetails(fractureBody);
  const fractureNote = fractureHeadline(fractureBody);
  const prose: { title: string; text: string }[] = [
    { title: 'Image quality', text: body('IMAGE QUALITY') },
    { title: 'Joint & alignment', text: body('JOINT & ALIGNMENT') },
    { title: 'Other observations', text: body('OTHER OBSERVATIONS') },
  ].filter((item) => item.text);
  const unstructured = sections.length === 1 && sections[0].title === '';

  return (
    <div
      className={cn(
        'space-y-4 rounded-lg border p-4',
        analysis.isReviewed
          ? 'border-emerald-200 bg-emerald-50/40 dark:border-emerald-500/30 dark:bg-emerald-500/5'
          : 'border-fuchsia-200 bg-fuchsia-50/40 dark:border-fuchsia-500/30 dark:bg-fuchsia-500/5',
        compact && 'opacity-80',
      )}
    >
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
        <span
          className={cn(
            'inline-flex items-center gap-1.5 font-medium',
            analysis.isReviewed ? 'text-emerald-700 dark:text-emerald-300' : 'text-fuchsia-700 dark:text-fuchsia-300',
          )}
        >
          {analysis.isReviewed ? <CheckCircle2 className="h-3.5 w-3.5" /> : <Sparkles className="h-3.5 w-3.5" />}
          {analysis.isReviewed ? 'AI draft — reviewed by a clinician' : 'AI draft — not yet reviewed'}
        </span>
        <span className="text-muted-foreground">
          {analysis.model} · {formatDateTime(analysis.generatedAtUtc)}
        </span>
      </div>

      {!unstructured && (
        <div className="flex flex-wrap items-center gap-2">
          <VerdictBadge verdict={summary.verdict} />
          {summary.confidence && (
            <span className={cn('rounded-full border bg-background px-2.5 py-1 text-xs font-medium', CONFIDENCE_STYLES[summary.confidence])}>
              {summary.confidence} confidence
            </span>
          )}
          {summary.region && <span className="rounded-full border border-border bg-background px-2.5 py-1 text-xs text-foreground">{summary.region}</span>}
        </div>
      )}

      {unstructured ? (
        <p className="whitespace-pre-wrap text-sm text-foreground">{sections[0].body}</p>
      ) : (
        <div className="space-y-4">
          {/* The verdict sentence only adds anything when it says more than the badge already does. */}
          {fractureNote.length > 30 && summary.verdict !== 'none' && <p className="text-sm text-foreground">{fractureNote}</p>}

          {fractureDetails.length > 0 && (
            <div>
              <SectionLabel>Fracture details</SectionLabel>
              <dl className="grid gap-x-6 gap-y-1.5 text-sm sm:grid-cols-2">
                {fractureDetails.map((detail) => (
                  <div key={detail.label} className="flex gap-2">
                    <dt className="w-24 shrink-0 text-muted-foreground">{detail.label}</dt>
                    <dd className="text-foreground">{detail.value}</dd>
                  </div>
                ))}
              </dl>
            </div>
          )}

          {findings.length > 0 && (
            <div>
              <SectionLabel>Key findings</SectionLabel>
              <ul className="list-disc space-y-1 pl-5 text-sm text-foreground marker:text-muted-foreground">
                {findings.map((finding, index) => (
                  <li key={index}>{finding}</li>
                ))}
              </ul>
            </div>
          )}

          {prose.length > 0 && (
            <div className="grid gap-4 sm:grid-cols-2">
              {prose.map((item) => (
                <div key={item.title}>
                  <SectionLabel>{item.title}</SectionLabel>
                  <p className="whitespace-pre-wrap text-sm text-foreground">{item.text}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      <div className="flex flex-wrap items-end justify-between gap-3 border-t border-border/70 pt-3">
        <p className="max-w-xl text-xs text-muted-foreground">{analysis.disclaimer}</p>
        {analysis.isReviewed ? (
          analysis.reviewedAtUtc && <p className="text-xs text-muted-foreground">Reviewed {formatDateTime(analysis.reviewedAtUtc)}</p>
        ) : (
          canReview &&
          !compact && (
            <Button size="sm" variant="outline" onClick={onReview} disabled={reviewing}>
              {reviewing ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <CheckCircle2 className="mr-2 h-4 w-4" />}
              Mark as reviewed
            </Button>
          )
        )}
      </div>
    </div>
  );
}
