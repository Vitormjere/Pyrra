// Espelha os DTOs de Pyrra.Api/Dtos/Financas.

export type FinanceEntryType = 'Entrada' | 'Saida'

// GET /api/financas/saldo
// Os totais são "até hoje": lançamentos com data futura (conta agendada) ficam
// de fora do saldo atual, por decisão do FinanceService.
export interface BalanceResponse {
  totalInToDate: number
  totalOutToDate: number
  currentBalance: number
}

export interface FinanceCategoryResponse {
  id: string
  name: string
  /** true = categoria do sistema, compartilhada; false = criada pelo usuário. */
  isDefault: boolean
}

export interface FinanceEntryResponse {
  id: string
  categoryId: string
  amount: number
  type: FinanceEntryType
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  description: string | null
  createdAt: string
}

// GET /api/financas/semana
// Os totais aqui são DO PERÍODO, não acumulados — diferente do BalanceResponse.
// E, ao contrário do saldo, a semana inclui lançamentos com data futura.
export interface WeeklyFinanceSummaryResponse {
  weekStart: string
  weekEnd: string
  periodTotalIn: number
  periodTotalOut: number
  periodBalance: number
  entries: FinanceEntryResponse[]
}

// GET /api/financas/historico — um ponto por dia, terminando hoje. Dias sem
// lançamento repetem o saldo anterior, então a série nunca tem buracos.
export interface DailyBalanceResponse {
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  balance: number
}

// POST /api/financas/lancamentos. amount é sempre POSITIVO: o sinal vem do type.
export interface CreateFinanceEntryPayload {
  categoryId: string
  amount: number
  type: FinanceEntryType
  date?: string | null
  description?: string | null
}
