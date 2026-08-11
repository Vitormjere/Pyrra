// Espelha os DTOs de Pyrra.Api/Dtos/Desafios/ChallengeCategoryDtos.cs e ChallengeDtos.cs — CRUD
// administrativo do catálogo (só admin acessa). ChallengeCategory em si já existe em ./challenges,
// reaproveitada aqui.

import type { ChallengeCategoryColor } from './challenges'

// GET /api/admin/desafios — desafio do catálogo, sem a categoria embutida (ao contrário de
// AvailableChallenge em ./challenges, que é a visão do time).
export interface AdminChallenge {
  id: string
  categoryId: string
  title: string
  description: string | null
  points: number
  /** DateTime ISO — nulo = sem prazo, vale enquanto a categoria estiver ativa. */
  deadline: string | null
  createdAt: string
  updatedAt: string
}

export interface ChallengeCategoryPayload {
  name: string
  description: string | null
  icon: string
  color: ChallengeCategoryColor
}

export interface ChallengePayload {
  categoryId: string
  title: string
  description: string | null
  points: number
  deadline: string | null
}
