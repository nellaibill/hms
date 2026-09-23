import { ArrowLeft, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { PatientDetails, PatientSummaryCard, usePatientQuery } from '../../features/patients';

export default function PatientViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: patient, isPending, isError } = usePatientQuery(id);
  const [activeTab, setActiveTab] = useState('overview');

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
      <div className="px-3 pt-2 lg:px-4 print:hidden">
        <Link
          to="/patients/registration"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to registration
        </Link>
      </div>

      <div className="flex flex-1 flex-col gap-3 p-3 pt-2 lg:p-4 lg:pt-2">
        <div className="flex w-full flex-col gap-3">
          <PatientSummaryCard patient={patient} onAddDocument={() => setActiveTab('documents')} />
          <PatientDetails patient={patient} activeTab={activeTab} onActiveTabChange={setActiveTab} />
        </div>
      </div>
    </div>
  );
}
