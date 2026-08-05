import type { UserPlan } from './auth'

// GET /api/usuario/{username}/perfil — perfil público de terceiro. Deliberadamente SEM email,
// tom de comunicação, horário de notificação ou fuso: são pessoais, não sociais.
export interface PublicProfile {
  id: string
  name: string
  username: string | null
  plan: UserPlan
  friendCount: number
  streakCurrent: number
  streakBest: number
}
