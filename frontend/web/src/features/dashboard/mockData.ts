import {
  Ambulance,
  BedDouble,
  Calendar as CalendarIcon,
  FlaskConical,
  Pill,
  ReceiptText,
  Scissors,
  ScanLine,
  Users,
  Wallet,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

/* ---------------------------------- KPIs --------------------------------- */

export interface KpiDatum {
  id: string;
  label: string;
  value: string;
  icon: LucideIcon;
  changePct: number;
  /** Whether an upward move is a good thing for this metric — decides badge color, not arrow direction. */
  goodWhen: 'up' | 'down';
  sparkline: number[];
}

export const kpiData: KpiDatum[] = [
  { id: 'patients-today', label: 'Patients Today', value: '428', icon: Users, changePct: 6.2, goodWhen: 'up', sparkline: [312, 340, 355, 330, 380, 402, 390, 428] },
  { id: 'op-visits', label: 'OP Visits', value: '312', icon: ReceiptText, changePct: 4.8, goodWhen: 'up', sparkline: [260, 275, 268, 290, 300, 285, 305, 312] },
  { id: 'ip-admissions', label: 'IP Admissions', value: '47', icon: BedDouble, changePct: -2.1, goodWhen: 'up', sparkline: [52, 50, 55, 48, 51, 49, 46, 47] },
  { id: 'revenue-today', label: 'Revenue Today', value: '₹8.42L', icon: Wallet, changePct: 11.4, goodWhen: 'up', sparkline: [6.1, 6.8, 6.5, 7.2, 7.6, 7.9, 8.1, 8.42] },
  { id: 'pending-bills', label: 'Pending Bills', value: '63', icon: ReceiptText, changePct: 3.5, goodWhen: 'down', sparkline: [48, 51, 55, 58, 54, 59, 61, 63] },
  { id: 'available-beds', label: 'Available Beds', value: '86', icon: BedDouble, changePct: -4.5, goodWhen: 'up', sparkline: [102, 98, 95, 93, 90, 88, 89, 86] },
];

/* ------------------------------ Operational ------------------------------ */

export interface AppointmentRow {
  time: string;
  patient: string;
  doctor: string;
  type: string;
}

export const todaysAppointments: AppointmentRow[] = [
  { time: '09:00', patient: 'Ravi Kumar', doctor: 'Dr. Menon', type: 'Follow-up' },
  { time: '09:30', patient: 'Sita Iyer', doctor: 'Dr. Krishnan', type: 'New consult' },
  { time: '10:15', patient: 'Farhan Ali', doctor: 'Dr. Menon', type: 'Follow-up' },
  { time: '11:00', patient: 'Meera Nair', doctor: 'Dr. Rao', type: 'Procedure review' },
  { time: '11:45', patient: 'Ashok Pillai', doctor: 'Dr. Krishnan', type: 'New consult' },
];

export interface LabPendingRow {
  test: string;
  patient: string;
  priority: 'Routine' | 'Urgent' | 'Critical';
}

export const labPending: LabPendingRow[] = [
  { test: 'CBC + ESR', patient: 'Ravi Kumar', priority: 'Routine' },
  { test: 'Lipid Profile', patient: 'Meera Nair', priority: 'Routine' },
  { test: 'Troponin-I', patient: 'Ashok Pillai', priority: 'Critical' },
  { test: 'Blood Culture', patient: 'Farhan Ali', priority: 'Urgent' },
  { test: 'HbA1c', patient: 'Sita Iyer', priority: 'Routine' },
];

export interface RadiologyRow {
  study: string;
  patient: string;
  modality: 'X-Ray' | 'CT' | 'MRI' | 'USG';
}

export const radiologyQueue: RadiologyRow[] = [
  { study: 'Chest PA View', patient: 'Farhan Ali', modality: 'X-Ray' },
  { study: 'Abdomen Plain', patient: 'Meera Nair', modality: 'USG' },
  { study: 'Brain w/ Contrast', patient: 'Ashok Pillai', modality: 'MRI' },
  { study: 'Spine Screening', patient: 'Ravi Kumar', modality: 'CT' },
];

export interface OtScheduleRow {
  time: string;
  procedure: string;
  surgeon: string;
  room: string;
}

export const otSchedule: OtScheduleRow[] = [
  { time: '08:30', procedure: 'Appendectomy', surgeon: 'Dr. Varma', room: 'OT-1' },
  { time: '10:00', procedure: 'Knee Arthroscopy', surgeon: 'Dr. Rao', room: 'OT-2' },
  { time: '13:00', procedure: 'Cataract Surgery', surgeon: 'Dr. Krishnan', room: 'OT-1' },
];

export interface AmbulanceRow {
  vehicle: string;
  status: 'Available' | 'On Trip' | 'Returning';
  location: string;
}

export const ambulanceStatus: AmbulanceRow[] = [
  { vehicle: 'AMB-01', status: 'Available', location: 'Main Bay' },
  { vehicle: 'AMB-02', status: 'On Trip', location: 'Enroute — MG Road' },
  { vehicle: 'AMB-03', status: 'Returning', location: 'City Hospital' },
  { vehicle: 'AMB-04', status: 'Available', location: 'Main Bay' },
];

export interface PharmacyRow {
  rx: string;
  patient: string;
  items: number;
}

export const pharmacyQueue: PharmacyRow[] = [
  { rx: 'RX-10231', patient: 'Ravi Kumar', items: 3 },
  { rx: 'RX-10232', patient: 'Sita Iyer', items: 1 },
  { rx: 'RX-10233', patient: 'Ashok Pillai', items: 5 },
  { rx: 'RX-10234', patient: 'Meera Nair', items: 2 },
];

export const operationalWidgets = [
  { title: "Today's Appointments", icon: CalendarIcon, count: 18, countLabel: 'today', link: '/clinical/opd' },
  { title: 'Lab Pending', icon: FlaskConical, count: 12, countLabel: 'pending', link: '/diagnostics/lab' },
  { title: 'Radiology Queue', icon: ScanLine, count: 7, countLabel: 'in queue', link: '/diagnostics/radiology' },
  { title: 'OT Schedule', icon: Scissors, count: 3, countLabel: 'today', link: '/clinical/ot' },
  { title: 'Ambulance Status', icon: Ambulance, count: 2, countLabel: 'available', link: '/support/ambulance' },
  { title: 'Pharmacy Queue', icon: Pill, count: 9, countLabel: 'pending', link: '/pharmacy' },
];

/* -------------------------------- Analytics ------------------------------- */

export const opIpTrend = [
  { day: 'Mon', op: 268, ip: 42 },
  { day: 'Tue', op: 285, ip: 45 },
  { day: 'Wed', op: 301, ip: 51 },
  { day: 'Thu', op: 279, ip: 48 },
  { day: 'Fri', op: 322, ip: 53 },
  { day: 'Sat', op: 296, ip: 49 },
  { day: 'Sun', op: 312, ip: 47 },
];

export const revenueExpense = [
  { month: 'Feb', revenue: 186, expense: 142 },
  { month: 'Mar', revenue: 198, expense: 150 },
  { month: 'Apr', revenue: 205, expense: 148 },
  { month: 'May', revenue: 221, expense: 159 },
  { month: 'Jun', revenue: 214, expense: 162 },
  { month: 'Jul', revenue: 238, expense: 168 },
];

/* ---------------------------- Hospital overview ---------------------------- */

export interface DepartmentPerformanceRow {
  department: string;
  utilization: number;
}

export const departmentPerformance: DepartmentPerformanceRow[] = [
  { department: 'Cardiology', utilization: 92 },
  { department: 'General Medicine', utilization: 88 },
  { department: 'Pediatrics', utilization: 85 },
  { department: 'Orthopedics', utilization: 78 },
  { department: 'Radiology', utilization: 70 },
  { department: 'ENT', utilization: 65 },
];

export interface WardOccupancyRow {
  ward: string;
  occupied: number;
  total: number;
}

export const wardOccupancy: WardOccupancyRow[] = [
  { ward: 'General Ward', occupied: 78, total: 100 },
  { ward: 'ICU', occupied: 18, total: 20 },
  { ward: 'Maternity', occupied: 22, total: 30 },
  { ward: 'Pediatric', occupied: 14, total: 24 },
];

export interface DoctorAvailabilityRow {
  name: string;
  specialty: string;
  status: 'Available' | 'In Consultation' | 'On Leave';
}

export const doctorAvailability: DoctorAvailabilityRow[] = [
  { name: 'Dr. Arjun Menon', specialty: 'General Medicine', status: 'In Consultation' },
  { name: 'Dr. Meera Krishnan', specialty: 'Radiology', status: 'Available' },
  { name: 'Dr. Priya Rao', specialty: 'Orthopedics', status: 'Available' },
  { name: 'Dr. Suresh Varma', specialty: 'Surgery', status: 'In Consultation' },
  { name: 'Dr. Kavya Nair', specialty: 'Pediatrics', status: 'On Leave' },
];

/* -------------------------------- Productivity ------------------------------ */

export interface CalendarEvent {
  date: number;
  label: string;
}

export const calendarEvents: CalendarEvent[] = [
  { date: 5, label: 'Board Review' },
  { date: 14, label: 'NABH Audit' },
  { date: 23, label: 'Blood Donation Camp' },
];

export interface DashboardNotification {
  id: string;
  title: string;
  detail: string;
  time: string;
  severity: 'info' | 'warning' | 'critical';
}

export const dashboardNotifications: DashboardNotification[] = [
  { id: 'dn-1', title: 'Critical lab value', detail: 'Troponin-I flagged for Ashok Pillai', time: '4 min ago', severity: 'critical' },
  { id: 'dn-2', title: 'Discount approval requested', detail: 'Reception requested 15% off Invoice #2291', time: '22 min ago', severity: 'warning' },
  { id: 'dn-3', title: 'Roster published', detail: "Next week's nursing roster is live", time: '1 hr ago', severity: 'info' },
  { id: 'dn-4', title: 'Blood stock low', detail: 'O-negative below reorder threshold', time: '2 hr ago', severity: 'warning' },
];

export interface PendingTask {
  id: string;
  title: string;
  due: string;
  priority: 'Low' | 'Medium' | 'High';
}

export const pendingTasks: PendingTask[] = [
  { id: 'pt-1', title: 'Approve pharmacy purchase order #PO-448', due: 'Today, 4:00 PM', priority: 'High' },
  { id: 'pt-2', title: 'Review discount request — Invoice #2291', due: 'Today, 5:30 PM', priority: 'Medium' },
  { id: 'pt-3', title: 'Sign off nursing roster — Ward 3', due: 'Tomorrow', priority: 'Medium' },
  { id: 'pt-4', title: 'Renew Dr. Varma’s license record', due: 'In 3 days', priority: 'Low' },
];

export interface RecentActivity {
  id: string;
  actor: string;
  action: string;
  time: string;
}

export const recentActivities: RecentActivity[] = [
  { id: 'ra-1', actor: 'Priya Nair', action: 'Registered new patient — Grace Hopper (UHID 000512)', time: '6 min ago' },
  { id: 'ra-2', actor: 'Dr. Menon', action: 'Completed consultation for Ravi Kumar', time: '18 min ago' },
  { id: 'ra-3', actor: 'Suresh Kumar', action: 'Dispensed RX-10229 from Pharmacy', time: '35 min ago' },
  { id: 'ra-4', actor: 'System', action: 'Nightly bed-occupancy report generated', time: '2 hr ago' },
  { id: 'ra-5', actor: 'Ramesh Pillai', action: 'Reconciled 42 invoices for yesterday', time: '3 hr ago' },
];
