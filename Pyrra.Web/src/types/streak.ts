// Espelha os DTOs de Pyrra.Api/Dtos/Streaks.

export interface MilestoneResponse {
  /** O marco atingido, em dias (ex.: 7, 30). */
  milestone: number
  /** Fração 0..1 — média de aproveitamento do trecho até o marco. */
  averagePercentage: number
  /** DateOnly serializado como "YYYY-MM-DD". */
  reachedDate: string
}

// GET /api/streak/marcos-pendentes — marcos já atingidos que ainda não foram
// exibidos ao usuário. Carregam Id porque a confirmação pode ser seletiva.
export interface PendingMilestoneResponse {
  id: string
  /** O marco atingido, em dias (ex.: 7, 30). */
  milestone: number
  /** Fração 0..1 — média de aproveitamento do trecho até o marco. */
  averagePercentage: number
  /** DateOnly serializado como "YYYY-MM-DD". */
  reachedDate: string
}

// GET /api/streak/freezes-usados-pendentes — dias perdoados por freeze que ainda
// não foram avisados ao usuário. Carregam Id porque a confirmação pode ser seletiva.
export interface PendingFreezeUseResponse {
  id: string
  /** O dia perdoado, DateOnly serializado como "YYYY-MM-DD". */
  date: string
}

// GET /api/streak
export interface StreakStatusResponse {
  /** Streak CONSOLIDADO: só dias passados já acertados. Não inclui hoje. */
  currentCount: number
  bestCount: number
  freezesAvailable: number
  /** Meta de hoje batida, calculada ao vivo. */
  todayGoalMet: boolean
  /** O número que a UI mostra: currentCount + (todayGoalMet ? 1 : 0). */
  displayCount: number
  milestonesReached: MilestoneResponse[]
}
