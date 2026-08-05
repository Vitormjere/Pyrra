import api from './api'
import type { AvailableChallenge, PendingSubmission, TeamCategoryStatus, TeamMemberRanking } from '../types/challenges'

// todas as categorias do catálogo com o flag de ativação, só o dono vê (403/404 pra quem não é)
export async function getTeamCategories(teamId: string): Promise<TeamCategoryStatus[]> {
  const { data } = await api.get<TeamCategoryStatus[]>(`/api/times/${teamId}/desafios/categorias`)
  return data
}

export async function activateTeamCategory(teamId: string, categoryId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/desafios/categorias/${categoryId}`)
}

export async function deactivateTeamCategory(teamId: string, categoryId: string): Promise<void> {
  await api.delete(`/api/times/${teamId}/desafios/categorias/${categoryId}`)
}

// desafios das categorias ativas do time, qualquer membro vê
export async function getAvailableChallenges(teamId: string): Promise<AvailableChallenge[]> {
  const { data } = await api.get<AvailableChallenge[]>(`/api/times/${teamId}/desafios`)
  return data
}

// envia a prova por foto de um desafio, multipart/form-data, mesmo padrão do banner de time
export async function submitChallengeProof(teamId: string, challengeId: string, file: File): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)
  await api.post(`/api/times/${teamId}/desafios/${challengeId}/submissoes`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

// fila de submissões pendentes do time, só o dono vê (403/404 pra quem não é)
export async function getPendingSubmissions(teamId: string): Promise<PendingSubmission[]> {
  const { data } = await api.get<PendingSubmission[]>(`/api/times/${teamId}/desafios/submissoes`)
  return data
}

export async function approveSubmission(teamId: string, submissionId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/desafios/submissoes/${submissionId}/aprovar`)
}

export async function rejectSubmission(teamId: string, submissionId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/desafios/submissoes/${submissionId}/recusar`)
}

// busca a foto via endpoint autenticado e devolve um object URL local — quem chama tem que revogar com URL.revokeObjectURL depois
export async function getSubmissionPhotoUrl(teamId: string, submissionId: string): Promise<string> {
  const { data } = await api.get(`/api/times/${teamId}/desafios/submissoes/${submissionId}/foto`, {
    responseType: 'blob',
  })
  return URL.createObjectURL(data as Blob)
}

// ranking de membros por placar individual (não o TotalPoints coletivo do time), qualquer membro vê
export async function getTeamRanking(teamId: string): Promise<TeamMemberRanking[]> {
  const { data } = await api.get<TeamMemberRanking[]>(`/api/times/${teamId}/desafios/ranking`)
  return data
}
