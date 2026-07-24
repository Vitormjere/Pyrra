import api from './api'
import type {
  PendingFreezeUseResponse,
  PendingMilestoneResponse,
  StreakStatusResponse,
} from '../types/streak'

// Atenção: este GET não é somente leitura no backend — ele roda o acerto do
// streak (SettleStreakAsync) antes de responder. É o que mantém o foguinho em dia
// sem depender de job agendado, e por isso vale rechamá-lo depois de um check-in.
export async function getStreakStatus(): Promise<StreakStatusResponse> {
  const { data } = await api.get<StreakStatusResponse>('/api/streak')
  return data
}

// Consulte SEMPRE depois de getStreakStatus(): é o acerto disparado por aquele
// GET que cria os marcos, então buscar os dois em paralelo pode perder um marco
// recém-nascido.
export async function getPendingMilestones(): Promise<PendingMilestoneResponse[]> {
  const { data } = await api.get<PendingMilestoneResponse[]>(
    '/api/streak/marcos-pendentes',
  )
  return data
}

/**
 * Marca marcos como exibidos. Sem `ids`, o backend confirma TODOS os pendentes —
 * passar a lista explícita é o que permite confirmar um de cada vez, sem
 * descartar celebrações que o usuário ainda não viu.
 *
 * @returns quantos marcos foram confirmados.
 */
export async function acknowledgeMilestones(ids?: string[]): Promise<number> {
  const { data } = await api.post<{ acknowledged: number }>(
    '/api/streak/marcos-pendentes/confirmar',
    { ids: ids ?? null },
  )
  return data.acknowledged
}

// Mesmo par do marcos, para os avisos de freeze usado. Consulte SEMPRE depois de
// getStreakStatus(): é o acerto disparado por aquele GET que registra o freeze
// gasto, então buscar em paralelo poderia perder um aviso recém-nascido.
export async function getPendingFreezeUses(): Promise<PendingFreezeUseResponse[]> {
  const { data } = await api.get<PendingFreezeUseResponse[]>(
    '/api/streak/freezes-usados-pendentes',
  )
  return data
}

/**
 * Marca avisos de freeze como exibidos. Sem `ids`, o backend confirma TODOS os
 * pendentes — passar a lista explícita permite confirmar um de cada vez.
 *
 * @returns quantos avisos foram confirmados.
 */
export async function acknowledgeFreezeUses(ids?: string[]): Promise<number> {
  const { data } = await api.post<{ acknowledged: number }>(
    '/api/streak/freezes-usados-pendentes/confirmar',
    { ids: ids ?? null },
  )
  return data.acknowledged
}
