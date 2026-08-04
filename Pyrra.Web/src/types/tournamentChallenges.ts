// espelha os DTOs de desafios de um torneio específico, separados dos desafios normais do time em types/challenges.ts

import type { UserSummary } from './community'
import type { ChallengeCategory, ChallengeSubmissionStatus } from './challenges'

export type ChallengeSource = 'Time' | 'TorneioCatalogo' | 'TorneioProprio'

// GET /api/torneios/{id}/desafios/catalogo — desafio do catálogo com status de vínculo ao torneio; goal/unit vêm do vínculo, não do desafio original
export interface TournamentCatalogChallenge {
  id: string
  title: string
  description: string | null
  points: number
  deadline: string | null
  category: ChallengeCategory
  isLinked: boolean
  goal: number | null
  unit: string | null
}

// GET/POST /api/torneios/{id}/desafios/proprios — desafio criado livremente pelo dono do torneio, sem categoria nem vínculo com o catálogo
export interface TournamentOwnChallenge {
  id: string
  tournamentId: string
  title: string
  description: string | null
  points: number
  goal: number | null
  unit: string | null
  createdAt: string
  updatedAt: string
}

// GET /api/times/{teamId}/desafios/torneios/{tournamentId} — desafios do torneio em que o time está aprovado; progress é a soma das contribuições aprovadas desse time
export interface AvailableTournamentChallenge {
  id: string
  title: string
  description: string | null
  points: number
  source: ChallengeSource
  goal: number | null
  unit: string | null
  progress: number | null
  mySubmissionStatus: ChallengeSubmissionStatus | null
}

// GET /api/times/{teamId}/desafios/torneios/{tournamentId}/submissoes — fila de submissões do time, visível ao dono do torneio
export interface PendingTournamentSubmission {
  id: string
  createdAt: string
  challengeTitle: string
  challengePoints: number
  source: ChallengeSource
  quantity: number | null
  submitter: UserSummary
}

// GET /api/torneios/{id}/desafios/submissoes — a mesma fila, cruzando todos os times participantes do torneio
export interface PendingTournamentSubmissionWithTeam extends PendingTournamentSubmission {
  teamId: string
  teamName: string
}

// progresso de um time num desafio com meta, linha da visão agregada do dono
export interface TeamChallengeProgress {
  teamId: string
  teamName: string
  progress: number
}

// GET /api/torneios/{id}/desafios/progresso — progresso agregado de cada desafio com meta, só o dono vê
export interface TournamentChallengeProgress {
  challengeId: string
  challengeTitle: string
  source: ChallengeSource
  goal: number
  unit: string
  teams: TeamChallengeProgress[]
}
