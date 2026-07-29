import api from './api'
import type {
  Tournament,
  TournamentDetails,
  TournamentRequest,
  TournamentTeamEntry,
} from '../types/tournaments'
import type { TeamBannerTheme } from '../types/teams'

// Todos os torneios existentes — sem conceito de privacidade, visível a qualquer usuário logado.
export async function getAllTournaments(): Promise<Tournament[]> {
  const { data } = await api.get<Tournament[]>('/api/torneios')
  return data
}

// Torneios cujo dono sou eu.
export async function getMyTournaments(): Promise<Tournament[]> {
  const { data } = await api.get<Tournament[]>('/api/torneios/meus')
  return data
}

// Solicita a criação de um torneio — vira uma TournamentRequest pendente até um admin aprovar.
export async function requestTournament(name: string, description: string | null): Promise<TournamentRequest> {
  const { data } = await api.post<TournamentRequest>('/api/torneios/solicitacoes', { name, description })
  return data
}

// Cria o torneio direto, sem passar por solicitação/aprovação — só admin (o backend devolve 403
// pra quem não for). Quem chama já vira o dono.
export async function createTournament(
  name: string,
  description: string | null,
  bannerTheme: TeamBannerTheme,
): Promise<Tournament> {
  const { data } = await api.post<Tournament>('/api/torneios', { name, description, bannerTheme })
  return data
}

export async function getTournamentDetails(tournamentId: string): Promise<TournamentDetails> {
  const { data } = await api.get<TournamentDetails>(`/api/torneios/${tournamentId}`)
  return data
}

export async function setTournamentBannerTheme(tournamentId: string, bannerTheme: TeamBannerTheme): Promise<Tournament> {
  const { data } = await api.post<Tournament>(`/api/torneios/${tournamentId}/tema`, { bannerTheme })
  return data
}

// Upload de imagem de capa — multipart/form-data, mesmo padrão do banner de time.
export async function uploadTournamentBannerImage(tournamentId: string, file: File): Promise<Tournament> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await api.post<Tournament>(`/api/torneios/${tournamentId}/banner`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data
}

export async function removeTournamentBannerImage(tournamentId: string): Promise<Tournament> {
  const { data } = await api.delete<Tournament>(`/api/torneios/${tournamentId}/banner`)
  return data
}

// Dono do time solicita entrada no torneio (escolhido por id) — só o dono do time.
export async function requestTeamEntry(tournamentId: string, teamId: string): Promise<TournamentTeamEntry> {
  const { data } = await api.post<TournamentTeamEntry>(`/api/torneios/${tournamentId}/times/${teamId}`)
  return data
}

// Mesma coisa, resolvendo o torneio pelo token de convite — usado pela tela de Convite de Torneio.
export async function requestTeamEntryViaInvite(inviteToken: string, teamId: string): Promise<TournamentTeamEntry> {
  const { data } = await api.post<TournamentTeamEntry>(
    `/api/torneios/convite/${encodeURIComponent(inviteToken)}/times/${teamId}`,
  )
  return data
}

// Fila de entradas pendentes do torneio — só o dono do torneio vê (404 pra quem não é).
export async function getPendingEntries(tournamentId: string): Promise<TournamentTeamEntry[]> {
  const { data } = await api.get<TournamentTeamEntry[]>(`/api/torneios/${tournamentId}/entradas`)
  return data
}

export async function approveEntry(tournamentId: string, tournamentTeamId: string): Promise<void> {
  await api.post(`/api/torneios/${tournamentId}/entradas/${tournamentTeamId}/aprovar`)
}

export async function rejectEntry(tournamentId: string, tournamentTeamId: string): Promise<void> {
  await api.post(`/api/torneios/${tournamentId}/entradas/${tournamentTeamId}/recusar`)
}

// Ranking do torneio — times Aprovados, por Score. Sem restrição de dono/membro.
export async function getTournamentRanking(tournamentId: string): Promise<TournamentTeamEntry[]> {
  const { data } = await api.get<TournamentTeamEntry[]>(`/api/torneios/${tournamentId}/ranking`)
  return data
}
