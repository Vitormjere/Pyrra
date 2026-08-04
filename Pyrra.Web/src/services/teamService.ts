import api from './api'
import type {
  JoinResult,
  PublicTeam,
  Team,
  TeamBannerTheme,
  TeamDetails,
  TeamInvite,
  TeamVisibility,
} from '../types/teams'

export async function createTeam(
  name: string,
  description: string | null,
  memberLimit: number,
  visibility: TeamVisibility,
  bannerTheme: TeamBannerTheme,
): Promise<Team> {
  const { data } = await api.post<Team>('/api/times', {
    name,
    description,
    memberLimit,
    visibility,
    bannerTheme,
  })
  return data
}

export async function getMyTeams(): Promise<Team[]> {
  const { data } = await api.get<Team[]>('/api/times')
  return data
}

// todos os times do site — só pra tela de admin
export async function getAllTeams(): Promise<Team[]> {
  const { data } = await api.get<Team[]>('/api/times/todos')
  return data
}

// times que ainda não estão em nenhum torneio, pra oferecer só esses no botão de solicitar entrada
export async function getMyEligibleTeamsForTournament(): Promise<Team[]> {
  const { data } = await api.get<Team[]>('/api/times/meus/elegiveis-torneio')
  return data
}

// times públicos pra aba Explorar — já vem com o token de convite, afinal são públicos mesmo
export async function getPublicTeams(): Promise<PublicTeam[]> {
  const { data } = await api.get<PublicTeam[]>('/api/times/publicos')
  return data
}

export async function setTeamVisibility(teamId: string, visibility: TeamVisibility): Promise<void> {
  await api.post(`/api/times/${teamId}/visibilidade`, { visibility })
}

export async function setTeamBannerTheme(teamId: string, bannerTheme: TeamBannerTheme): Promise<Team> {
  const { data } = await api.post<Team>(`/api/times/${teamId}/tema`, { bannerTheme })
  return data
}

// validação de tipo e tamanho é toda do backend, erro chega como { message }
export async function uploadTeamBannerImage(teamId: string, file: File): Promise<Team> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await api.post<Team>(`/api/times/${teamId}/banner`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data
}

export async function removeTeamBannerImage(teamId: string): Promise<Team> {
  const { data } = await api.delete<Team>(`/api/times/${teamId}/banner`)
  return data
}

export async function getTeamDetails(teamId: string): Promise<TeamDetails> {
  const { data } = await api.get<TeamDetails>(`/api/times/${teamId}`)
  return data
}

export async function inviteFriendToTeam(teamId: string, friendUserId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/convidar`, { friendUserId })
}

export async function getPendingTeamInvites(): Promise<TeamInvite[]> {
  const { data } = await api.get<TeamInvite[]>('/api/times/convites')
  return data
}

export async function getPendingTeamInvitesCount(): Promise<number> {
  const { data } = await api.get<{ count: number }>('/api/times/convites/contagem')
  return data.count
}

export async function acceptTeamInvite(inviteId: string): Promise<void> {
  await api.post(`/api/times/convites/${inviteId}/aceitar`)
}

export async function declineTeamInvite(inviteId: string): Promise<void> {
  await api.post(`/api/times/convites/${inviteId}/recusar`)
}

// time cheio ou já ser membro não é erro aqui, o resultado vem no corpo da resposta
export async function joinTeamViaLink(token: string): Promise<JoinResult> {
  const { data } = await api.post<JoinResult>(
    `/api/times/convite/${encodeURIComponent(token)}/entrar`,
  )
  return data
}

export async function leaveTeam(teamId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/sair`)
}

export async function removeTeamMember(teamId: string, memberUserId: string): Promise<void> {
  await api.delete(`/api/times/${teamId}/membros/${memberUserId}`)
}

export async function deleteTeam(teamId: string): Promise<void> {
  await api.delete(`/api/times/${teamId}`)
}

export async function transferTeamOwnership(teamId: string, newOwnerUserId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/transferir`, { newOwnerUserId })
}
