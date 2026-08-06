import {
  LayoutDashboard,
  ClipboardList,
  UserSearch,
  Stethoscope,
  BedDouble,
  Scissors,
  FlaskConical,
  ScanLine,
  Droplet,
  Pill,
  Truck,
  Boxes,
  Wallet,
  FileBadge,
  FolderOpen,
  UsersRound,
  History,
  Settings,
  CalendarDays,
  MessageSquare,
  BarChart3,
  Files,
  type LucideIcon,
} from 'lucide-react';
import type { Role } from '@/features/auth/types';

// Flat, two-section Primary Navigation model — Dashboard stands alone at the
// top, everything else lives directly under a "Clinical" or "Administrative"
// section with no further nesting. This file is the single source of truth
// the Sidebar, Breadcrumbs, and route table are all generated from.

export interface NavLeaf {
  type: 'leaf';
  label: string;
  path: string;
  icon: LucideIcon;
  description: string;
  roles: Role[] | 'all';
  /** Section header rendered above this item in the sidebar — top-level items only. */
  section?: string;
}

export type NavNode = NavLeaf;

export const navigationTree: NavNode[] = [
  {
    type: 'leaf',
    label: 'Dashboard',
    path: '/dashboard',
    icon: LayoutDashboard,
    description: 'Executive overview — census, income & expense, HR presence, and plans/projects status.',
    roles: 'all',
  },
  {
    type: 'leaf',
    label: 'Patient Enquiry',
    path: '/patients/enquiry',
    icon: UserSearch,
    description: 'Find an existing patient by name, UHID, or phone to view or update their registration.',
    roles: ['receptionist', 'doctor', 'nurse'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Reception and Registration',
    path: '/patients/registration',
    icon: ClipboardList,
    description: 'Register a new patient or find an existing one to update their registration.',
    roles: ['receptionist', 'doctor', 'nurse'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Out Patient Department (OPD)',
    path: '/clinical/opd',
    icon: Stethoscope,
    description: 'Outpatient consultant queues, consultations, prescriptions, and investigation orders.',
    roles: ['doctor', 'nurse'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'In Patient Department (IPD)',
    path: '/clinical/ipd',
    icon: BedDouble,
    description: 'Inpatient bed/ward management, admissions, nursing charting, and discharge workflows.',
    roles: ['doctor', 'nurse'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Operation Theatre (OT)',
    path: '/clinical/ot',
    icon: Scissors,
    description: 'OT scheduling, consent management, surgical team assignment, and operative notes.',
    roles: ['doctor', 'nurse'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Pharmacy',
    path: '/pharmacy',
    icon: Pill,
    description: 'Prescription fulfillment queue, drug master, and stock/batch/expiry tracking.',
    roles: ['pharmacist', 'doctor'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Central Laboratory',
    path: '/diagnostics/lab',
    icon: FlaskConical,
    description: 'Test order queue, sample tracking, and result entry with critical value flagging.',
    roles: ['labTechnician', 'radiologist', 'doctor'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Radiology',
    path: '/diagnostics/radiology',
    icon: ScanLine,
    description: 'Modality worklist, study review, and radiology report entry and release.',
    roles: ['labTechnician', 'radiologist', 'doctor'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Blood Bank',
    path: '/diagnostics/blood-bank',
    icon: Droplet,
    description: 'Donor management, blood unit inventory, and issue/crossmatch tracking.',
    roles: ['labTechnician', 'radiologist', 'doctor'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Ambulance',
    path: '/support/ambulance',
    icon: Truck,
    description: 'Dispatch requests, trip logs, and ambulance billing.',
    roles: ['admin'],
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Accounts and Finance',
    path: '/finance/accounts',
    icon: Wallet,
    description: 'Unified invoice ledger, payments & refunds, insurance/TPA claims, and financial reports.',
    roles: ['accounts', 'receptionist'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Records and Certificates',
    path: '/records/certificates',
    icon: FileBadge,
    description: 'Certificate issuance and medical records department (MRD) retrieval.',
    roles: ['doctor', 'admin'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Document Management',
    path: '/documents',
    icon: Files,
    description: 'Centralized document repository — upload, preview, download, and archive files for any HMS record.',
    roles: 'all',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Human Resource Management (HR)',
    path: '/admin/hr',
    icon: UsersRound,
    description: 'Staff directory, roster/shift assignment, leave management, and credentialing.',
    roles: ['hr', 'admin'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Activity Log',
    path: '/admin/activity-log',
    icon: History,
    description: 'System-wide, read-only audit trail of every module\'s write transactions.',
    roles: ['hr', 'admin'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Hospital Inventory Management',
    path: '/support/inventory',
    icon: Boxes,
    description: 'Item master, stock ledger, reorder alerts, and vendor purchase orders.',
    roles: ['admin'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Programmes and Calendar',
    path: '/engagement/programmes',
    icon: CalendarDays,
    description: 'Hospital events, health camps, and programme scheduling.',
    roles: 'all',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Messages and Notifications',
    path: '/engagement/messages',
    icon: MessageSquare,
    description: 'Notification center covering clinical, operational, administrative, and financial alerts.',
    roles: 'all',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Reports',
    path: '/reports',
    icon: BarChart3,
    description: 'Operational, clinical, financial, and statutory/regulatory reports.',
    roles: ['admin', 'accounts', 'hr'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'E-MRD',
    path: '/records/emrd',
    icon: FolderOpen,
    description: 'Digital document repository for scanned and archived patient records.',
    roles: ['doctor', 'admin'],
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Settings',
    path: '/admin/settings',
    icon: Settings,
    description: 'Roles & permissions, master data, and system configuration.',
    roles: ['hr', 'admin'],
    section: 'Administrative',
  },
];

export function filterNavigationForRole(role: Role): NavNode[] {
  if (role === 'superAdmin' || role === 'admin') return navigationTree;

  const nodeVisible = (roles: Role[] | 'all') => roles === 'all' || roles.includes(role);

  return navigationTree.filter((node) => nodeVisible(node.roles));
}

export function findLeafByPath(path: string): NavLeaf | undefined {
  return navigationTree.find((node) => node.path === path);
}

/** Every leaf route in the tree — the single source routes.tsx generates pages from. */
export function getAllLeaves(): NavLeaf[] {
  return navigationTree;
}
