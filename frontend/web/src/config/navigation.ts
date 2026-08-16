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
  /** Backend permission-catalog module (e.g. "patient-management") whose
   * `.view` key gates this leaf's visibility — see docs behind
   * filterNavigationForPermissions below. Omitted (e.g. Dashboard) means
   * visible to every authenticated user regardless of permissions. */
  permission?: string;
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
  },
  {
    type: 'leaf',
    label: 'Patient Enquiry',
    path: '/patients/enquiry',
    icon: UserSearch,
    description: 'Find an existing patient by name, UHID, or phone to view or update their registration.',
    permission: 'patient-management',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Reception and Registration',
    path: '/patients/registration',
    icon: ClipboardList,
    description: 'Register a new patient or find an existing one to update their registration.',
    permission: 'patient-management',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Out Patient Department (OPD)',
    path: '/clinical/opd',
    icon: Stethoscope,
    description: 'Outpatient consultant queues, consultations, prescriptions, and investigation orders.',
    permission: 'clinical-care',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'In Patient Department (IPD)',
    path: '/clinical/ipd',
    icon: BedDouble,
    description: 'Inpatient bed/ward management, admissions, nursing charting, and discharge workflows.',
    permission: 'clinical-care',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Operation Theatre (OT)',
    path: '/clinical/ot',
    icon: Scissors,
    description: 'OT scheduling, consent management, surgical team assignment, and operative notes.',
    permission: 'clinical-care',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Pharmacy',
    path: '/pharmacy',
    icon: Pill,
    description: 'Prescription fulfillment queue, drug master, and stock/batch/expiry tracking.',
    permission: 'pharmacy',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Central Laboratory',
    path: '/diagnostics/lab',
    icon: FlaskConical,
    description: 'Test order queue, sample tracking, and result entry with critical value flagging.',
    permission: 'diagnostics',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Radiology',
    path: '/diagnostics/radiology',
    icon: ScanLine,
    description: 'Modality worklist, study review, and radiology report entry and release.',
    permission: 'diagnostics',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Blood Bank',
    path: '/diagnostics/blood-bank',
    icon: Droplet,
    description: 'Donor management, blood unit inventory, and issue/crossmatch tracking.',
    permission: 'diagnostics',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Ambulance',
    path: '/support/ambulance',
    icon: Truck,
    description: 'Dispatch requests, trip logs, and ambulance billing.',
    permission: 'support-services',
    section: 'Clinical',
  },
  {
    type: 'leaf',
    label: 'Accounts and Finance',
    path: '/finance/accounts',
    icon: Wallet,
    description: 'Unified invoice ledger, payments & refunds, insurance/TPA claims, and financial reports.',
    permission: 'finance-billing',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Records and Certificates',
    path: '/records/certificates',
    icon: FileBadge,
    description: 'Certificate issuance and medical records department (MRD) retrieval.',
    permission: 'records-compliance',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Document Management',
    path: '/documents',
    icon: Files,
    description: 'Centralized document repository — upload, preview, download, and archive files for any HMS record.',
    permission: 'records-compliance',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Human Resource Management (HR)',
    path: '/admin/hr',
    icon: UsersRound,
    description: 'Staff directory, roster/shift assignment, leave management, and credentialing.',
    permission: 'workforce-admin',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Activity Log',
    path: '/admin/activity-log',
    icon: History,
    description: 'System-wide, read-only audit trail of every module\'s write transactions.',
    permission: 'workforce-admin',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Hospital Inventory Management',
    path: '/support/inventory',
    icon: Boxes,
    description: 'Item master, stock ledger, reorder alerts, and vendor purchase orders.',
    permission: 'support-services',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Programmes and Calendar',
    path: '/engagement/programmes',
    icon: CalendarDays,
    description: 'Hospital events, health camps, and programme scheduling.',
    permission: 'engagement',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Messages and Notifications',
    path: '/engagement/messages',
    icon: MessageSquare,
    description: 'Notification center covering clinical, operational, administrative, and financial alerts.',
    permission: 'engagement',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Reports',
    path: '/reports',
    icon: BarChart3,
    description: 'Operational, clinical, financial, and statutory/regulatory reports.',
    permission: 'reports-analytics',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'E-MRD',
    path: '/records/emrd',
    icon: FolderOpen,
    description: 'Digital document repository for scanned and archived patient records.',
    permission: 'records-compliance',
    section: 'Administrative',
  },
  {
    type: 'leaf',
    label: 'Settings',
    path: '/admin/settings',
    icon: Settings,
    description: 'Roles & permissions, master data, and system configuration.',
    permission: 'identity-administration',
    section: 'Administrative',
  },
];

/** Permission-driven — a leaf shows only when the signed-in user holds that
 * leaf's `${permission}.view` key (leaves with no `permission` are always
 * visible, e.g. Dashboard). Replaces the old hardcoded role-name allowlist:
 * an admin can now grant/revoke sidebar access purely by editing a role's
 * permissions, without a frontend code change. */
export function filterNavigationForPermissions(hasPermission: (key: string) => boolean): NavNode[] {
  return navigationTree.filter((node) => !node.permission || hasPermission(`${node.permission}.view`));
}

export function findLeafByPath(path: string): NavLeaf | undefined {
  return navigationTree.find((node) => node.path === path);
}

/** Every leaf route in the tree — the single source routes.tsx generates pages from. */
export function getAllLeaves(): NavLeaf[] {
  return navigationTree;
}
