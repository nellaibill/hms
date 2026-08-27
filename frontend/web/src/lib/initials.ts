// Shared between the Messaging feature's ConversationListItem and MessageThread avatars —
// both need the same "first letter of first name + first letter of last name" logic.
export function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[1]?.[0] ?? '')).toUpperCase() || '?';
}
