import { useAuth } from '@/features/auth/AuthContext';

/**
 * Whether the signed-in user can read discharge summaries — the same two checks the API makes
 * (DischargeSummariesController: [RequireFeature("discharge-summary")] and
 * [RequirePermission("discharge-summary.view")]) and the summary pages' own routes make. Screens
 * that only link to or fetch a summary (the Admissions list's PDF button, the admission page's
 * Discharge Summary card) use it to leave themselves out, instead of calling the API and getting
 * a 403 — e.g. for a Receptionist, whose role has IPD access but no discharge summaries.
 */
export function useCanViewDischargeSummary(): boolean {
  const { hasFeature, hasPermission } = useAuth();
  return hasFeature('discharge-summary') && hasPermission('discharge-summary.view');
}
