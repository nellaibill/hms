// A role name containing "consultant" (e.g. "Doctor / Consultant") is treated as a
// clinical role that needs a consultant mapping — freeform Role names have no dedicated
// "is clinical" flag today, so this mirrors the substring check that's the closest
// available signal, same spirit as LoginTypes.cs's own name-based role matching.
// Shared by UserCreateForm and UserEditForm so the rule can't drift between the two.
export function isConsultantRoleName(name: string | undefined): boolean {
  return Boolean(name?.toLowerCase().includes('consultant'));
}
