/** Mirrors HMS.Modules.HR.Contracts.HREnums — serialized as strings (JsonStringEnumConverter). */
export const ASSIGNMENT_STATUSES = ['Scheduled', 'Completed', 'Cancelled'] as const;
export type AssignmentStatus = (typeof ASSIGNMENT_STATUSES)[number];

/** Just the binary availability state — anything more specific is free text in `reason`. */
export const AVAILABILITY_STATUSES = ['Available', 'Unavailable'] as const;
export type AvailabilityStatus = (typeof AVAILABILITY_STATUSES)[number];

export const SWAP_REQUEST_STATUSES = ['Pending', 'Approved', 'Rejected', 'Cancelled'] as const;
export type SwapRequestStatus = (typeof SWAP_REQUEST_STATUSES)[number];
