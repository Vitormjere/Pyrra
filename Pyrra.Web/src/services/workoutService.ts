import api from './api'
import type {
  CreateWorkoutPayload,
  WorkoutPlanDayResponse,
  WorkoutPlanExerciseResponse,
  WorkoutResponse,
  WorkoutType,
} from '../types/workout'
import type { WeekDay } from '../types/plan'

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

// Intervalo arbitrário — base da Agenda.
export async function getWorkoutsForRange(
  start: string,
  end: string,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>('/api/treinos/intervalo', {
    params: { inicio: start, fim: end },
  })
  return data
}

// Sempre os 7 dias, mesmo os sem label.
export async function getWorkoutPlan(): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.get<WorkoutPlanDayResponse[]>('/api/treinos/plano')
  return data
}

// Envia o plano inteiro: a tela salva o que está na tela, sem diffs.
export async function saveWorkoutPlan(
  days: WorkoutPlanDayResponse[],
): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.put<WorkoutPlanDayResponse[]>(
    '/api/treinos/plano',
    { days },
  )
  return data
}

// sets/reps são ignorados pelo backend quando type é Corrida.
export async function addPlanExercise(
  day: WeekDay,
  type: WorkoutType,
  exerciseName: string,
  sets?: number | null,
  reps?: number | null,
): Promise<WorkoutPlanExerciseResponse> {
  const { data } = await api.post<WorkoutPlanExerciseResponse>(
    `/api/treinos/plano/${day}/exercicios`,
    { type, exerciseName, sets: sets ?? null, reps: reps ?? null },
  )
  return data
}

export async function removePlanExercise(exerciseId: string): Promise<void> {
  await api.delete(`/api/treinos/plano/exercicios/${exerciseId}`)
}

// Mesma forma de payload da criação; o backend revalida por tipo.
export async function updateWorkout(
  workoutId: string,
  payload: CreateWorkoutPayload,
): Promise<WorkoutResponse> {
  const { data } = await api.put<WorkoutResponse>(
    `/api/treinos/${workoutId}`,
    payload,
  )
  return data
}

export async function deleteWorkout(workoutId: string): Promise<void> {
  await api.delete(`/api/treinos/${workoutId}`)
}
