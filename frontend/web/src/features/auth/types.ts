export type Role =
  | 'superAdmin'
  | 'admin'
  | 'receptionist'
  | 'doctor'
  | 'nurse'
  | 'labTechnician'
  | 'radiologist'
  | 'pharmacist'
  | 'hr'
  | 'accounts';

export interface RoleDefinition {
  id: Role;
  label: string;
  description: string;
}

export interface MockUser {
  id: string;
  name: string;
  role: Role;
  username: string;
  department?: string;
}

export interface AuthState {
  user: MockUser | null;
}
