import api from './api'
import type {
  PendingFreezeUseResponse,
  PendingMilestoneResponse,
  StreakStatusResponse,
} from '../types/streak'

// esse GET não é só leitura, ele roda o acerto do streak antes de responder, por isso vale rechamar após um check-in
export async function getStreakStatus(): Promise<StreakStatusResponse> {
  const { data } = await api.get<StreakStatusResponse>('/api/streak')
  return data
}

// chame sempre depois de getStreakStatus(), é o acerto dele que cria os marcos — em paralelo pode perder um recém-criado
export async function getPendingMilestones(): Promise<PendingMilestoneResponse[]> {
  const { data } = await api.get<PendingMilestoneResponse[]>(
    '/api/streak/marcos-pendentes',
  )
  return data
}

// marca marcos como exibidos, sem `ids` o backend confirma todos os pendentes de uma vez
export async function acknowledgeMilestones(ids?: string[]): Promise<number> {
  const { data } = await api.post<{ acknowledged: number }>(
    '/api/streak/marcos-pendentes/confirmar',
    { ids: ids ?? null },
  )
  return data.acknowledged
}

// mesmo padrão dos marcos, agora pros avisos de freeze usado — chame sempre depois de getStreakStatus()
export async function getPendingFreezeUses(): Promise<PendingFreezeUseResponse[]> {
  const { data } = await api.get<PendingFreezeUseResponse[]>(
    '/api/streak/freezes-usados-pendentes',
  )
  return data
}

// marca avisos de freeze como exibidos, sem `ids` o backend confirma todos os pendentes de uma vez
export async function acknowledgeFreezeUses(ids?: string[]): Promise<number> {
  const { data } = await api.post<{ acknowledged: number }>(
    '/api/streak/freezes-usados-pendentes/confirmar',
    { ids: ids ?? null },
  )
  return data.acknowledged
}
