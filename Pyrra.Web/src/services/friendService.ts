import api from './api'
import type {
  Friend,
  FriendRequest,
  InviteLink,
  InviteResult,
  UserSearchResult,
} from '../types/community'

export async function searchUsers(term: string): Promise<UserSearchResult[]> {
  const { data } = await api.get<UserSearchResult[]>('/api/amigos/buscar', {
    params: { termo: term },
  })
  return data
}

export async function getFriends(): Promise<Friend[]> {
  const { data } = await api.get<Friend[]>('/api/amigos')
  return data
}

export async function getPendingRequests(): Promise<FriendRequest[]> {
  const { data } = await api.get<FriendRequest[]>('/api/amigos/pedidos')
  return data
}

export async function getPendingCount(): Promise<number> {
  const { data } = await api.get<{ count: number }>('/api/amigos/pedidos/contagem')
  return data.count
}

// Só o número de amigos, para o Perfil — poupa hidratar a lista inteira.
export async function getFriendsCount(): Promise<number> {
  const { data } = await api.get<{ count: number }>('/api/amigos/contagem')
  return data.count
}

export async function sendFriendRequest(addresseeId: string): Promise<void> {
  await api.post('/api/amigos/pedidos', { addresseeId })
}

export async function acceptFriendRequest(friendshipId: string): Promise<void> {
  await api.post(`/api/amigos/pedidos/${friendshipId}/aceitar`)
}

export async function declineFriendRequest(friendshipId: string): Promise<void> {
  await api.post(`/api/amigos/pedidos/${friendshipId}/recusar`)
}

export async function removeFriendship(friendshipId: string): Promise<void> {
  await api.delete(`/api/amigos/${friendshipId}`)
}

// Link de convite pessoal (estável). Criado no backend na primeira chamada.
export async function getInviteLink(): Promise<InviteLink> {
  const { data } = await api.get<InviteLink>('/api/amigos/convite')
  return data
}

// Abrir um convite: envia o pedido ao dono do link e devolve o desfecho.
export async function acceptInvite(token: string): Promise<InviteResult> {
  const { data } = await api.post<InviteResult>(
    `/api/amigos/convite/${encodeURIComponent(token)}/aceitar`,
  )
  return data
}
