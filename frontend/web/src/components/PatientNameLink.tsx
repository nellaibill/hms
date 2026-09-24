import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

interface PatientNameLinkProps {
  patientId: string;
  children: ReactNode;
  className?: string;
  /** Opens Patient Details in a new tab — for pages with an in-progress form (billing, admission,
   * dispense), where navigating away in place would lose what's been entered. */
  newTab?: boolean;
}

/** A patient's name rendered as a link to their existing Patient Details page
 * (/patients/registration/:id) — the same route and link styling PatientTable already uses, shared
 * so every list/table that shows a patient name navigates the same way. */
export function PatientNameLink({ patientId, children, className, newTab }: PatientNameLinkProps) {
  return (
    <Link
      to={`/patients/registration/${patientId}`}
      {...(newTab ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
      className={cn('font-medium text-foreground hover:text-primary hover:underline', className)}
    >
      {children}
    </Link>
  );
}
