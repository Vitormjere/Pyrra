// Espelha os DTOs de Pyrra.Api/Dtos/Comunidade.

// Projeção pública de um usuário — nunca traz email. username pode ser null em teoria, mas na
// prática só aparece quem já tem username (a busca exige).
export interface UserSummary {
  id: string
  name: string
  username: string | null
}

// GET /api/amigos
export interface Friend {
  friendshipId: string
  user: UserSummary
  /** DateTime ISO — desde quando são amigos. */
  since: string
}

// GET /api/amigos/pedidos
export interface FriendRequest {
  friendshipId: string
  requester: UserSummary
  createdAt: string
}

// Estado do vínculo com um resultado de busca — decide qual botão a UI mostra.
export type FriendRelationState =
  | 'None'
  | 'RequestSent'
  | 'RequestReceived'
  | 'Friends'

// GET /api/amigos/buscar
export interface UserSearchResult {
  user: UserSummary
  state: FriendRelationState
}

// GET /api/amigos/convite
export interface InviteLink {
  token: string
  /** Caminho relativo (/convite/{token}); a URL absoluta é montada com window.location.origin. */
  path: string
}

// Desfecho de abrir um convite.
export type InviteOutcome =
  | 'RequestSent'
  | 'AlreadyFriends'
  | 'AlreadyPending'
  | 'OwnLink'

// POST /api/amigos/convite/{token}/aceitar
export interface InviteResult {
  owner: UserSummary
  outcome: InviteOutcome
}
