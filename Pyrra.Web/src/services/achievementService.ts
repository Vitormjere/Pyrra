import api from './api'
import type { AchievementResponse, PendingAchievementResponse } from '../types/achievement'

// catálogo completo do usuário — desbloqueadas e bloqueadas, com progresso quando dá pra calcular
export async function getAchievements(): Promise<AchievementResponse[]> {
  const { data } = await api.get<AchievementResponse[]>('/api/conquistas')
  return data
}

// chame sempre depois de um evento que pode desbloquear (getStreakStatus, aprovação de desafio) — mesma lógica de streakService
export async function getPendingAchievements(): Promise<PendingAchievementResponse[]> {
  const { data } = await api.get<PendingAchievementResponse[]>('/api/conquistas/pendentes')
  return data
}

// marca desbloqueios como exibidos, sem `ids` o backend confirma todos os pendentes de uma vez
export async function acknowledgeAchievements(ids?: string[]): Promise<number> {
  const { data } = await api.post<{ acknowledged: number }>(
    '/api/conquistas/pendentes/confirmar',
    { ids: ids ?? null },
  )
  return data.acknowledged
}
