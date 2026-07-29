import { ArrowLeft, Loader2, Pencil, UserRound } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { PatientDetails, PatientDocumentUpload, usePatientQuery } from '../../features/patients';

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

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <Button
          asChild
          variant="outline"
          className="absolute right-6 top-1/2 -translate-y-1/2 gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
        >
          <Link to={`/patients/registration/${patient.id}/edit`}>
            <Pencil className="h-4 w-4" />
            Edit
          </Link>
        </Button>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserRound className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            {patient.title} {patient.firstName} {patient.lastName}
          </h1>
        </div>
        <p className="font-mono text-sm text-page-banner-foreground/85">{patient.uhid}</p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="mx-auto grid w-full max-w-[1920px] grid-cols-1 gap-4 lg:grid-cols-3">
          <div className="lg:col-span-2">
            <PatientDetails patient={patient} />
          </div>
          <div>
            <PatientDocumentUpload patientId={patient.id} />
          </div>
        </div>
      </div>
    </div>
  );
}
