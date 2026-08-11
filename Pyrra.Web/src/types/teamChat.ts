// espelha os DTOs de Pyrra.Api/Dtos/Chat/TeamChatDtos.cs — chat em grupo do time

import type { UserSummary } from './community'

export interface TeamChatMessage {
  id: string
  sender: UserSummary
  teamId: string
  content: string
  createdAt: string
}
