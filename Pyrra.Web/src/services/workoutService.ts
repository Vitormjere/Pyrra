import api from './api'
import type {
  CreateWorkoutPayload,
  WorkoutResponse,
  WorkoutType,
} from '../types/workout'

// O backend valida quais campos combinam com o Type e calcula o pace quando ele
// não vem informado — por isso o payload carrega os campos das duas modalidades.
export async function createWorkout(
  payload: CreateWorkoutPayload,
): Promise<WorkoutResponse> {
  const { data } = await api.post<WorkoutResponse>('/api/treinos', payload)
  return data
}

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
