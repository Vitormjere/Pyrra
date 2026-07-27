import type { WeekDay } from './plan'

// Espelha os DTOs de Pyrra.Api/Dtos/Treinos.

export type WorkoutType = 'Academia' | 'Corrida'

// GET /api/treinos
// Os campos das duas modalidades convivem no mesmo registro e só um conjunto vem
// preenchido: Academia usa exerciseName/loadKg/sets/reps, Corrida usa
// distanceKm/durationMinutes/paceMinPerKm. Quem garante a coerência é o backend.
export interface WorkoutResponse {
  id: string
  type: WorkoutType
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string

  exerciseName: string | null
  loadKg: number | null
  sets: number | null
  reps: number | null

  distanceKm: number | null
  durationMinutes: number | null
  paceMinPerKm: number | null

  notes: string | null
  createdAt: string
}

// POST /api/treinos. date nula = hoje no fuso do usuário; paceMinPerKm omitido
// faz o backend derivá-lo de duração ÷ distância.
export interface CreateWorkoutPayload {
  type: WorkoutType
  date?: string | null
  exerciseName?: string | null
  loadKg?: number | null
  sets?: number | null
  reps?: number | null
  distanceKm?: number | null
  durationMinutes?: number | null
  notes?: string | null
}

// Exercício planejado. O campo Order não é exposto: a lista já chega na ordem
// certa, e a posição é detalhe interno do backend.
export interface WorkoutPlanExerciseResponse {
  id: string
  type: WorkoutType
  /** Em Academia é o exercício; em Corrida, a descrição curta do treino. */
  exerciseName: string
  /** Só preenchidos em Academia. */
  sets: number | null
  reps: number | null
}

// GET/PUT /api/treinos/plano — sempre os 7 dias, Segunda→Domingo.
// label nulo/vazio = "sem plano definido", NÃO descanso.
export interface WorkoutPlanDayResponse {
  dayOfWeek: WeekDay
  label: string | null
  exercises: WorkoutPlanExerciseResponse[]
}

// Exercício de um template (só leitura, para o preview). Sem id nem type: o catálogo
// é todo de Academia e o card não edita, só mostra.
export interface WorkoutTemplateExercisePreview {
  exerciseName: string
  sets: number | null
  reps: number | null
}

// Um dia do template. label "Descanso" é explícito (diferente do plano do usuário).
export interface WorkoutTemplateDayPreview {
  dayOfWeek: WeekDay
  label: string
  exercises: WorkoutTemplateExercisePreview[]
}

// GET /api/treinos/templates — catálogo fixo de planos prontos.
// isCustom = "Personalizado": não aplica estrutura, só leva ao editor manual.
export interface WorkoutTemplate {
  id: string
  name: string
  description: string
  trainingDaysPerWeek: number
  restDaysPerWeek: number
  isCustom: boolean
  days: WorkoutTemplateDayPreview[]
}
