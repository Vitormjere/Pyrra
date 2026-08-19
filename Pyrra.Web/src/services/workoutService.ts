import api from './api'
import type {
  CreateWorkoutPayload,
  WorkoutPlanDayResponse,
  WorkoutPlanExerciseResponse,
  WorkoutResponse,
  WorkoutTemplate,
  WorkoutType,
} from '../types/workout'
import type { WeekDay } from '../types/plan'

// o payload carrega os campos das duas modalidades, o backend valida por Type e calcula o pace se faltar
export async function createWorkout(
  payload: CreateWorkoutPayload,
): Promise<WorkoutResponse> {
  const { data } = await api.post<WorkoutResponse>('/api/treinos', payload)
  return data
}

// vem do backend já ordenado do mais recente pro mais antigo, o primeiro item é o treino mais novo
export async function getWorkouts(
  type?: WorkoutType,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>('/api/treinos', {
    params: type ? { tipo: type } : undefined,
  })
  return data
}

// histórico de um exercício de academia em ordem crescente de data, pra ler a evolução de carga
export async function getWorkoutHistory(
  exerciseName: string,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>(
    `/api/treinos/exercicio/${encodeURIComponent(exerciseName)}`,
  )
  return data
}

// intervalo arbitrário, base da Agenda
export async function getWorkoutsForRange(
  start: string,
  end: string,
): Promise<WorkoutResponse[]> {
  const { data } = await api.get<WorkoutResponse[]>('/api/treinos/intervalo', {
    params: { inicio: start, fim: end },
  })
  return data
}

// sempre os 7 dias, mesmo os sem label
export async function getWorkoutPlan(): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.get<WorkoutPlanDayResponse[]>('/api/treinos/plano')
  return data
}

// envia o plano inteiro, a tela salva o que está na tela, sem diffs
export async function saveWorkoutPlan(
  days: WorkoutPlanDayResponse[],
): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.put<WorkoutPlanDayResponse[]>(
    '/api/treinos/plano',
    { days },
  )
  return data
}

// troca dois dias de lugar — o backend só atualiza o WorkoutPlanDay.DayOfWeek de cada um, os
// exercícios (ligados por FK) acompanham sozinhos, sem precisar reenviar nada deles
export async function swapWorkoutPlanDays(
  diaOrigem: WeekDay,
  diaDestino: WeekDay,
): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.post<WorkoutPlanDayResponse[]>(
    '/api/treinos/plano/trocar-dias',
    { diaOrigem, diaDestino },
  )
  return data
}

// sets/reps são ignorados pelo backend quando type é Corrida
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

// catálogo de templates prontos, com dias e exercícios pro preview
export async function getWorkoutTemplates(): Promise<WorkoutTemplate[]> {
  const { data } = await api.get<WorkoutTemplate[]>('/api/treinos/templates')
  return data
}

// aplica o template sobrescrevendo o plano da semana, mesmo formato do getWorkoutPlan pra tela recarregar sem um segundo caminho
export async function applyWorkoutTemplate(
  templateId: string,
): Promise<WorkoutPlanDayResponse[]> {
  const { data } = await api.post<WorkoutPlanDayResponse[]>(
    `/api/treinos/templates/${templateId}/aplicar`,
  )
  return data
}

// mesma forma de payload da criação, o backend revalida por tipo
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
