import { ApiError, type CreateEmployeeRequest, type EmployeeFormValues } from '@hms/shared';
import { ArrowLeft, Users } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { EmployeeForm, useCreateEmployeeMutation } from '../../features/employees';

export default function EmployeeCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreateEmployeeMutation();

  function handleSubmit(values: EmployeeFormValues) {
    const request: CreateEmployeeRequest = {
      ...values,
      reportingManagerId: values.reportingManagerId || null,
      profilePhotoUrl: values.profilePhotoUrl || null,
      userId: values.userId || null,
    };
    mutation.mutate(request, {
      onSuccess: (employee) => navigate(`/admin/hr/employees/${employee.id}`),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr/employees" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to employees
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Users className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Employee</h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Create a new employee record.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <EmployeeForm
          mode="create"
          submitLabel="Create Employee"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
