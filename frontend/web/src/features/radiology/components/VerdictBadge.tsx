import { AlertOctagon, AlertTriangle, CheckCircle2, HelpCircle, type LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { FractureVerdict } from '../parseXrayAnalysis';

const VERDICT_STYLES: Record<FractureVerdict, { label: string; icon: LucideIcon; className: string }> = {
  none: {
    label: 'No obvious fracture',
    icon: CheckCircle2,
    className: 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300',
  },
  possible: {
    label: 'Possible fracture',
    icon: AlertTriangle,
    className: 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300',
  },
  identified: {
    label: 'Fracture identified',
    icon: AlertOctagon,
    className: 'border-red-200 bg-red-50 text-red-700 dark:border-red-500/30 dark:bg-red-500/10 dark:text-red-300',
  },
  unknown: {
    label: 'Verdict unclear',
    icon: HelpCircle,
    className: 'border-slate-200 bg-slate-50 text-slate-700 dark:border-slate-500/30 dark:bg-slate-500/10 dark:text-slate-300',
  },
};

/** The AI's fracture verdict as a colour-coded pill. Colour is never the only signal — the label and
 * icon carry the meaning too. */
export function VerdictBadge({ verdict, className }: { verdict: FractureVerdict; className?: string }) {
  const { label, icon: Icon, className: tone } = VERDICT_STYLES[verdict];
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold', tone, className)}>
      <Icon className="h-3.5 w-3.5" aria-hidden />
      {label}
    </span>
  );
}
