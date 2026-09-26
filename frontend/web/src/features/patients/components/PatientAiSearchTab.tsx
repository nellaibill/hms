import { ApiError, type DocumentSearchHitResponse, type Patient } from '@hms/shared';
import { AlertTriangle, ChevronDown, ChevronUp, Eye, FileSearch, FileText, Loader2, Search, Sparkles, X } from 'lucide-react';
import { useState, type FormEvent, type ReactNode } from 'react';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import { usePatientDocumentSearchQuery } from '../hooks/usePatientDocumentSearchQuery';
import { openDocument } from '../openDocument';

/** Starting points for a doctor who hasn't typed anything yet — phrased as questions, the way
 * semantic search works best. */
const SUGGESTED_QUESTIONS = [
  'Which antibiotics were given?',
  'Latest HbA1c and blood sugar results',
  'Discharge advice and follow-up',
  'Known allergies or drug reactions',
];

/** Collapsed passages show roughly this many characters. */
const PREVIEW_CHARS = 320;

/**
 * Relevance bands for BAAI/bge-m3 cosine similarity (1 − distance) between a question and a
 * passage. Rough, empirically-set cut-offs: the model rarely scores unrelated text above
 * ~0.4, while a passage that actually answers the question lands around 0.55–0.75.
 */
function relevance(similarity: number): { label: string; variant: BadgeProps['variant'] } {
  if (similarity >= 0.6) return { label: 'Strong match', variant: 'success' };
  if (similarity >= 0.45) return { label: 'Possible match', variant: 'warning' };
  return { label: 'Weak match', variant: 'secondary' };
}

function errorMessage(error: unknown): { title: string; detail: string } {
  if (error instanceof ApiError) {
    if (error.status === 503) {
      return { title: 'AI Search isn’t enabled', detail: 'Document search hasn’t been configured for this hospital. Ask your administrator to enable embeddings.' };
    }
    if (error.status === 502) {
      return { title: 'Search service unavailable', detail: 'The AI search service didn’t respond. Please try again in a moment.' };
    }
    if (error.status === 403) {
      return { title: 'Not permitted', detail: 'You don’t have permission to search this patient’s documents.' };
    }
    return { title: 'Search failed', detail: error.message };
  }
  return { title: 'Search failed', detail: 'Couldn’t reach the server. Check your connection and try again.' };
}

/** Question words too common to be worth highlighting in a passage. */
const STOP_WORDS = new Set([
  'and', 'the', 'for', 'with', 'was', 'were', 'are', 'has', 'had', 'have', 'any', 'all', 'from', 'this', 'that', 'there',
  'what', 'which', 'when', 'who', 'how', 'did', 'does', 'given', 'show', 'list', 'latest', 'last', 'patient', 'results',
]);

/** Wraps the question's meaningful words (3+ letters, not stop words) in <mark> — semantic
 * matches often won't contain them, but when they do it shows the doctor where to look. */
function highlight(text: string, question: string): ReactNode {
  const words = [...new Set(question.toLowerCase().match(/[\p{L}\p{N}]{3,}/gu) ?? [])].filter((word) => !STOP_WORDS.has(word));
  if (words.length === 0) return text;
  const pattern = new RegExp(`(${words.map((w) => w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|')})`, 'giu');
  return text.split(pattern).map((part, i) =>
    i % 2 === 1 ? (
      <mark key={i} className="rounded-sm bg-amber-200/70 px-0.5 text-foreground dark:bg-amber-400/30">
        {part}
      </mark>
    ) : (
      part
    ),
  );
}

function ResultCard({ hit, rank, question }: { hit: DocumentSearchHitResponse; rank: number; question: string }) {
  const [expanded, setExpanded] = useState(false);
  const [opening, setOpening] = useState(false);
  const { label, variant } = relevance(hit.similarity);
  const content = hit.content.replace(/\n{2,}/g, '\n');
  const isLong = content.length > PREVIEW_CHARS;
  const shown = expanded || !isLong ? content : `${content.slice(0, PREVIEW_CHARS).trimEnd()}…`;

  const handleOpen = async () => {
    setOpening(true);
    try {
      await openDocument({ id: hit.documentId, originalFileName: hit.originalFileName }, 'view');
    } finally {
      setOpening(false);
    }
  };

  return (
    <li className="rounded-lg border border-border bg-card p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 items-center gap-2">
          <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">{rank}</span>
          <FileText className="h-4 w-4 shrink-0 text-muted-foreground" />
          <span className="truncate text-sm font-medium text-foreground" title={hit.originalFileName}>
            {hit.originalFileName}
          </span>
          <span className="shrink-0 text-xs text-muted-foreground">· passage {hit.chunkIndex + 1}</span>
        </div>
        <div className="flex shrink-0 items-center gap-1.5">
          <Badge variant={variant} className="text-[10px]" title={`Similarity ${hit.similarity.toFixed(2)}`}>
            {label} · {Math.round(hit.similarity * 100)}%
          </Badge>
          <Button type="button" variant="outline" size="sm" className="h-7 gap-1 px-2 text-xs" onClick={handleOpen} disabled={opening}>
            {opening ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Eye className="h-3.5 w-3.5" />}
            Open document
          </Button>
        </div>
      </div>

      <p className="mt-2 whitespace-pre-line break-words text-sm leading-relaxed text-foreground/90">{highlight(shown, question)}</p>

      {isLong && (
        <button type="button" onClick={() => setExpanded((v) => !v)} className="mt-1 inline-flex items-center gap-0.5 text-xs font-medium text-primary hover:underline">
          {expanded ? (
            <>
              Show less <ChevronUp className="h-3.5 w-3.5" />
            </>
          ) : (
            <>
              Show full passage <ChevronDown className="h-3.5 w-3.5" />
            </>
          )}
        </button>
      )}
    </li>
  );
}

