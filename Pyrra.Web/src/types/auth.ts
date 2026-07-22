// Espelha os DTOs de Pyrra.Api/Dtos/Auth. Os nomes vêm em camelCase porque é a
// política padrão de serialização do ASP.NET Core; os enums vêm como TEXTO por
// causa do JsonStringEnumConverter registrado no Program.cs.

// Enums do backend viram union de strings, não enum do TypeScript: o tsconfig usa
// erasableSyntaxOnly, que proíbe enum, e a união já dá a mesma checagem em tempo
// de compilação sem gerar código.
export type CommunicationTone = 'Direto' | 'Acolhedor' | 'Desafiador'

export type UserPlan = 'Free' | 'Premium'

// POST /api/auth/login e POST /api/auth/register
export interface AuthResponse {
  userId: string
  email: string
  name: string
  token: string
  /** DateTime ISO 8601 (ex.: "2026-07-22T21:00:00Z"). */
  expiresAt: string
}

// GET /api/auth/me e PATCH /api/usuario/preferencias.
// Não inclui senha: o backend nunca projeta PasswordHash.
export interface UserResponse {
  id: string
  email: string
  name: string
  /** IANA time zone (ex.: "America/Sao_Paulo"). */
  timezone: string
  communicationTone: CommunicationTone
  /** Hora local no formato "HH:mm". */
  eveningNotificationTime: string
  plan: UserPlan
  createdAt: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  name: string
}
