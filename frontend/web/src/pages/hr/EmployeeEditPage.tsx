import { ApiError, type EmployeeFormValues, type UpdateEmployeeRequest } from '@hms/shared';
import { ArrowLeft, Loader2, Users } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { EmployeeForm, useEmployeeQuery, useUpdateEmployeeMutation } from '../../features/employees';

export default function EmployeeEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: employee, isPending, isError } = useEmployeeQuery(id);
  const mutation = useUpdateEmployeeMutation();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading employee…
      </div>
    );
  }

  if (isError || !employee) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Employee not found.
        </p>
      </div>
    );
  }

  function handleSubmit(values: EmployeeFormValues) {
    const request: UpdateEmployeeRequest = {
      firstName: values.firstName,
      lastName: values.lastName,
      gender: values.gender,
      dateOfBirth: values.dateOfBirth,
      phone: values.phone,
      email: values.email,
      address: values.address,
      emergencyContactName: values.emergencyContactName,
      emergencyContactPhone: values.emergencyContactPhone,
      departmentId: values.departmentId,
      designationId: values.designationId,
      employeeType: values.employeeType,
      joiningDate: values.joiningDate,
      employmentStatus: values.employmentStatus,
      reportingManagerId: values.reportingManagerId || null,
      profilePhotoUrl: values.profilePhotoUrl || null,
      userId: values.userId || null,
      isActive: values.isActive,
    };
    mutation.mutate(
      { id: id as string, request },
      { onSuccess: () => navigate(`/admin/hr/employees/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to={`/admin/hr/employees/${id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to employee
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Users className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            Edit {employee.firstName} {employee.lastName}
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this employee's record.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <EmployeeForm
          mode="edit"
          employeeId={employee.id}
          submitLabel="Save Changes"
          isSubmitting={mutation.isPending}
          apiError={mutation.error instanceof ApiError ? mutation.error : null}
          defaultValues={{
            employeeCode: employee.employeeCode,
            firstName: employee.firstName,
            lastName: employee.lastName,
            gender: employee.gender,
            dateOfBirth: employee.dateOfBirth,
            phone: employee.phone,
            email: employee.email,
            address: employee.address,
            emergencyContactName: employee.emergencyContactName,
            emergencyContactPhone: employee.emergencyContactPhone,
            departmentId: employee.departmentId,
            designationId: employee.designationId,
            employeeType: employee.employeeType,
            joiningDate: employee.joiningDate,
            employmentStatus: employee.employmentStatus,
            reportingManagerId: employee.reportingManagerId ?? '',
            profilePhotoUrl: employee.profilePhotoUrl ?? '',
            userId: employee.userId ?? '',
            isActive: employee.isActive,
          }}
          onSubmit={handleSubmit}
        />
      </div>
    </div>
  );
}
