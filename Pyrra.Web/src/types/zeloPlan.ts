import type { WeekDay } from './plan'
import type { WorkoutType } from './workout'
import type { MealType } from './nutrition'

// Espelha os DTOs de Pyrra.Api/Dtos/Zelo (fluxo do Zelo conversacional).

export type ZeloPlanSessionStatus = 'Coletando' | 'PlanoGerado' | 'Aplicada' | 'Descartada'

export type ZeloPlanMessageRole = 'Usuario' | 'Zelo'

export interface ZeloPlanQuestionResponse {
  key: string
  text: string
  /** null = texto livre; presente = escolha única entre as opções. */
  options: string[] | null
}

// POST /api/zelo/plano/iniciar, /responder, /tentar-novamente
export interface ZeloPlanSessionResponse {
  sessionId: string
  status: ZeloPlanSessionStatus
  /** null quando o formulário está completo (veja status/error para saber o que fazer a seguir). */
  nextQuestion: ZeloPlanQuestionResponse | null
  answeredCount: number
  /** preenchido quando a geração falhou — status continua Coletando, front oferece "tentar de novo". */
  error: string | null
}

export interface GeneratedWorkoutExerciseResponse {
  type: WorkoutType
  exerciseName: string
  sets: number | null
  reps: number | null
  order: number
}

export interface GeneratedWorkoutDayResponse {
  dayOfWeek: WeekDay
  label: string | null
  exercises: GeneratedWorkoutExerciseResponse[]
}

export interface GeneratedNutritionItemResponse {
  mealType: MealType
  itemName: string
  quantity: string
}

export interface GeneratedNutritionDayResponse {
  dayOfWeek: WeekDay
  items: GeneratedNutritionItemResponse[]
}

export interface GeneratedPlanResponse {
  summary: string
  workoutDays: GeneratedWorkoutDayResponse[]
  nutritionDays: GeneratedNutritionDayResponse[]
}

// GET /api/zelo/plano/{sessionId}/preview
export interface ZeloPlanPreviewResponse {
  sessionId: string
  status: ZeloPlanSessionStatus
  plan: GeneratedPlanResponse
}

// GET/POST /api/zelo/plano/{sessionId}/mensagens
export interface ZeloPlanChatMessageResponse {
  id: string
  role: ZeloPlanMessageRole
  content: string
  /** ISO 8601. */
  createdAt: string
}

// POST .../mensagens — reply nulo + error preenchido = o Zelo não conseguiu responder (a mensagem do usuário já foi salva, aparece no próximo GET)
export interface ZeloPlanChatResponse {
  reply: ZeloPlanChatMessageResponse | null
  error: string | null
}
