import {
  LayoutDashboard,
  Search,
  ClipboardList,
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
  type LucideIcon,
} from 'lucide-react';
import type { Role } from '@/features/auth/types';

// Domain grouping mirrors docs/InformationArchitecture.md's 10-domain
// Primary Navigation model (plus Home) — this file is the single source of
// truth the Sidebar, Breadcrumbs, and route table are all generated from.

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

export interface NavGroup {
  type: 'group';
  label: string;
  icon: LucideIcon;
  roles: Role[] | 'all';
  children: NavLeaf[];
  /** Section header rendered above this item in the sidebar — top-level items only. */
  section?: string;
}

export type NavNode = NavLeaf | NavGroup;

export const navigationTree: NavNode[] = [
  {
    type: 'leaf',
    label: 'Home',
    path: '/dashboard',
    icon: LayoutDashboard,
    description: 'Executive overview — census, income & expense, HR presence, and plans/projects status.',
    roles: 'all',
    section: 'Overview',
  },
  {
    type: 'group',
    label: 'Patient Management',
    icon: Search,
    section: 'Clinical',
    roles: ['receptionist', 'doctor', 'nurse'],
    children: [
      {
        type: 'leaf',
        label: 'Patient Enquiry',
        path: '/patients/enquiry',
        icon: Search,
        description: 'Search patients by name, age, UHID, or phone, and view the aggregated Patient 360 record.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Reception & Registration',
        path: '/patients/registration',
        icon: ClipboardList,
        description: 'Register new patients or update existing ones, capturing demographics, contacts, and encounter details.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'group',
    label: 'Clinical Care',
    icon: Stethoscope,
    roles: ['doctor', 'nurse'],
    section: 'Clinical',
    children: [
      {
        type: 'leaf',
        label: 'OPD',
        path: '/clinical/opd',
        icon: Stethoscope,
        description: 'Outpatient consultant queues, consultations, prescriptions, and investigation orders.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'IPD',
        path: '/clinical/ipd',
        icon: BedDouble,
        description: 'Inpatient bed/ward management, admissions, nursing charting, and discharge workflows.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Operation Theatre',
        path: '/clinical/ot',
        icon: Scissors,
        description: 'OT scheduling, consent management, surgical team assignment, and operative notes.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'group',
    label: 'Diagnostics & Ancillary',
    icon: FlaskConical,
    roles: ['labTechnician', 'radiologist', 'doctor'],
    section: 'Clinical',
    children: [
      {
        type: 'leaf',
        label: 'Central Laboratory',
        path: '/diagnostics/lab',
        icon: FlaskConical,
        description: 'Test order queue, sample tracking, and result entry with critical value flagging.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Radiology',
        path: '/diagnostics/radiology',
        icon: ScanLine,
        description: 'Modality worklist, study review, and radiology report entry and release.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Blood Bank',
        path: '/diagnostics/blood-bank',
        icon: Droplet,
        description: 'Donor management, blood unit inventory, and issue/crossmatch tracking.',
        roles: 'all',
      },
    ],
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
    type: 'group',
    label: 'Support Services',
    icon: Truck,
    roles: ['admin'],
    section: 'Operations',
    children: [
      {
        type: 'leaf',
        label: 'Ambulance',
        path: '/support/ambulance',
        icon: Truck,
        description: 'Dispatch requests, trip logs, and ambulance billing.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Hospital Inventory Management',
        path: '/support/inventory',
        icon: Boxes,
        description: 'Item master, stock ledger, reorder alerts, and vendor purchase orders.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'leaf',
    label: 'Finance & Billing',
    path: '/finance/accounts',
    icon: Wallet,
    description: 'Unified invoice ledger, payments & refunds, insurance/TPA claims, and financial reports.',
    roles: ['accounts', 'receptionist'],
    section: 'Operations',
  },
  {
    type: 'group',
    label: 'Records & Compliance',
    icon: FileBadge,
    roles: ['doctor', 'admin'],
    section: 'Operations',
    children: [
      {
        type: 'leaf',
        label: 'Records & Certificates',
        path: '/records/certificates',
        icon: FileBadge,
        description: 'Certificate issuance and medical records department (MRD) retrieval.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'E-MRD',
        path: '/records/emrd',
        icon: FolderOpen,
        description: 'Digital document repository for scanned and archived patient records.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'group',
    label: 'Workforce & Administration',
    icon: UsersRound,
    roles: ['hr', 'admin'],
    section: 'Administration',
    children: [
      {
        type: 'leaf',
        label: 'Human Resources',
        path: '/admin/hr',
        icon: UsersRound,
        description: 'Staff directory, roster/shift assignment, leave management, and credentialing.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Activity Log',
        path: '/admin/activity-log',
        icon: History,
        description: 'System-wide, read-only audit trail of every module\'s write transactions.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Settings',
        path: '/admin/settings',
        icon: Settings,
        description: 'Roles & permissions, master data, and system configuration.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'group',
    label: 'Engagement',
    icon: CalendarDays,
    roles: 'all',
    section: 'Administration',
    children: [
      {
        type: 'leaf',
        label: 'Programmes & Calendar',
        path: '/engagement/programmes',
        icon: CalendarDays,
        description: 'Hospital events, health camps, and programme scheduling.',
        roles: 'all',
      },
      {
        type: 'leaf',
        label: 'Messages & Notifications',
        path: '/engagement/messages',
        icon: MessageSquare,
        description: 'Notification center covering clinical, operational, administrative, and financial alerts.',
        roles: 'all',
      },
    ],
  },
  {
    type: 'leaf',
    label: 'Reports & Analytics',
    path: '/reports',
    icon: BarChart3,
    description: 'Operational, clinical, financial, and statutory/regulatory reports.',
    roles: ['admin', 'accounts', 'hr'],
    section: 'Administration',
  },
];

export function filterNavigationForRole(role: Role): NavNode[] {
  if (role === 'superAdmin' || role === 'admin') return navigationTree;

  const nodeVisible = (roles: Role[] | 'all') => roles === 'all' || roles.includes(role);

  return navigationTree
    .filter((node) => nodeVisible(node.roles))
    .map((node) => {
      if (node.type === 'leaf') return node;
      const children = node.children.filter((child) => nodeVisible(child.roles));
      return { ...node, children };
    })
    .filter((node) => node.type === 'leaf' || node.children.length > 0);
}

export function findLeafByPath(path: string): NavLeaf | undefined {
  for (const node of navigationTree) {
    if (node.type === 'leaf' && node.path === path) return node;
    if (node.type === 'group') {
      const match = node.children.find((child) => child.path === path);
      if (match) return match;
    }
  }
  return undefined;
}

export function findGroupForPath(path: string): NavGroup | undefined {
  return navigationTree.find(
    (node): node is NavGroup => node.type === 'group' && node.children.some((child) => child.path === path),
  );
}

/** Every leaf route in the tree, flattened — the single source routes.tsx generates pages from. */
export function getAllLeaves(): NavLeaf[] {
  return navigationTree.flatMap((node) => (node.type === 'leaf' ? [node] : node.children));
}
