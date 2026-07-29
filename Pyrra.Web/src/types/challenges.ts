// Espelha os DTOs de Pyrra.Api/Dtos/Desafios/*.cs

import type { UserSummary } from './community'

export type ChallengeCategoryColor = 'Verde' | 'Azul' | 'Roxo' | 'Laranja' | 'Vermelho' | 'Dourado'

export type ChallengeSubmissionStatus = 'Pendente' | 'Aprovado' | 'Recusado'

export interface ChallengeCategory {
  id: string
  name: string
  description: string | null
  icon: string
  color: ChallengeCategoryColor
  createdAt: string
  updatedAt: string
}

// GET /api/times/{id}/desafios/categorias — categoria do catálogo com o flag de ativação do time.
export interface TeamCategoryStatus {
  id: string
  name: string
  description: string | null
  icon: string
  color: ChallengeCategoryColor
  isActive: boolean
}

// GET /api/times/{id}/desafios — desafio disponível, já com a categoria embutida.
// mySubmissionStatus nulo = quem pediu a lista nunca enviou prova pra esse desafio nesse time.
export interface AvailableChallenge {
  id: string
  title: string
  description: string | null
  points: number
  /** DateTime ISO — nulo = sempre disponível enquanto a categoria estiver ativa. */
  deadline: string | null
  category: ChallengeCategory
  mySubmissionStatus: ChallengeSubmissionStatus | null
}

// GET /api/times/{id}/desafios/submissoes — submissão na fila de aprovação do dono. Sem
// photoUrl: o container é privado, a foto é buscada via getSubmissionPhotoUrl (autenticado).
export interface PendingSubmission {
  id: string
  createdAt: string
  challenge: {
    id: string
    title: string
    description: string | null
    points: number
    deadline: string | null
  }
  submitter: UserSummary
}
