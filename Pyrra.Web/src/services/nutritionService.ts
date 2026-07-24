import api from './api'
import type {
  DayNutritionResponse,
  MealType,
  NutritionItemResponse,
  NutritionPlanItemResponse,
  PlanDayResponse,
  WeekNutritionResponse,
} from '../types/nutrition'
import type { WeekDay } from '../types/plan'

// date omitida = hoje no fuso do usuário, resolvido no backend.
export async function addItem(
  mealType: MealType,
  itemName: string,
  quantity: string,
  date?: string,
): Promise<NutritionItemResponse> {
  const { data } = await api.post<NutritionItemResponse>('/api/nutricao', {
    mealType,
    itemName,
    quantity,
    date: date ?? null,
  })
  return data
}

// Vem agrupado por refeição, com as quatro sempre presentes.
export async function getForDay(date?: string): Promise<DayNutritionResponse> {
  const { data } = await api.get<DayNutritionResponse>('/api/nutricao', {
    params: date ? { date } : undefined,
  })
  return data
}

// Os 7 dias da semana, cada um agrupado por refeição.
export async function getForWeek(
  weekStart?: string,
): Promise<WeekNutritionResponse> {
  const { data } = await api.get<WeekNutritionResponse>('/api/nutricao/semana', {
    params: weekStart ? { inicio: weekStart } : undefined,
  })
  return data
}

// Remoção é definitiva — não há soft delete no backend.
export async function removeItem(itemId: string): Promise<void> {
  await api.delete(`/api/nutricao/${itemId}`)
}

// Grade completa 7x4 do plano semanal.
export async function getPlan(): Promise<PlanDayResponse[]> {
  const { data } = await api.get<PlanDayResponse[]>('/api/nutricao/plano')
  return data
}

export async function addPlanItem(
  day: WeekDay,
  mealType: MealType,
  itemName: string,
  quantity: string,
): Promise<NutritionPlanItemResponse> {
  const { data } = await api.post<NutritionPlanItemResponse>(
    '/api/nutricao/plano',
    { day, mealType, itemName, quantity },
  )
  return data
}

export async function removePlanItem(itemId: string): Promise<void> {
  await api.delete(`/api/nutricao/plano/${itemId}`)
}

// Edita só nome e quantidade; refeição e data permanecem.
export async function updateItem(
  itemId: string,
  itemName: string,
  quantity: string,
): Promise<NutritionItemResponse> {
  const { data } = await api.put<NutritionItemResponse>(
    `/api/nutricao/${itemId}`,
    { itemName, quantity },
  )
  return data
}
