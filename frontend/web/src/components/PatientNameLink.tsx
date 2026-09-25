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
  /** Patient Details tab to land on (e.g. 'billing') — defaults to Overview. */
  tab?: string;
}

/** A patient's name rendered as a link to their existing Patient Details page
 * (/patients/registration/:id) — the same route and link styling PatientTable already uses, shared
 * so every list/table that shows a patient name navigates the same way. */
export function PatientNameLink({ patientId, children, className, newTab, tab }: PatientNameLinkProps) {
  return (
    <Link
      to={`/patients/registration/${patientId}${tab ? `?tab=${encodeURIComponent(tab)}` : ''}`}
      {...(newTab ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
      className={cn('font-medium text-foreground hover:text-primary hover:underline', className)}
    >
      {children}
    </Link>
  );
}
