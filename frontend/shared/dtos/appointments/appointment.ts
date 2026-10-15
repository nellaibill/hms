import type { AppointmentSource, AppointmentStatus } from '../../enums/appointments';

/** Mirrors HMS.Modules.Appointments.Contracts.AppointmentResponse. Dates are "yyyy-MM-dd" and
 * times "HH:mm:ss", both hospital-local (DateOnly/TimeOnly on the API). */
export interface Appointment {
  id: string;
  appointmentNumber: string;
  /** Null while the booking is for a first-time caller who isn't registered yet. */
  patientId?: string | null;
  patientName: string;
  phoneNumber: string;
  uhid?: string | null;
  isNewPatient: boolean;
  departmentId: string;
  consultantId: string;
  appointmentTypeId?: string | null;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  /** 1 = the consultant's first visiting session, 2 = the second. */
  session: number;
  tokenNumber: number;
  status: AppointmentStatus;
  source: AppointmentSource;
  notes?: string | null;
  cancelReason?: string | null;
  cancelledAt?: string | null;
  visitId?: string | null;
  arrivedAt?: string | null;
  noShowAt?: string | null;
  rescheduleCount: number;
  createdAt: string;
  createdBy?: string | null;
  updatedAt?: string | null;
}

/** Mirrors BookAppointmentRequest — either patientId, or patientName + phoneNumber. */
export interface BookAppointmentRequest {
  patientId?: string | null;
  patientName?: string | null;
  phoneNumber?: string | null;
  departmentId: string;
  consultantId: string;
  appointmentTypeId?: string | null;
  appointmentDate: string;
  startTime: string;
  source: AppointmentSource;
  notes?: string | null;
}

export interface RescheduleAppointmentRequest {
  appointmentDate: string;
  startTime: string;
}

export interface CancelAppointmentRequest {
  reason: string;
}

export interface MarkAppointmentArrivedRequest {
  patientId: string;
  visitId: string;
}

export interface AppointmentListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  fromDate?: string;
  toDate?: string;
  consultantId?: string;
  departmentId?: string;
  patientId?: string;
  status?: AppointmentStatus;
}

/** Mirrors AppointmentSlotResponse. Seat N of a slot gets token firstToken + N - 1. */
export interface AppointmentSlot {
  session: number;
  startTime: string;
  endTime: string;
  firstToken: number;
  capacity: number;
  booked: number;
  available: number;
  isBlocked: boolean;
  blockReason?: string | null;
  isPast: boolean;
  isBookable: boolean;
}

/** Mirrors AppointmentSlotsResponse. When isWorkingDay is false, message says why. */
export interface AppointmentSlots {
  consultantId: string;
  date: string;
  isWorkingDay: boolean;
  message?: string | null;
  slotMinutes: number;
  patientsPerSlot: number;
  slots: AppointmentSlot[];
}

export interface AppointmentSummary {
  date: string;
  total: number;
  booked: number;
  arrived: number;
  cancelled: number;
  noShow: number;
}

/** Mirrors CreateConsultantBlockRequest. session null = the whole day. */
export interface CreateConsultantBlockRequest {
  consultantId: string;
  fromDate: string;
  toDate: string;
  session?: number | null;
  reason: string;
}

export interface ConsultantBlock {
  id: string;
  consultantId: string;
  fromDate: string;
  toDate: string;
  session?: number | null;
  reason: string;
  createdAt: string;
  createdBy?: string | null;
}

/** The new block, plus the Booked appointments that now fall inside it (not cancelled
 * automatically — reception reschedules or cancels them). */
export interface ConsultantBlockCreated {
  block: ConsultantBlock;
  affectedAppointments: Appointment[];
}

export interface ConsultantBlockListQuery {
  consultantId?: string;
  fromDate?: string;
  toDate?: string;
}