/**
 * Semantic (RAG) search over this patient's uploaded documents: a doctor asks a question in
 * plain language and gets back the passages closest in meaning, each linked to its source
 * document. It retrieves passages — it doesn't generate an answer — so every result is text
 * that is actually in the patient's record.
 */
export function PatientAiSearchTab({ patient }: { patient: Patient }) {
  const [draft, setDraft] = useState('');
  const [question, setQuestion] = useState('');
  const search = usePatientDocumentSearchQuery(patient.id, question);
  const hits = search.data ?? [];

  const submit = (text: string) => {
    const trimmed = text.trim();
    setDraft(trimmed);
    setQuestion(trimmed);
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    submit(draft);
  };

  const clear = () => {
    setDraft('');
    setQuestion('');
  };

  return (
    <div className="flex flex-col gap-2.5">
      <div className="rounded-lg border border-border bg-card p-3">
        <div className="mb-2 flex items-center gap-1.5">
          <Sparkles className="h-4 w-4 text-primary" />
          <h2 className="text-sm font-semibold text-foreground">Search this patient’s documents</h2>
        </div>
        <p className="mb-3 text-xs text-muted-foreground">
          Ask in plain language — results are matched by meaning, not just exact words, across {patient.firstName}’s uploaded PDF, Word and Excel documents.
        </p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-2 sm:flex-row">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              placeholder="e.g. Which antibiotics were given during the last admission?"
              className="pl-8 pr-8"
              aria-label="Search question"
              maxLength={500}
            />
            {draft && (
              <button
                type="button"
                onClick={clear}
                className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-0.5 text-muted-foreground hover:text-foreground"
                aria-label="Clear search"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
          <Button type="submit" className="gap-1.5" disabled={!draft.trim() || search.isFetching}>
            {search.isFetching ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
            Search
          </Button>
        </form>

        <div className="mt-2.5 flex flex-wrap gap-1.5">
          {SUGGESTED_QUESTIONS.map((suggestion) => (
            <button
              key={suggestion}
              type="button"
              onClick={() => submit(suggestion)}
              className={cn(
                'rounded-full border border-border px-2.5 py-1 text-xs text-muted-foreground transition-colors hover:border-primary hover:text-primary',
                question === suggestion && 'border-primary bg-primary/5 text-primary',
              )}
            >
              {suggestion}
            </button>
          ))}
        </div>
      </div>

      {!question ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border bg-card px-4 py-10 text-center">
          <FileSearch className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm font-medium text-foreground">Ask a question to search this patient’s documents</p>
          <p className="max-w-md text-xs text-muted-foreground">
            Try a diagnosis, medication, lab test or instruction. Scanned images and photo-only PDFs aren’t searchable yet.
          </p>
        </div>
      ) : search.isPending ? (
        <div className="flex items-center justify-center gap-2 rounded-lg border border-border bg-card py-10 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Searching documents…
        </div>
      ) : search.isError ? (
        (() => {
          const { title, detail } = errorMessage(search.error);
          return (
            <div className="flex items-start gap-2 rounded-lg border border-destructive/40 bg-destructive/5 p-3 text-sm">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
              <div>
                <p className="font-medium text-destructive">{title}</p>
                <p className="text-muted-foreground">{detail}</p>
              </div>
            </div>
          );
        })()
      ) : hits.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border border-border bg-card px-4 py-10 text-center">
          <FileSearch className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm font-medium text-foreground">No matching passages found</p>
          <p className="max-w-md text-xs text-muted-foreground">
            This patient may have no searchable documents yet, or none that mention this. Try rephrasing the question.
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          <p className="px-0.5 text-xs text-muted-foreground">
            {hits.length} passage{hits.length === 1 ? '' : 's'} most related to “{question}”, best match first.
          </p>
          <ol className="flex flex-col gap-2">
            {hits.map((hit, index) => (
              <ResultCard key={`${hit.documentId}-${hit.chunkIndex}`} hit={hit} rank={index + 1} question={question} />
            ))}
          </ol>
          <p className="flex items-start gap-1.5 px-0.5 text-[11px] text-muted-foreground">
            <AlertTriangle className="mt-px h-3.5 w-3.5 shrink-0" />
            Results are passages copied from the patient’s documents, ranked by relevance — not a diagnosis or summary. Always confirm against the full document.
          </p>
        </div>
      )}
    </div>
  );
}
