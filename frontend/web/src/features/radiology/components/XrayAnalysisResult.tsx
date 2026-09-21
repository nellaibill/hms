import type { XrayAiAnalysisResponse } from '@hms/shared';
import { Sparkles } from 'lucide-react';
import { parseXrayAnalysis } from '../parseXrayAnalysis';

/** One image's AI draft. The disclaimer is always shown — this is never a diagnosis. */
export function XrayAnalysisResult({ result }: { result: XrayAiAnalysisResponse }) {
  const sections = parseXrayAnalysis(result.analysis);

  return (
    <div className="space-y-3 rounded-lg border border-fuchsia-200 bg-fuchsia-50/50 p-4 dark:border-fuchsia-500/30 dark:bg-fuchsia-500/5">
      <div className="flex items-center gap-2 text-xs font-medium text-fuchsia-700 dark:text-fuchsia-300">
        <Sparkles className="h-3.5 w-3.5" />
        AI draft — not reviewed · {result.model}
      </div>

      {sections.map((section, index) => (
        <div key={`${section.title}-${index}`}>
          {section.title && <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{section.title}</p>}
          <p className="whitespace-pre-wrap text-sm text-foreground">{section.body || '—'}</p>
        </div>
      ))}

      <p className="border-t border-fuchsia-200 pt-3 text-xs text-muted-foreground dark:border-fuchsia-500/30">{result.disclaimer}</p>
    </div>
  );
}
