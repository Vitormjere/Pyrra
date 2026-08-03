// Espelha os DTOs de Pyrra.Api/Dtos/Desafios/TournamentChallengeDtos.cs — desafios de um torneio
// específico (Fase 5b), separados dos desafios normais do time em types/challenges.ts.

import type { UserSummary } from './community'
import type { ChallengeCategory, ChallengeSubmissionStatus } from './challenges'

export type ChallengeSource = 'Time' | 'TorneioCatalogo' | 'TorneioProprio'

// GET /api/torneios/{id}/desafios/catalogo — desafio do catálogo geral com o status de vínculo
// ao torneio (dono do torneio escolhe daqui). Goal/Unit vêm do VÍNCULO, não do desafio original
// — nulos quando não vinculado ou vinculado sem meta (Fase 5c).
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

// GET/POST /api/torneios/{id}/desafios/proprios — desafio criado livremente pelo dono do
// torneio, sem categoria e sem vínculo com o catálogo geral. Goal/Unit opcionais (Fase 5c).
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

// GET /api/times/{teamId}/desafios/torneios/{tournamentId} — desafio disponível de UM torneio
// específico em que o time está Aprovado (catálogo vinculado + próprio, já achatados). Goal/Unit
// nulos = sem meta, continua binário; Progress é a soma das contribuições Aprovadas DESSE TIME
// (Fase 5c) — 0 ou mais quando há meta, null quando não há.
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

// GET /api/times/{teamId}/desafios/torneios/{tournamentId}/submissoes — fila de UM time dentro
// do torneio, visível ao dono do torneio. Quantity nula = desafio sem meta (Fase 5c).
export interface PendingTournamentSubmission {
  id: string
  createdAt: string
  challengeTitle: string
  challengePoints: number
  source: ChallengeSource
  quantity: number | null
  submitter: UserSummary
}

// GET /api/torneios/{id}/desafios/submissoes — a mesma fila, cruzando TODOS os times
// participantes do torneio (pode ser mais de um) — inclui de qual time veio.
export interface PendingTournamentSubmissionWithTeam extends PendingTournamentSubmission {
  teamId: string
  teamName: string
}

// Progresso de UM time num desafio com meta — linha da visão agregada do dono (Fase 5c).
export interface TeamChallengeProgress {
  teamId: string
  teamName: string
  progress: number
}

// GET /api/torneios/{id}/desafios/progresso — progresso agregado de cada desafio COM META,
// cruzando todos os times Aprovados no torneio. Só o dono vê; desafios sem meta não aparecem.
export interface TournamentChallengeProgress {
  challengeId: string
  challengeTitle: string
  source: ChallengeSource
  goal: number
  unit: string
  teams: TeamChallengeProgress[]
}
