import { useQuery } from '@tanstack/react-query';
import { getAllPatientVisitsForReport } from '../patientReportData';

/** Unpaginated, cross-patient, date-range-filtered visit list for Patient Reports — refetches
 * whenever the range changes, since (unlike usePatientsForReportQuery) the date filter is
 * pushed server-side here. */
export function usePatientVisitsForReportQuery(from: string, to: string) {
  return useQuery({
    queryKey: ['patient-visits', 'report-all', from, to],
    queryFn: () => getAllPatientVisitsForReport(from, to),
  });
}
