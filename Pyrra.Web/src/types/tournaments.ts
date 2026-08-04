// espelha os DTOs de Pyrra.Api/Dtos/Comunidade/TournamentDtos.cs

import type { UserSummary } from './community'
import type { TeamBannerTheme } from './teams'

export type TournamentTeamStatus = 'Pendente' | 'Aprovado' | 'Recusado'

export type TournamentRequestStatus = 'Pendente' | 'Aprovado' | 'Recusado'

// GET /api/torneios, GET /api/torneios/meus, POST /api/torneios
export interface Tournament {
  id: string
  name: string
  description: string | null
  owner: UserSummary
  isOwner: boolean
  bannerTheme: TeamBannerTheme
  /** Preenchida = imagem customizada, com prioridade sobre bannerTheme na exibição. */
  bannerImageUrl: string | null
}

// GET /api/torneios/{id}
export interface TournamentDetails {
  tournament: Tournament
  inviteToken: string
  /** Caminho relativo (/torneios/convite/{token}); a URL absoluta é montada com window.location.origin. */
  invitePath: string
}

// POST /api/torneios/solicitacoes — solicitação de criação de torneio, aguardando aprovação de um admin
export interface TournamentRequest {
  id: string
  proposedName: string
  proposedDescription: string | null
  requester: UserSummary
  createdAt: string
  status: TournamentRequestStatus
  reviewedAt: string | null
}

// um time participando (ou solicitando participar) de um torneio, usado na fila de entradas e no ranking
export interface TournamentTeamEntry {
  id: string
  tournamentId: string
  teamId: string
  teamName: string
  teamBannerTheme: TeamBannerTheme
  teamBannerImageUrl: string | null
  status: TournamentTeamStatus
  score: number
  requestedAt: string
}
