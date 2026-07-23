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

export const mockUsers: MockUser[] = [
  { id: 'u-1', name: 'Ananya Rao', role: 'superAdmin', username: 'superadmin', department: 'Administration' },
  { id: 'u-2', name: 'Vikram Shetty', role: 'admin', username: 'admin', department: 'Administration' },
  { id: 'u-3', name: 'Priya Nair', role: 'receptionist', username: 'reception1', department: 'Front Desk' },
  { id: 'u-4', name: 'Dr. Arjun Menon', role: 'doctor', username: 'dr.menon', department: 'General Medicine' },
  { id: 'u-5', name: 'Sr. Lakshmi Pillai', role: 'nurse', username: 'nurse.pillai', department: 'IPD — Ward 3' },
  { id: 'u-6', name: 'Karthik Iyer', role: 'labTechnician', username: 'lab.karthik', department: 'Central Laboratory' },
  { id: 'u-7', name: 'Dr. Meera Krishnan', role: 'radiologist', username: 'dr.krishnan', department: 'Radiology' },
  { id: 'u-8', name: 'Suresh Kumar', role: 'pharmacist', username: 'pharmacy.suresh', department: 'Pharmacy' },
  { id: 'u-9', name: 'Divya Balan', role: 'hr', username: 'hr.divya', department: 'Human Resources' },
  { id: 'u-10', name: 'Ramesh Pillai', role: 'accounts', username: 'accounts.ramesh', department: 'Accounts & Finance' },
];

export function findMockUserByRole(role: string): MockUser | undefined {
  return mockUsers.find((user) => user.role === role);
}
