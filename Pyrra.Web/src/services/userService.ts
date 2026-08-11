import api from './api'
import type { CommunicationTone, ProfileVisibility, UserResponse } from '../types/auth'
import type { PublicProfile } from '../types/profile'

export interface UsernameAvailability {
  available: boolean
  reason: string | null
}

// define ou troca o username, o backend normaliza (minúsculas, sem "@") e valida
export async function setUsername(username: string): Promise<UserResponse> {
  const { data } = await api.put<UserResponse>('/api/usuario/username', {
    username,
  })
  return data
}

// checa disponibilidade enquanto o usuário digita, sem gravar nada
export async function checkUsernameAvailability(
  username: string,
): Promise<UsernameAvailability> {
  const { data } = await api.get<UsernameAvailability>(
    '/api/usuario/username/disponivel',
    { params: { username } },
  )
  return data
}

// atualiza as preferências, é tudo-ou-nada: o backend exige os dois campos, não dá pra mudar só o horário
export async function updatePreferences(
  communicationTone: CommunicationTone,
  eveningNotificationTime: string,
): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/preferencias', {
    communicationTone,
    eveningNotificationTime,
  })
  return data
}

// conclui (ou pula) o onboarding — as preferências são opcionais, ao pular manda só o horário padrão
export async function completeOnboarding(prefs?: {
  communicationTone?: CommunicationTone
  eveningNotificationTime?: string
}): Promise<UserResponse> {
  const { data } = await api.post<UserResponse>(
    '/api/usuario/onboarding/concluir',
    prefs ?? {},
  )
  return data
}

// foto de perfil — mesmo padrão do banner de time (validação de tipo/tamanho é do backend)
export async function uploadProfilePicture(file: File): Promise<UserResponse> {
  const formData = new FormData()
  formData.append('file', file)
  // não fixar Content-Type: o navegador precisa gerar o boundary do multipart sozinho
  const { data } = await api.post<UserResponse>('/api/usuario/foto', formData)
  return data
}

export async function removeProfilePicture(): Promise<UserResponse> {
  const { data } = await api.delete<UserResponse>('/api/usuario/foto')
  return data
}

// daqui pra baixo: ações da tela de Configurações (edição de conta)

export async function updateName(name: string): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/nome', { name })
  return data
}

// exige a senha atual, o backend confere antes de checar unicidade do novo e-mail
export async function changeEmail(
  newEmail: string,
  currentPassword: string,
): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/email', {
    newEmail,
    currentPassword,
  })
  return data
}

export async function changePassword(
  currentPassword: string,
  newPassword: string,
): Promise<void> {
  await api.patch('/api/usuario/senha', { currentPassword, newPassword })
}

// IANA time zone (ex: "America/Sao_Paulo"), validado no servidor
export async function updateTimezone(timezone: string): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/fuso', {
    timezone,
  })
  return data
}

// soft delete, exige a senha atual — depois disso o token para de funcionar, mas o chamador ainda precisa dar logout()
export async function deleteAccount(currentPassword: string): Promise<void> {
  await api.delete('/api/usuario', { data: { currentPassword } })
}

// quem pode ver o perfil público: Publico (qualquer logado) ou SomenteAmigos
export async function updateProfileVisibility(
  visibility: ProfileVisibility,
): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/privacidade', {
    visibility,
  })
  return data
}

// perfil público de terceiro por username, lança 403 se for "somente amigos" ou 404 se não existir — a tela trata via getApiErrorMessage
export async function getPublicProfile(username: string): Promise<PublicProfile> {
  const { data } = await api.get<PublicProfile>(
    `/api/usuario/${encodeURIComponent(username)}/perfil`,
  )
  return data
}
