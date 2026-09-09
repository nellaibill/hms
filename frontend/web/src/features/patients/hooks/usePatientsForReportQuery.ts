import { useQuery } from '@tanstack/react-query';
import { getAllPatientsForReport } from '../patientReportData';

/** Date-range-scoped, capped patient list for Patient Reports — see patientReportData.ts's own
 * comments on why the range is pushed server-side and why the result is capped rather than
 * exhaustive. `truncated` tells the page whether to disclose that the charts are based on a
 * bounded sample, not a full count, for the current range. */
export function usePatientsForReportQuery(from: string, to: string) {
  return useQuery({
    queryKey: ['patients', 'report-all', from, to],
    queryFn: () => getAllPatientsForReport(from, to),
  });
}
