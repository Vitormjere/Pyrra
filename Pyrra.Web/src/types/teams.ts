// espelha os DTOs de Pyrra.Api/Dtos/Comunidade/TeamDtos.cs

import type { UserSummary } from './community'

export type TeamVisibility = 'Privado' | 'Publico'

export type TeamBannerTheme = 'Verde' | 'Azul' | 'Roxo' | 'Laranja' | 'Vermelho' | 'Dourado'

// GET /api/times, GET /api/times/{id} (Team), POST /api/times
export interface Team {
  id: string
  name: string
  description: string | null
  owner: UserSummary
  memberCount: number
  memberLimit: number
  totalPoints: number
  isOwner: boolean
  isMember: boolean
  visibility: TeamVisibility
  bannerTheme: TeamBannerTheme
  /** Preenchida = imagem customizada, com prioridade sobre bannerTheme na exibição. */
  bannerImageUrl: string | null
}

export interface TeamMember {
  userId: string
  user: UserSummary
  isOwner: boolean
  /** DateTime ISO, nulo para o dono (que não tem linha própria de membership). */
  joinedAt: string | null
}

// torneio em que o time tem uma entrada ativa (pendente ou aprovado)
export interface ActiveTournament {
  tournamentId: string
  tournamentName: string
  status: 'Pendente' | 'Aprovado'
}

// GET /api/times/{id}
export interface TeamDetails {
  team: Team
  members: TeamMember[]
  inviteToken: string
  /** Caminho relativo (/times/convite/{token}); a URL absoluta é montada com window.location.origin. */
  invitePath: string
  /** Vazio = o time não está em nenhum torneio no momento. Até 5 simultâneos (Fase 5b). */
  activeTournaments: ActiveTournament[]
}

// GET /api/times/convites
export interface TeamInvite {
  inviteId: string
  team: Team
  inviter: UserSummary
  createdAt: string
}

// desfecho de entrar via link de convite de time
export type JoinOutcome = 'Joined' | 'AlreadyMember' | 'TeamFull' | 'OwnLink'

// POST /api/times/convite/{token}/entrar
export interface JoinResult {
  team: Team
  outcome: JoinOutcome
}

// GET /api/times/publicos — time público na aba Explorar, já vem com o token de convite
export interface PublicTeam {
  team: Team
  inviteToken: string
}
