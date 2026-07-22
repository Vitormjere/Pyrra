import api from './api'
import type { TaskResponse, WeeklyTasksResponse } from '../types/task'

// date omitida = hoje no fuso do usuário, resolvido no backend. Traz as tarefas
// do dia, concluídas e pendentes.
export async function getTasksForDay(date?: string): Promise<TaskResponse[]> {
  const { data } = await api.get<TaskResponse[]>('/api/tarefas', {
    params: date ? { date } : undefined,
  })
  return data
}

// Pendentes ATRASADAS da semana (dias já passados), não os 7 dias inteiros.
export async function getPendingTasksForWeek(
  weekStart?: string,
): Promise<WeeklyTasksResponse> {
  const { data } = await api.get<WeeklyTasksResponse>('/api/tarefas/semana', {
    params: weekStart ? { inicio: weekStart } : undefined,
  })
  return data
}

export async function toggleTaskCompleted(
  taskId: string,
): Promise<TaskResponse> {
  const { data } = await api.patch<TaskResponse>(
    `/api/tarefas/${taskId}/concluir`,
  )
  return data
}
