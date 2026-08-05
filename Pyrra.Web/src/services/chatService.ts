import api from './api'
import type { ChatConversation, ChatMessage } from '../types/chat'
import type { UserSummary } from '../types/community'

// admins ativos, o jogador usa isso pra saber com quem pode começar uma conversa
export async function getActiveAdmins(): Promise<UserSummary[]> {
  const { data } = await api.get<UserSummary[]>('/api/chat/admins')
  return data
}

// conversas do usuário, uma por contraparte com quem já houve troca de mensagem
export async function getChatConversations(): Promise<ChatConversation[]> {
  const { data } = await api.get<ChatConversation[]>('/api/chat/conversas')
  return data
}

export async function getChatConversation(counterpartId: string): Promise<ChatMessage[]> {
  const { data } = await api.get<ChatMessage[]>(`/api/chat/conversas/${counterpartId}/mensagens`)
  return data
}

export async function sendChatMessage(counterpartId: string, content: string): Promise<ChatMessage> {
  const { data } = await api.post<ChatMessage>(`/api/chat/conversas/${counterpartId}/mensagens`, { content })
  return data
}

export async function markChatConversationAsRead(counterpartId: string): Promise<void> {
  await api.post(`/api/chat/conversas/${counterpartId}/marcar-lida`)
}
