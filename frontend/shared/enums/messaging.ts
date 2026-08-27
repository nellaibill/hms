/** Mirrors HMS.Modules.Messaging.Contracts.ConversationType — serialized as strings
 * (JsonStringEnumConverter). */
export const CONVERSATION_TYPES = ['OneToOne', 'Group'] as const;
export type ConversationType = (typeof CONVERSATION_TYPES)[number];
