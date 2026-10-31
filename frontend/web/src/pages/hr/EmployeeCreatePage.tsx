import { ApiError, type CreateEmployeeRequest, type EmployeeFormValues } from '@hms/shared';
import { useQueryClient } from '@tanstack/react-query';
import { Users } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { useToast } from '@/components/ui/toast-context';
import { toEmployeeLoginRequest, type LoginAccessDraft } from '@/features/users';
import { EmployeeForm, useCreateEmployeeMutation } from '../../features/employees';

export default function EmployeeCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const mutation = useCreateEmployeeMutation();

  function handleSubmit(values: EmployeeFormValues, loginDraft: LoginAccessDraft) {
    const login = toEmployeeLoginRequest(loginDraft);
    const request: CreateEmployeeRequest = {
      ...values,
      reportingManagerId: values.reportingManagerId || null,
      profilePhotoUrl: values.profilePhotoUrl || null,
      userId: null,
      login,
    };
    mutation.mutate(request, {
      onSuccess: (employee) => {
        if (login) {
          void queryClient.invalidateQueries({ queryKey: ['users'] });
          toast({ title: 'Employee and login saved', description: `${employee.firstName} can now log in as ${login.username}.`, variant: 'success' });
        }
        navigate(`/admin/hr/employees/${employee.id}`);
      },
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
