import { DEPARTMENTS } from '@/config/hospitalData';
import { addDays, todayIso } from './utils/date';
import type { CalendarEvent, EventType } from './types';

interface SeedSpec {
  offsetDays: number;
  span: number;
  title: string;
  description: string;
  eventType: EventType;
  department?: string;
  allDay: boolean;
  reminder?: { type: 'Notification' | 'Email' | 'SMS'; beforeDays: number };
}

// Seeded relative to "today" (not fixed calendar dates) so the demo always has a populated
// current month, plus a spread into the adjacent months for Prev/Next navigation to show
// something. A few well-known fixed holidays are added separately below.
const SEED: SeedSpec[] = [
  { offsetDays: -18, span: 0, title: 'Fire Safety Drill', description: 'Mandatory evacuation drill for all floors.', eventType: 'Training', department: 'Emergency', allDay: false },
  { offsetDays: -12, span: 0, title: 'Dr. Priya — Leave', description: 'Approved personal leave.', eventType: 'DoctorLeave', department: 'Neurology', allDay: true },
  { offsetDays: -9, span: 1, title: 'Annual Health Camp', description: 'Free health check-up camp open to the public.', eventType: 'HospitalEvent', department: undefined, allDay: true },
  { offsetDays: -6, span: 0, title: 'Department Heads Meeting', description: 'Monthly coordination meeting with all department heads.', eventType: 'Meeting', department: undefined, allDay: false, reminder: { type: 'Notification', beforeDays: 1 } },
  { offsetDays: -3, span: 0, title: 'MRI Machine Maintenance', description: 'Scheduled preventive maintenance — Radiology suite offline.', eventType: 'Maintenance', department: 'Radiology', allDay: false },
  { offsetDays: -1, span: 0, title: 'Dr. Arun Kumar — Leave', description: 'Approved medical leave.', eventType: 'DoctorLeave', department: 'General Surgery', allDay: true },
  { offsetDays: 0, span: 0, title: 'Infection Control Training', description: 'Refresher training for nursing staff.', eventType: 'Training', department: 'ICU', allDay: false },
  { offsetDays: 1, span: 0, title: 'Blood Donation Drive', description: 'Community blood donation camp in the main lobby.', eventType: 'HospitalEvent', department: undefined, allDay: true },
  { offsetDays: 2, span: 0, title: 'Vendor Review — Pharmacy Supplies', description: 'Quarterly vendor performance review.', eventType: 'Meeting', department: 'Pharmacy', allDay: false, reminder: { type: 'Email', beforeDays: 1 } },
  { offsetDays: 3, span: 0, title: 'Backup Generator Test', description: 'Monthly load test of the emergency power backup.', eventType: 'Maintenance', department: undefined, allDay: false },
  { offsetDays: 5, span: 0, title: 'Dr. Meenakshi — Leave', description: 'Approved conference leave.', eventType: 'DoctorLeave', department: 'Pediatrics', allDay: true },
  { offsetDays: 5, span: 0, title: 'New Equipment Orientation', description: 'Orientation for the new lab analyzer.', eventType: 'Training', department: 'Laboratory', allDay: false },
  { offsetDays: 6, span: 0, title: 'Independence Day', description: 'National holiday — hospital administrative offices closed.', eventType: 'Holiday', department: undefined, allDay: true },
  { offsetDays: 8, span: 0, title: 'Budget Planning Meeting', description: 'FY budget planning session with finance and admin.', eventType: 'Meeting', department: undefined, allDay: false },
  { offsetDays: 9, span: 0, title: 'HVAC Servicing — OT Block', description: 'Air handling unit servicing for the operation theatre block.', eventType: 'Maintenance', department: 'Emergency', allDay: false },
  { offsetDays: 11, span: 0, title: 'Dr. Senthil Kumar — Leave', description: 'Approved family emergency leave.', eventType: 'DoctorLeave', department: 'Dermatology', allDay: true },
  { offsetDays: 13, span: 0, title: 'CME Session: Cardiac Emergencies', description: 'Continuing medical education session for consulting physicians.', eventType: 'Training', department: 'Cardiology', allDay: false, reminder: { type: 'Notification', beforeDays: 2 } },
  { offsetDays: 14, span: 0, title: 'Hospital Foundation Day', description: 'Annual celebration marking the hospital\'s founding.', eventType: 'HospitalEvent', department: undefined, allDay: true },
  { offsetDays: 16, span: 0, title: 'IT Systems Upgrade Window', description: 'HMS maintenance window — brief downtime expected after hours.', eventType: 'Maintenance', department: undefined, allDay: false },
  { offsetDays: 18, span: 0, title: 'Nursing Staff Town Hall', description: 'Open forum for nursing staff feedback and updates.', eventType: 'Meeting', department: undefined, allDay: false },
  { offsetDays: 21, span: 0, title: 'Dr. Lakshmi — Leave', description: 'Approved planned leave.', eventType: 'DoctorLeave', department: 'ENT', allDay: true },
  { offsetDays: 23, span: 0, title: 'Diabetes Awareness Camp', description: 'Free screening and awareness camp for the local community.', eventType: 'HospitalEvent', department: undefined, allDay: true },
  { offsetDays: 25, span: 0, title: 'Elevator Maintenance', description: 'Routine servicing — Block B elevators offline for 2 hours.', eventType: 'Maintenance', department: undefined, allDay: false },
  { offsetDays: 27, span: 0, title: 'Compliance & Audit Review', description: 'Internal compliance review meeting with department heads.', eventType: 'Meeting', department: undefined, allDay: false },
  { offsetDays: 30, span: 0, title: 'New Hire Orientation', description: 'Orientation program for staff joining this month.', eventType: 'Training', department: undefined, allDay: false },
  { offsetDays: 32, span: 0, title: 'Gandhi Jayanti', description: 'National holiday.', eventType: 'Holiday', department: undefined, allDay: true },
  { offsetDays: -25, span: 0, title: 'Ambulance Fleet Servicing', description: 'Scheduled servicing for the ambulance fleet.', eventType: 'Other', department: undefined, allDay: false },
  { offsetDays: -30, span: 0, title: 'Dr. Balasubramanian — Leave', description: 'Approved leave.', eventType: 'DoctorLeave', department: 'Gynecology', allDay: true },
];

function toReminderAt(startDate: string, beforeDays: number): string {
  return `${addDays(startDate, -beforeDays)}T08:00:00.000Z`;
}

export function buildMockEvents(): CalendarEvent[] {
  const today = todayIso();
  const now = new Date().toISOString();

  return SEED.map((seed, index) => {
    const startDate = addDays(today, seed.offsetDays);
    const endDate = addDays(startDate, seed.span);
    return {
      id: `evt-${String(index + 1).padStart(3, '0')}`,
      title: seed.title,
      description: seed.description,
      eventType: seed.eventType,
      department: seed.department,
      startDate,
      endDate,
      allDay: seed.allDay,
      reminderEnabled: Boolean(seed.reminder),
      reminderType: seed.reminder?.type,
      reminderAt: seed.reminder ? toReminderAt(startDate, seed.reminder.beforeDays) : undefined,
      createdBy: 'Admin User',
      createdAt: now,
      updatedAt: now,
    } satisfies CalendarEvent;
  });
}

export { DEPARTMENTS };
