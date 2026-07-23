import api from './api'
import type { PlanNoteResponse } from '../types/planning'

// date omitida = hoje no fuso do usuário. Dia sem nota devolve 200 com content
// vazio e updatedAt nulo, não 404 — "ainda não escreveu" é estado normal.
export async function getToday(date?: string): Promise<PlanNoteResponse> {
  const { data } = await api.get<PlanNoteResponse>('/api/planejamento', {
    params: date ? { date } : undefined,
  })
  return data
}

// Upsert idempotente por (usuário, data): salvar de novo sobrescreve o dia.
export async function save(
  content: string,
  date?: string,
): Promise<PlanNoteResponse> {
  const { data } = await api.put<PlanNoteResponse>(
    '/api/planejamento',
    { content },
    { params: date ? { date } : undefined },
  )
  return data
}

// Só os dias em que houve texto — o backend filtra as notas em branco.
export async function getHistory(days = 30): Promise<PlanNoteResponse[]> {
  const { data } = await api.get<PlanNoteResponse[]>(
    '/api/planejamento/historico',
    { params: { dias: days } },
  )
  return data
}
