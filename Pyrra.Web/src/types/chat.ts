// espelha os DTOs de chat do backend, mensagens entre admin e jogadores

import type { UserSummary } from './community'

export interface ChatMessage {
  id: string
  sender: UserSummary
  recipientId: string
  content: string
  createdAt: string
  /** null = não lida. */
  readAt: string | null
}

// uma linha da lista de conversas, com quem já houve troca de mensagem
export interface ChatConversation {
  counterpart: UserSummary
  lastMessageContent: string
  lastMessageAt: string
  lastMessageFromMe: boolean
  unreadCount: number
}
