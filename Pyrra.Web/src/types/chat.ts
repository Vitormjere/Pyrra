// Espelha os DTOs de Pyrra.Api/Dtos/Chat — chat entre admin e jogadores (Fase Admin-4a). Sem
// tempo real ainda: mensagens aparecem ao enviar (local) ou ao (re)carregar a conversa — a versão
// ao vivo chega na Fase Admin-4b, por cima do mesmo modelo.

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

// Uma linha da lista de conversas — a contraparte com quem já houve troca de mensagem.
export interface ChatConversation {
  counterpart: UserSummary
  lastMessageContent: string
  lastMessageAt: string
  lastMessageFromMe: boolean
  unreadCount: number
}
