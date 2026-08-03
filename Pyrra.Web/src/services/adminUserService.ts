import api from './api'
import type { AdminUser, CreateAdminAccountRequest } from '../types/admin'

// Cria uma nova conta JÁ admin — nasce com onboarding concluído, sem passar pelo gate de username.
export async function createAdminAccount(request: CreateAdminAccountRequest): Promise<AdminUser> {
  const { data } = await api.post<AdminUser>('/api/admin/contas', request)
  return data
}

// Todos os usuários, inclusive contas excluídas (deletedAt preenchido) — só a listagem
// administrativa enxerga isso.
export async function getAllUsers(): Promise<AdminUser[]> {
  const { data } = await api.get<AdminUser[]>('/api/admin/usuarios')
  return data
}

// Soft delete — sem exigir a senha do alvo, autorizado pelo IsAdmin de quem chama. O backend
// recusa se o alvo também for admin.
export async function deleteUser(userId: string): Promise<void> {
  await api.delete(`/api/admin/usuarios/${userId}`)
}
