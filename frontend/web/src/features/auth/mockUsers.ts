import type { MockUser, RoleDefinition } from './types';

// Roles map 1:1 to the 10 role-based dashboards in docs/DashboardSpecifications.md.
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

// Doctor entries reuse names from config/hospitalData.ts's DOCTORS roster so the same
// consultants recur across login, the topbar "signed in as", patients, and the dashboard.
export const mockUsers: MockUser[] = [
  { id: 'u-1', name: 'Kalaivani Ramasamy', role: 'superAdmin', username: 'superadmin', department: 'Administration' },
  { id: 'u-2', name: 'Muthuraman Pillai', role: 'admin', username: 'admin', department: 'Administration' },
  { id: 'u-3', name: 'Anitha Selvaraj', role: 'receptionist', username: 'reception1', department: 'Front Desk' },
  { id: 'u-4', name: 'Dr. Karthikeyan', role: 'doctor', username: 'dr.karthikeyan', department: 'General Medicine' },
  { id: 'u-5', name: 'Sr. Kalaiselvi Nadar', role: 'nurse', username: 'nurse.kalaiselvi', department: 'IPD — Ward 2' },
  { id: 'u-6', name: 'Elango Raj', role: 'labTechnician', username: 'lab.elango', department: 'Central Laboratory' },
  { id: 'u-7', name: 'Dr. Nirmala', role: 'radiologist', username: 'dr.nirmala', department: 'Radiology' },
  { id: 'u-8', name: 'Murugesan Pillai', role: 'pharmacist', username: 'pharmacy.murugesan', department: 'Pharmacy' },
  { id: 'u-9', name: 'Deepa Rajendran', role: 'hr', username: 'hr.deepa', department: 'Human Resources' },
  { id: 'u-10', name: 'Balamurugan Chettiar', role: 'accounts', username: 'accounts.bala', department: 'Accounts & Finance' },
];

export function findMockUserByRole(role: string): MockUser | undefined {
  return mockUsers.find((user) => user.role === role);
}
