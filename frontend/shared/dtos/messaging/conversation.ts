import type { ConversationType } from '../../enums';

/** Mirrors HMS.Modules.Messaging.Contracts.ConversationResponse. `participantUserIds`
 * carries only ids — resolving display names is the frontend's job (a batched call to
 * Identity's staff-directory endpoint), not something the backend decorates this DTO with. */
export interface Conversation {
  id: string;
  type: ConversationType;
  title?: string | null;
  lastMessageAt?: string | null;
  participantUserIds: string[];
  unreadCount: number;
  createdAt: string;
}

/** Mirrors HMS.Modules.Messaging.Contracts.CreateConversationRequest — the caller is added
 * automatically; `participantUserIds` lists only the *other* participants. */
export interface CreateConversationRequest {
  type: ConversationType;
  title?: string | null;
  participantUserIds: string[];
}
