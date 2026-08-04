import { z } from 'zod';

/**
 * Mirrors HMS.Modules.HR.Application.Validators.CreateWeeklyRosterRequestValidator /
 * UpdateWeeklyRosterRequestValidator — only WeekStartDate and DepartmentId are required.
 * DepartmentId has no backing directory yet (no Department module exists — see
 * HMS.Modules.HR.Domain.WeeklyRoster's own doc comment), so it's a plain GUID-format text
 * field rather than a picker; Published/PublishedDate aren't user-editable here at all —
 * they're managed by the dedicated Publish action (Phase 5), not this form (client-side
 * convenience only, the backend remains authoritative — docs/ApiStandards.md §7,
 * docs/FrontendArchitecture.md §9).
 */
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const weeklyRosterSchema = z.object({
  weekStartDate: z.string().trim().min(1, 'Week start date is required'),
  departmentId: z.string().trim().min(1, 'Department ID is required').regex(guidPattern, 'Enter a valid Department ID (GUID)'),
});

export const createWeeklyRosterSchema = weeklyRosterSchema;
export const updateWeeklyRosterSchema = weeklyRosterSchema;

export type WeeklyRosterFormValues = z.infer<typeof weeklyRosterSchema>;
