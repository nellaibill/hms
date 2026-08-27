import { z } from 'zod';
import { EMPLOYEE_GENDERS, EMPLOYEE_TYPES, EMPLOYMENT_STATUSES } from '../../enums/hr';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateEmployeeRequestValidator /
 * UpdateEmployeeRequestValidator exactly — FirstName/LastName/DateOfBirth/Phone/Email/
 * Address/EmergencyContactName/EmergencyContactPhone/DepartmentId/DesignationId/JoiningDate
 * are required, with EmployeeCode required only on create (natural key, immutable after
 * creation). ReportingManagerId/ProfilePhotoUrl/UserId have no backend rule beyond their
 * type, so no extra constraint is added here (client-side convenience only, the backend
 * remains authoritative — docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const employeeCommonSchema = {
  firstName: z.string().trim().min(1, 'First name is required').max(100),
  lastName: z.string().trim().min(1, 'Last name is required').max(100),
  gender: z.enum(EMPLOYEE_GENDERS, { message: 'Gender is required' }),
  dateOfBirth: z.string().trim().min(1, 'Date of birth is required'),
  phone: z.string().trim().min(1, 'Phone is required').max(20),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email').max(256),
  address: z.string().trim().min(1, 'Address is required').max(500),
  emergencyContactName: z.string().trim().min(1, 'Emergency contact name is required').max(100),
  emergencyContactPhone: z.string().trim().min(1, 'Emergency contact phone is required').max(20),
  departmentId: z.string().trim().min(1, 'Department is required'),
  designationId: z.string().trim().min(1, 'Designation is required'),
  employeeType: z.enum(EMPLOYEE_TYPES, { message: 'Employee type is required' }),
  joiningDate: z.string().trim().min(1, 'Joining date is required'),
  employmentStatus: z.enum(EMPLOYMENT_STATUSES, { message: 'Employment status is required' }),
  reportingManagerId: z.string().trim().optional().or(z.literal('')),
  profilePhotoUrl: z.string().trim().optional().or(z.literal('')),
  userId: z.string().trim().optional().or(z.literal('')),
  isActive: z.boolean(),
};

export const createEmployeeSchema = z.object({
  employeeCode: z.string().trim().min(1, 'Employee code is required').max(30),
  ...employeeCommonSchema,
});

export const updateEmployeeSchema = z.object(employeeCommonSchema);

export type EmployeeFormValues = z.infer<typeof createEmployeeSchema>;
