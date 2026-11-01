import { humanize } from '@/features/patients/humanize';

/** humanize() for status tokens, except "AutoCompleted", which reads as "Auto-completed"
 * rather than "Auto Completed". Safe for the other OPD tabs' status lists too — none of them
 * has that token. */
export function opdStatusLabel(status: string): string {
  return status === 'AutoCompleted' ? 'Auto-completed' : humanize(status);
}
