import type { Role } from '@/features/auth/types';

/**
 * Roles allowed to edit or delete a patient record from the UI. Mirrors the
 * `SELF_APPROVE_ROLES` gating pattern in
 * features/billing/components/DiscountApprovalControl.tsx. Delete is the more destructive
 * action, so it's restricted to a narrower set than Edit.
 *
 * UI-only, same posture as features/auth/RequireRole.tsx — the backend has no role/permission
 * policies yet (only authentication), so this is a UX guardrail, not the enforcement boundary.
 */
export const PATIENT_EDIT_ROLES: readonly Role[] = ['receptionist', 'admin', 'superAdmin'];
export const PATIENT_DELETE_ROLES: readonly Role[] = ['admin', 'superAdmin'];
