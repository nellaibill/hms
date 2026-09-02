import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { ConsultantName } from '@/components/ConsultantName';
import { DepartmentName } from '@/components/DepartmentName';
import { formatCurrency } from '../billingCalculations';
import type { RecentBill, RecentBillConsultant } from '../types';
import { PaymentStatusBadge } from './PaymentStatusBadge';

interface RecentPatientBillsTableProps {
  bills: RecentBill[];
}

/** Every consultant on the visit, comma-separated — mirrors PatientDetails.tsx's
 * ConsultantsCell (a visit can have 1, 2, or more consultants; all are shown directly rather
 * than truncated behind a hover-only "+N"). */
function ConsultantsCell({ consultants }: { consultants: RecentBillConsultant[] }) {
  if (consultants.length === 0) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <span className="inline-flex flex-wrap items-center gap-x-1">
      {consultants.map((consultant, index) => (
        <span key={consultant.consultantId}>
          <ConsultantName consultantId={consultant.consultantId} />
          {index < consultants.length - 1 && ','}
        </span>
      ))}
    </span>
  );
}

export function RecentPatientBillsTable({ bills }: RecentPatientBillsTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5">Patient Name</th>
            <th className="px-4 py-2.5">Age / Gender</th>
            <th className="px-4 py-2.5">Contact Number</th>
            <th className="px-4 py-2.5">UHID</th>
            <th className="px-4 py-2.5">Registration Type</th>
            <th className="px-4 py-2.5">Department</th>
            <th className="px-4 py-2.5">Consultant(s)</th>
            <th className="px-4 py-2.5">Appointment Date &amp; Time</th>
            <th className="px-4 py-2.5 text-right">Payment</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {bills.map((bill) => {
            const primaryDepartmentId = bill.consultants[0]?.departmentId;
            return (
              <tr key={bill.invoiceId} className="hover:bg-muted/30">
                <td className="whitespace-nowrap px-4 py-3">
                  <Link to={`/finance/accounts/${bill.invoiceId}`} className="font-medium text-foreground hover:text-primary hover:underline">
                    {bill.patientName}
                  </Link>
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">
                  {bill.age ?? '—'}
                  {bill.gender ? ` / ${bill.gender}` : ''}
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-muted-foreground">{bill.contactNumber ?? '—'}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">{bill.patientUhid}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  {bill.registrationType ? (
                    <Badge variant="outline" className="text-[10px]">
                      {bill.registrationType}
                    </Badge>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-foreground">
                  {primaryDepartmentId ? <DepartmentName departmentId={primaryDepartmentId} /> : <span className="text-muted-foreground">—</span>}
                </td>
                <td className="px-4 py-3 text-foreground">
                  <ConsultantsCell consultants={bill.consultants} />
                </td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-muted-foreground">
                  {new Date(bill.appointmentDateTime).toLocaleString('en-IN')}
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-col items-end gap-1">
                    {bill.isVoided ? <Badge variant="secondary">Voided</Badge> : <PaymentStatusBadge status={bill.paymentStatus} />}
                    <span className="font-medium tabular-nums text-foreground">{formatCurrency(bill.netAmount)}</span>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
