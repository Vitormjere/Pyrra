import api from './api'
import type {
  AvailableTournamentChallenge,
  PendingTournamentSubmissionWithTeam,
  TournamentCatalogChallenge,
  TournamentOwnChallenge,
} from '../types/tournamentChallenges'

// ---- gestão do dono do torneio ----

// Catálogo geral inteiro com o status de vínculo a esse torneio — só o dono vê.
export async function getTournamentCatalog(tournamentId: string): Promise<TournamentCatalogChallenge[]> {
  const { data } = await api.get<TournamentCatalogChallenge[]>(`/api/torneios/${tournamentId}/desafios/catalogo`)
  return data
}

export async function linkTournamentCatalogChallenge(tournamentId: string, challengeId: string): Promise<void> {
  await api.post(`/api/torneios/${tournamentId}/desafios/catalogo/${challengeId}`)
}

export async function unlinkTournamentCatalogChallenge(tournamentId: string, challengeId: string): Promise<void> {
  await api.delete(`/api/torneios/${tournamentId}/desafios/catalogo/${challengeId}`)
}

export async function getTournamentOwnChallenges(tournamentId: string): Promise<TournamentOwnChallenge[]> {
  const { data } = await api.get<TournamentOwnChallenge[]>(`/api/torneios/${tournamentId}/desafios/proprios`)
  return data
}

export async function createTournamentOwnChallenge(
  tournamentId: string,
  title: string,
  description: string | null,
  points: number,
): Promise<TournamentOwnChallenge> {
  const { data } = await api.post<TournamentOwnChallenge>(`/api/torneios/${tournamentId}/desafios/proprios`, {
    title,
    description,
    points,
  })
  return data
}

export async function deleteTournamentOwnChallenge(tournamentId: string, challengeId: string): Promise<void> {
  await api.delete(`/api/torneios/${tournamentId}/desafios/proprios/${challengeId}`)
}

// Fila de submissões pendentes do torneio, cruzando todos os times participantes — só o dono vê.
export async function getTournamentPendingSubmissions(tournamentId: string): Promise<PendingTournamentSubmissionWithTeam[]> {
  const { data } = await api.get<PendingTournamentSubmissionWithTeam[]>(`/api/torneios/${tournamentId}/desafios/submissoes`)
  return data
}

// ---- visão de membro de um time participante ----

// Desafios de UM torneio específico em que o time está Aprovado — qualquer membro do time vê.
export async function getAvailableTournamentChallenges(
  teamId: string,
  tournamentId: string,
): Promise<AvailableTournamentChallenge[]> {
  const { data } = await api.get<AvailableTournamentChallenge[]>(`/api/times/${teamId}/desafios/torneios/${tournamentId}`)
  return data
}

// Envia a prova de um desafio do catálogo geral vinculado ao torneio — multipart/form-data,
// mesmo padrão de submitChallengeProof (desafio de time).
export async function submitTournamentCatalogChallengeProof(
  teamId: string,
  tournamentId: string,
  challengeId: string,
  file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)
  await api.post(`/api/times/${teamId}/desafios/torneios/${tournamentId}/catalogo/${challengeId}/submissoes`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

// Envia a prova de um desafio próprio do torneio.
export async function submitTournamentOwnChallengeProof(
  teamId: string,
  tournamentId: string,
  challengeId: string,
  file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)
  await api.post(`/api/times/${teamId}/desafios/torneios/${tournamentId}/proprios/${challengeId}/submissoes`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

export async function approveTournamentSubmission(teamId: string, tournamentId: string, submissionId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/desafios/torneios/${tournamentId}/submissoes/${submissionId}/aprovar`)
}

export async function rejectTournamentSubmission(teamId: string, tournamentId: string, submissionId: string): Promise<void> {
  await api.post(`/api/times/${teamId}/desafios/torneios/${tournamentId}/submissoes/${submissionId}/recusar`)
}
