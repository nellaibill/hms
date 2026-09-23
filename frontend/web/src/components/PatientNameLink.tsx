import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

interface PatientNameLinkProps {
  patientId: string;
  children: ReactNode;
  className?: string;
}

/** A patient's name rendered as a link to their existing Patient Details page
 * (/patients/registration/:id) — the same route and link styling PatientTable already uses, shared
 * so every list/table that shows a patient name navigates the same way. */
export function PatientNameLink({ patientId, children, className }: PatientNameLinkProps) {
  return (
    <Link to={`/patients/registration/${patientId}`} className={cn('font-medium text-foreground hover:text-primary hover:underline', className)}>
      {children}
    </Link>
  );
}
