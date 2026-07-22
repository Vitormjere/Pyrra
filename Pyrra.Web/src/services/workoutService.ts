import api from './api'
import type { WorkoutResponse, WorkoutType } from '../types/workout'

// Vem do backend já ordenado do mais recente para o mais antigo (data desc,
// depois criação desc), então o primeiro item é o treino mais novo.
export async function getWorkouts(
  type?: WorkoutType,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>('/api/treinos', {
    params: type ? { tipo: type } : undefined,
  })
  return data
}

// Histórico de um exercício específico de Academia, em ordem CRESCENTE de data —
// é como se lê evolução de carga.
export async function getWorkoutHistory(
  exerciseName: string,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>(
    `/api/treinos/exercicio/${encodeURIComponent(exerciseName)}`,
  )
  return data
}
