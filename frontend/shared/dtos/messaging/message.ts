/** Mirrors HMS.Modules.Messaging.Contracts.MessageResponse. */
export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  body: string;
  createdAt: string;
}

/** Mirrors HMS.Modules.Messaging.Contracts.SendMessageRequest. */
export interface SendMessageRequest {
  body: string;
}
