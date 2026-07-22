// Espelha os DTOs de Pyrra.Api/Dtos/Focos.

export type FocusCategory =
  | 'Hidratacao'
  | 'Medicacao'
  | 'Exercicio'
  | 'Sono'
  | 'Estudo'
  | 'Leitura'
  | 'Mental'
  | 'Outro'

// GET /api/focos e POST /api/focos
export interface FocusResponse {
  id: string
  name: string
  category: FocusCategory
  weight: number
  active: boolean
  createdAt: string
}

// Situação de um foco dentro de um dia específico.
export interface FocusStatusResponse {
  focusId: string
  name: string
  weight: number
  completed: boolean
}

// GET /api/focos/score e resposta do check-in.
export interface DailyScoreResponse {
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  pointsEarned: number
  pointsPossible: number
  /** FRAÇÃO entre 0 e 1 (0.7143), não percentual — multiplique por 100 para exibir. */
  percentage: number
  /** Meta do dia batida: o backend usa o piso de 70%. */
  goalMet: boolean
  focuses: FocusStatusResponse[]
}
