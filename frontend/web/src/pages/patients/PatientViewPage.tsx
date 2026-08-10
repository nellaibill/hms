import { ArrowLeft, Loader2 } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { PatientDetails, PatientSummaryCard, usePatientQuery } from '../../features/patients';

export default function PatientViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: patient, isPending, isError } = usePatientQuery(id);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading patient…
      </div>
    );
  }

  if (isError || !patient) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Patient not found.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link
          to="/patients/registration"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to registration
        </Link>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="mx-auto grid w-full max-w-[1600px] grid-cols-1 items-start gap-4 lg:grid-cols-[300px_1fr]">
          <div className="lg:sticky lg:top-4">
            <PatientSummaryCard patient={patient} />
          </div>
          <PatientDetails patient={patient} />
        </div>
      </div>
    </div>
  );
}
