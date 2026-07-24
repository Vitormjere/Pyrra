import api from './api'
import type {
  BalanceResponse,
  CreateFinanceEntryPayload,
  DailyBalanceResponse,
  FinanceCategoryResponse,
  FinanceEntryResponse,
  WeeklyFinanceSummaryResponse,
} from '../types/finance'

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

// Responde 409 se o nome colidir com qualquer categoria visível — inclusive as
// padrão do sistema.
export async function createCategory(
  name: string,
): Promise<FinanceCategoryResponse> {
  const { data } = await api.post<FinanceCategoryResponse>(
    '/api/financas/categorias',
    { name },
  )
  return data
}

export async function createEntry(
  payload: CreateFinanceEntryPayload,
): Promise<FinanceEntryResponse> {
  const { data } = await api.post<FinanceEntryResponse>(
    '/api/financas/lancamentos',
    payload,
  )
  return data
}

// Série do saldo acumulado para o gráfico. O último ponto é hoje e bate com o
// currentBalance do getBalance() — os dois cortam lançamentos futuros.
export async function getBalanceHistory(
  days = 30,
): Promise<DailyBalanceResponse[]> {
  const { data } = await api.get<DailyBalanceResponse[]>(
    '/api/financas/historico',
    { params: { dias: days } },
  )
  return data
}

// weekStart omitido = semana atual. Uma data no meio da semana é normalizada
// para a segunda-feira dela pelo backend, que devolve o intervalo efetivo.
export async function getWeeklySummary(
  weekStart?: string,
): Promise<WeeklyFinanceSummaryResponse> {
  const { data } = await api.get<WeeklyFinanceSummaryResponse>(
    '/api/financas/semana',
    { params: weekStart ? { inicio: weekStart } : undefined },
  )
  return data
}

// Intervalo arbitrário, diferente do /semana fixo — base da Agenda.
export async function getEntriesForRange(
  start: string,
  end: string,
): Promise<FinanceEntryResponse[]> {
  const { data } = await api.get<FinanceEntryResponse[]>(
    '/api/financas/lancamentos',
    { params: { inicio: start, fim: end } },
  )
  return data
}

// Mesma forma de payload da criação.
export async function updateEntry(
  entryId: string,
  payload: CreateFinanceEntryPayload,
): Promise<FinanceEntryResponse> {
  const { data } = await api.put<FinanceEntryResponse>(
    `/api/financas/lancamentos/${entryId}`,
    payload,
  )
  return data
}

export async function deleteEntry(entryId: string): Promise<void> {
  await api.delete(`/api/financas/lancamentos/${entryId}`)
}

// Só categorias próprias; 409 se houver lançamentos vinculados.
export async function deleteCategory(categoryId: string): Promise<void> {
  await api.delete(`/api/financas/categorias/${categoryId}`)
}
