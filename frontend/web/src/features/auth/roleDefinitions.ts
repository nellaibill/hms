import type { RoleDefinition } from './types';

// Roles map 1:1 to the 10 role-based dashboards in docs/DashboardSpecifications.md, and to
// HMS.Modules.Identity.Application.LoginTypes.RoleNameByLoginType on the backend — this is
// the web login page's "Sign In As" dropdown.
export const roleDefinitions: RoleDefinition[] = [
  { id: 'superAdmin', label: 'Super Admin', description: 'Full system access across every module' },
  { id: 'admin', label: 'Hospital Administrator', description: 'Hospital-wide operational oversight' },
  { id: 'receptionist', label: 'Receptionist', description: 'Patient registration and front-desk billing' },
  { id: 'doctor', label: 'Doctor / Consultant', description: 'OPD, IPD, and OT clinical care' },
  { id: 'nurse', label: 'Nurse', description: 'Ward care, vitals, and medication administration' },
  { id: 'labTechnician', label: 'Lab Technician', description: 'Test orders, samples, and results' },
  { id: 'radiologist', label: 'Radiologist', description: 'Imaging worklist and radiology reports' },
  { id: 'pharmacist', label: 'Pharmacist', description: 'Prescription fulfillment and stock' },
  { id: 'hr', label: 'HR Officer', description: 'Staff, roster, leave, and credentialing' },
  { id: 'accounts', label: 'Accounts Officer', description: 'Billing, payments, and financial reports' },
];
