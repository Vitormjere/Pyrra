import api from './api'
import type { ChallengeCategory } from '../types/challenges'
import type { AdminChallenge, ChallengeCategoryPayload, ChallengePayload } from '../types/challengeCatalog'

// --- Categorias ---

export async function getCategories(): Promise<ChallengeCategory[]> {
  const { data } = await api.get<ChallengeCategory[]>('/api/admin/desafios/categorias')
  return data
}

export async function createCategory(payload: ChallengeCategoryPayload): Promise<ChallengeCategory> {
  const { data } = await api.post<ChallengeCategory>('/api/admin/desafios/categorias', payload)
  return data
}

export async function updateCategory(id: string, payload: ChallengeCategoryPayload): Promise<ChallengeCategory> {
  const { data } = await api.put<ChallengeCategory>(`/api/admin/desafios/categorias/${id}`, payload)
  return data
}

// 409 se a categoria ainda tiver desafios vinculados (ChallengeCategoryInUseException)
export async function deleteCategory(id: string): Promise<void> {
  await api.delete(`/api/admin/desafios/categorias/${id}`)
}

// --- Desafios ---

// sem categoryId, traz o catálogo inteiro
export async function getChallenges(categoryId?: string): Promise<AdminChallenge[]> {
  const { data } = await api.get<AdminChallenge[]>('/api/admin/desafios', {
    params: categoryId ? { categoriaId: categoryId } : undefined,
  })
  return data
}

export async function createChallenge(payload: ChallengePayload): Promise<AdminChallenge> {
  const { data } = await api.post<AdminChallenge>('/api/admin/desafios', payload)
  return data
}

export async function updateChallenge(id: string, payload: ChallengePayload): Promise<AdminChallenge> {
  const { data } = await api.put<AdminChallenge>(`/api/admin/desafios/${id}`, payload)
  return data
}

export async function deleteChallenge(id: string): Promise<void> {
  await api.delete(`/api/admin/desafios/${id}`)
}
