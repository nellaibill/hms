/** Mirrors HMS.Modules.Calendar.Contracts.EventType — serialized as strings (JsonStringEnumConverter). */
export const EVENT_TYPES = ['Holiday', 'HospitalEvent', 'DoctorLeave', 'Meeting', 'Training', 'Maintenance', 'Other'] as const;
export type EventType = (typeof EVENT_TYPES)[number];
