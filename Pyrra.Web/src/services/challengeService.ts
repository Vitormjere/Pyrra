import api from './api'
import type { AvailableChallenge, PendingSubmission, TeamCategoryStatus, TeamMemberRanking } from '../types/challenges'

// Todas as categorias do catálogo com o flag de ativação — só o dono vê (403/404 pra quem não é).
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

// Desafios das categorias ativas do time — qualquer membro.
export async function getAvailableChallenges(teamId: string): Promise<AvailableChallenge[]> {
  const { data } = await api.get<AvailableChallenge[]>(`/api/times/${teamId}/desafios`)
  return data
}

// Envia a prova por foto de um desafio — multipart/form-data, mesmo padrão do banner de time.
export async function submitChallengeProof(teamId: string, challengeId: string, file: File): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)
  await api.post(`/api/times/${teamId}/desafios/${challengeId}/submissoes`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

// Fila de submissões pendentes do time — só o dono vê (403/404 pra quem não é).
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

// Busca a foto de uma submissão pelo endpoint autenticado (container privado, sem URL pública) e
// devolve um object URL local — quem chama é responsável por revogar com URL.revokeObjectURL
// quando não precisar mais (ver PendingSubmissionRow em Detalhe.tsx).
export async function getSubmissionPhotoUrl(teamId: string, submissionId: string): Promise<string> {
  const { data } = await api.get(`/api/times/${teamId}/desafios/submissoes/${submissionId}/foto`, {
    responseType: 'blob',
  })
  return URL.createObjectURL(data as Blob)
}

// Ranking de membros do time por placar INDIVIDUAL (não o TotalPoints coletivo do time) —
// qualquer membro (dono ou não) vê.
export async function getTeamRanking(teamId: string): Promise<TeamMemberRanking[]> {
  const { data } = await api.get<TeamMemberRanking[]>(`/api/times/${teamId}/desafios/ranking`)
  return data
}
