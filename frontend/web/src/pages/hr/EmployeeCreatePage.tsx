import { ApiError, type CreateEmployeeRequest, type EmployeeFormValues } from '@hms/shared';
import { Users } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
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
      <PageBanner
        icon={Users}
        title="New Employee"
        subtitle="Create a new employee record."
        backTo="/admin/hr/employees"
        backLabel="Back to employees"
      />

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
