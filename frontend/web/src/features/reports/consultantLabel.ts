import { resolveRecordLabel } from '@/features/masters';

/** A billed consultant's name for the report tables and exports — "No consultant" for a line
 * billed without one (e.g. an injection or a file). */
export function consultantLabel(consultantId: string | undefined): string {
  return consultantId ? resolveRecordLabel('consultant', consultantId) : 'No consultant';
}
