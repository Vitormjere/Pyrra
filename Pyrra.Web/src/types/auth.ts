// Espelha os DTOs de Pyrra.Api/Dtos/Auth. Os nomes vêm em camelCase porque é a
// política padrão de serialização do ASP.NET Core; os enums vêm como TEXTO por
// causa do JsonStringEnumConverter registrado no Program.cs.

// Enums do backend viram union de strings, não enum do TypeScript: o tsconfig usa
// erasableSyntaxOnly, que proíbe enum, e a união já dá a mesma checagem em tempo
// de compilação sem gerar código.
export type CommunicationTone = 'Direto' | 'Acolhedor' | 'Desafiador'

export type UserPlan = 'Free' | 'Premium'

// Quem pode ver o perfil público do usuário. Publico: qualquer usuário logado. SomenteAmigos: só
// amigos confirmados — pedido pendente não conta.
export type ProfileVisibility = 'Publico' | 'SomenteAmigos'

// Cor de destaque do app (botões, links, ícones ativos, gráficos, splash, badges). Verde é o
// padrão/valor histórico — ver utils/accentColors.ts pros hex de cada uma.
export type AccentColor = 'Verde' | 'Azul' | 'Rosa' | 'Roxo' | 'Vermelho' | 'Laranja' | 'Amarelo'

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
  /** Identificador público (ex.: "vitorj", exibido como "@vitorj"). null até ser escolhido —
   *  é o que o gate de username usa para forçar a escolha no primeiro acesso. Vem sem "@". */
  username: string | null
  /** null até o usuário enviar uma foto — fallback é o círculo com a inicial. */
  profilePictureUrl: string | null
  /** IANA time zone (ex.: "America/Sao_Paulo"). */
  timezone: string
  communicationTone: CommunicationTone
  /** Hora local no formato "HH:mm". */
  eveningNotificationTime: string
  plan: UserPlan
  profileVisibility: ProfileVisibility
  accentColor: AccentColor
  /** true depois que o usuário concluiu ou pulou o onboarding de primeiro acesso. */
  onboardingCompleted: boolean
  createdAt: string
  /** Libera ações administrativas na UI (ex.: criar torneio direto, sem passar por solicitação). */
  isAdmin: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  name: string
  captchaToken: string
}
