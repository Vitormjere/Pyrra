import type { WeekDay } from './plan'
import type { WorkoutType } from './workout'
import type { MealType } from './nutrition'

// Espelha os DTOs de Pyrra.Api/Dtos/Zelo (fluxo do Zelo conversacional).

export type ZeloPlanSessionStatus = 'Coletando' | 'PlanoGerado' | 'Aplicada' | 'Descartada'

export type ZeloPlanMessageRole = 'Usuario' | 'Zelo'

export type ZeloEditStatus = 'Nenhuma' | 'Proposta' | 'Aplicada' | 'Descartada'

export type ZeloEditTarget = 'Treino' | 'Nutricao'

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

// edição pontual proposta pelo Zelo — sempre um dia inteiro de Treino OU uma refeição de um dia de
// Nutrição, nunca os dois ao mesmo tempo (o par correspondente vem null)
export interface ZeloEditProposalResponse {
  description: string
  target: ZeloEditTarget
  dayOfWeek: WeekDay
  label: string | null
  exercises: GeneratedWorkoutExerciseResponse[] | null
  mealType: MealType | null
  items: GeneratedNutritionItemResponse[] | null
}

// GET/POST /api/zelo/plano/{sessionId}/mensagens
export interface ZeloPlanChatMessageResponse {
  id: string
  role: ZeloPlanMessageRole
  content: string
  /** ISO 8601. */
  createdAt: string
  editStatus: ZeloEditStatus
  /** presente só quando editStatus é 'Proposta' — Aplicada/Descartada já foram resolvidas */
  editProposal: ZeloEditProposalResponse | null
}

// POST .../mensagens — reply nulo + error preenchido = o Zelo não conseguiu responder (a mensagem do usuário já foi salva, aparece no próximo GET)
export interface ZeloPlanChatResponse {
  reply: ZeloPlanChatMessageResponse | null
  error: string | null
}
