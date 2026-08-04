import type { Role } from '../auth/types';

/**
 * Seed data for the Users module's offline mock store — and, crucially, the SAME 10 records
 * `features/auth/mockAuthStore.ts` signs in against, so the "Sign in as" dropdown, the Users
 * list, and every StaffId picker across the app (StaffSelect) show one consistent set of
 * people rather than three unrelated demo datasets. One account per `roleDefinitions.ts`
 * entry — `loginType` is the "Sign in as" value that must be selected to sign into this
 * account (mirrors HMS.Modules.Identity.Application.LoginTypes' role-matching rule).
 */
export interface MockUserSeed {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  roleId: string;
  roleName: string;
  loginType: Role;
}

export const MOCK_USERS: MockUserSeed[] = [
  {
    id: 'user-001',
    username: 'superadmin',
    firstName: 'Ananya',
    lastName: 'Raghunathan',
    email: 'ananya.raghunathan@hms.demo',
    phoneNumber: '9840011201',
    roleId: 'role-superadmin',
    roleName: 'Super Admin',
    loginType: 'superAdmin',
  },
  {
    id: 'user-002',
    username: 'admin',
    firstName: 'Muthu',
    lastName: 'Vairavan',
    email: 'muthu.vairavan@hms.demo',
    phoneNumber: '9840011202',
    roleId: 'role-admin',
    roleName: 'Hospital Administrator',
    loginType: 'admin',
  },
  {
    id: 'user-003',
    username: 'receptionist',
    firstName: 'Kavya',
    lastName: 'Shanmugam',
    email: 'kavya.shanmugam@hms.demo',
    phoneNumber: '9840011203',
    roleId: 'role-receptionist',
    roleName: 'Receptionist',
    loginType: 'receptionist',
  },
  {
    id: 'user-004',
    username: 'doctor',
    firstName: 'Karthikeyan',
    lastName: 'Subramaniam',
    email: 'karthikeyan.subramaniam@hms.demo',
    phoneNumber: '9840011204',
    roleId: 'role-doctor',
    roleName: 'Doctor / Consultant',
    loginType: 'doctor',
  },
  {
    id: 'user-005',
    username: 'nurse',
    firstName: 'Devi',
    lastName: 'Pandiyan',
    email: 'devi.pandiyan@hms.demo',
    phoneNumber: '9840011205',
    roleId: 'role-nurse',
    roleName: 'Nurse',
    loginType: 'nurse',
  },
  {
    id: 'user-006',
    username: 'labtech',
    firstName: 'Ilamurugu',
    lastName: 'Chelladurai',
    email: 'ilamurugu.chelladurai@hms.demo',
    phoneNumber: '9840011206',
    roleId: 'role-labtechnician',
    roleName: 'Lab Technician',
    loginType: 'labTechnician',
  },
  {
    id: 'user-007',
    username: 'radiologist',
    firstName: 'Nirmala',
    lastName: 'Ravichandran',
    email: 'nirmala.ravichandran@hms.demo',
    phoneNumber: '9840011207',
    roleId: 'role-radiologist',
    roleName: 'Radiologist',
    loginType: 'radiologist',
  },
  {
    id: 'user-008',
    username: 'pharmacist',
    firstName: 'Senthil',
    lastName: 'Murugesan',
    email: 'senthil.murugesan@hms.demo',
    phoneNumber: '9840011208',
    roleId: 'role-pharmacist',
    roleName: 'Pharmacist',
    loginType: 'pharmacist',
  },
  {
    id: 'user-009',
    username: 'hr',
    firstName: 'Meenakshi',
    lastName: 'Balasubramaniam',
    email: 'meenakshi.balasubramaniam@hms.demo',
    phoneNumber: '9840011209',
    roleId: 'role-hrofficer',
    roleName: 'HR Officer',
    loginType: 'hr',
  },
  {
    id: 'user-010',
    username: 'accounts',
    firstName: 'Arun',
    lastName: 'Krishnamurthy',
    email: 'arun.krishnamurthy@hms.demo',
    phoneNumber: '9840011210',
    roleId: 'role-accountsofficer',
    roleName: 'Accounts Officer',
    loginType: 'accounts',
  },
];
