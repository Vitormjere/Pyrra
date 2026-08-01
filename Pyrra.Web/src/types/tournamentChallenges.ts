// Espelha os DTOs de Pyrra.Api/Dtos/Desafios/TournamentChallengeDtos.cs — desafios de um torneio
// específico (Fase 5b), separados dos desafios normais do time em types/challenges.ts.

import type { UserSummary } from './community'
import type { ChallengeCategory, ChallengeSubmissionStatus } from './challenges'

export type ChallengeSource = 'Time' | 'TorneioCatalogo' | 'TorneioProprio'

// GET /api/torneios/{id}/desafios/catalogo — desafio do catálogo geral com o status de vínculo
// ao torneio (dono do torneio escolhe daqui).
export interface TournamentCatalogChallenge {
  id: string
  title: string
  description: string | null
  points: number
  deadline: string | null
  category: ChallengeCategory
  isLinked: boolean
}

// GET/POST /api/torneios/{id}/desafios/proprios — desafio criado livremente pelo dono do
// torneio, sem categoria e sem vínculo com o catálogo geral.
export interface TournamentOwnChallenge {
  id: string
  tournamentId: string
  title: string
  description: string | null
  points: number
  createdAt: string
  updatedAt: string
}

// GET /api/times/{teamId}/desafios/torneios/{tournamentId} — desafio disponível de UM torneio
// específico em que o time está Aprovado (catálogo vinculado + próprio, já achatados).
export interface AvailableTournamentChallenge {
  id: string
  title: string
  description: string | null
  points: number
  source: ChallengeSource
  mySubmissionStatus: ChallengeSubmissionStatus | null
}

// GET /api/times/{teamId}/desafios/torneios/{tournamentId}/submissoes — fila de UM time dentro
// do torneio, visível ao dono do torneio.
export interface PendingTournamentSubmission {
  id: string
  createdAt: string
  challengeTitle: string
  challengePoints: number
  source: ChallengeSource
  submitter: UserSummary
}

// GET /api/torneios/{id}/desafios/submissoes — a mesma fila, cruzando TODOS os times
// participantes do torneio (pode ser mais de um) — inclui de qual time veio.
export interface PendingTournamentSubmissionWithTeam extends PendingTournamentSubmission {
  teamId: string
  teamName: string
}
