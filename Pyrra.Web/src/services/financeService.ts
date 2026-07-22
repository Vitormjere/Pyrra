import api from './api'
import type { BalanceResponse, FinanceCategoryResponse } from '../types/finance'

// Saldo acumulado até hoje. Lançamentos com data futura não entram — o corte é
// feito no backend, não aqui.
export async function getBalance(): Promise<BalanceResponse> {
  const { data } = await api.get<BalanceResponse>('/api/financas/saldo')
  return data
}

// Padrão do sistema + as próprias do usuário. Nunca as de outro usuário.
export async function getCategories(): Promise<FinanceCategoryResponse[]> {
  const { data } = await api.get<FinanceCategoryResponse[]>(
    '/api/financas/categorias',
  )
  return data
}
