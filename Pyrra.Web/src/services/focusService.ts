import api from './api'
import type { DailyScoreResponse, FocusResponse } from '../types/focus'

// date omitida = hoje no fuso do usuário, resolvido no backend. O front não
// calcula "hoje": o servidor conhece o timezone do usuário, o navegador conhece
// o do dispositivo, e os dois podem divergir (viagem, VPN).
export async function getDailyScore(date?: string): Promise<DailyScoreResponse> {
  const { data } = await api.get<DailyScoreResponse>('/api/focos/score', {
    params: date ? { date } : undefined,
  })
  return data
}

// Alterna o check-in e devolve o score do dia JÁ RECALCULADO — por isso a tela
// não precisa refazer a consulta do score depois de marcar um foco.
export async function toggleCheckIn(
  focusId: string,
  date?: string,
): Promise<DailyScoreResponse> {
  const { data } = await api.post<DailyScoreResponse>(
    `/api/focos/${focusId}/checkin`,
    null,
    { params: date ? { date } : undefined },
  )
  return data
}

export async function getFocuses(): Promise<FocusResponse[]> {
  const { data } = await api.get<FocusResponse[]>('/api/focos')
  return data
}

// A resposta já traz category e weight: quem classifica o foco pelo nome é o
// FocusCategoryMapper no backend, o front só envia o texto.
export async function createFocus(name: string): Promise<FocusResponse> {
  const { data } = await api.post<FocusResponse>('/api/focos', { name })
  return data
}
