// A role name containing "consultant" (e.g. "Doctor / Consultant") is treated as a
// clinical role that needs/gets a consultant mapping — freeform Role names have no
// dedicated "is clinical" flag today, so this mirrors the substring check that's the
// closest available signal, same spirit as LoginTypes.cs's own name-based role matching.
// Mirrors the backend's ClaimsPrincipalExtensions.GetScopedConsultantId — shared across
// features (auth session scoping, User Create/Edit forms), so it lives in lib/, not inside
// any one feature.
export function isConsultantRoleName(name: string | undefined): boolean {
  return Boolean(name?.toLowerCase().includes('consultant'));
}
