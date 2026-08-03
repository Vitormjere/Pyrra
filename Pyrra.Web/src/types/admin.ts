// Espelha os DTOs de Pyrra.Api/Dtos/Admin — gestão administrativa de contas (Fase Admin-2).

// GET /api/admin/usuarios e POST /api/admin/contas. Nunca traz senha nem hash.
export interface AdminUser {
  id: string
  email: string
  name: string
  username: string | null
  isAdmin: boolean
  /** DateTime ISO 8601. */
  createdAt: string
  /** DateTime ISO 8601, ou null se a conta está ativa. */
  deletedAt: string | null
}

// POST /api/admin/contas — a senha vai em texto puro, via HTTPS, direto do formulário; o backend
// faz o hash antes de guardar (mesmo caminho de RegisterRequest).
export interface CreateAdminAccountRequest {
  email: string
  name: string
  username: string
  password: string
}
