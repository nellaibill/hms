import { useQuery } from '@tanstack/react-query';
import { getAllPatientsForReport } from '../patientReportData';

/** Unpaginated patient list for Patient Reports — see patientReportData.ts's own comment on
 * the known page-size limitation this works around. */
export function usePatientsForReportQuery() {
  return useQuery({
    queryKey: ['patients', 'report-all'],
    queryFn: () => getAllPatientsForReport(),
  });
}
