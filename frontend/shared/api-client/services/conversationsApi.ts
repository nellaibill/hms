import { API_ROUTES } from '../../constants';
import type { Conversation, CreateConversationRequest, Message, SendMessageRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedMessages {
  items: Message[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Messaging module (HMS.Modules.Messaging.Endpoints.
 * ConversationsController), built on the shared HTTP client.
 */
export class ConversationsApi {
  constructor(private readonly client: HttpClient) {}

  async getMine(): Promise<Conversation[]> {
    const response = await this.client.get<Conversation[]>(API_ROUTES.conversations.base);
    return response.data;
  }

  /** Starts a one-to-one or group conversation — a one-to-one request that already has a
   * thread between the same two users returns that existing conversation instead of a
   * duplicate (see ConversationService.CreateAsync's own doc comment). */
  async create(request: CreateConversationRequest): Promise<Conversation> {
    const response = await this.client.post<Conversation>(API_ROUTES.conversations.base, request);
    return response.data;
  }

  async getMessages(conversationId: string, page = 1, pageSize = 50): Promise<PagedMessages> {
    const response = await this.client.get<Message[]>(API_ROUTES.conversations.messages(conversationId), {
      query: { page, pageSize },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async sendMessage(conversationId: string, request: SendMessageRequest): Promise<Message> {
    const response = await this.client.post<Message>(API_ROUTES.conversations.messages(conversationId), request);
    return response.data;
  }

  async markRead(conversationId: string): Promise<void> {
    await this.client.put(API_ROUTES.conversations.read(conversationId));
  }
}
