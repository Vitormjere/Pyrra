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
