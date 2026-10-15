/** Mirrors HMS.Modules.Appointments.Contracts.AppointmentStatus — serialized as strings. Only
 * Booked can still change; the other three are final. */
export const APPOINTMENT_STATUSES = ['Booked', 'Arrived', 'Cancelled', 'NoShow'] as const;
export type AppointmentStatus = (typeof APPOINTMENT_STATUSES)[number];

/** Mirrors HMS.Modules.Appointments.Contracts.AppointmentSource. */
export const APPOINTMENT_SOURCES = ['Phone', 'InPerson', 'Other'] as const;
export type AppointmentSource = (typeof APPOINTMENT_SOURCES)[number];
