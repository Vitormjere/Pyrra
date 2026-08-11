import api from './api'
import type { TeamChatMessage } from '../types/teamChat'

export async function getTeamChatMessages(teamId: string): Promise<TeamChatMessage[]> {
  const { data } = await api.get<TeamChatMessage[]>(`/api/times/${teamId}/chat/mensagens`)
  return data
}

export async function sendTeamChatMessage(teamId: string, content: string): Promise<TeamChatMessage> {
  const { data } = await api.post<TeamChatMessage>(`/api/times/${teamId}/chat/mensagens`, { content })
  return data
}
