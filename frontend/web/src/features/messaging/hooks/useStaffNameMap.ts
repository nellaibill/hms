import { useStaffDirectoryQuery } from './useStaffDirectoryQuery';

/** Resolves a participant UserId to a display name — ConversationResponse only ever carries
 * ids (see Conversation's own doc comment in frontend/shared/dtos/messaging/conversation.ts),
 * so the frontend does the lookup itself against the same staff directory the "start a
 * conversation" picker uses. Covers up to the directory's own 100-result cap; a hospital
 * with more active staff than that would need a real by-ids lookup, not built here. */
export function useStaffNameMap() {
  const directoryQuery = useStaffDirectoryQuery('');
  const nameById = new Map((directoryQuery.data ?? []).map((entry) => [entry.id, `${entry.firstName} ${entry.lastName}`.trim()]));

  return {
    nameById,
    isPending: directoryQuery.isPending,
  };
}
