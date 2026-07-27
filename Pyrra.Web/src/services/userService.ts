import api from './api'
import type { CommunicationTone, UserResponse } from '../types/auth'

export interface UsernameAvailability {
  available: boolean
  reason: string | null
}

/** Define ou troca o username. O backend normaliza (minúsculas, sem "@") e valida. */
export async function setUsername(username: string): Promise<UserResponse> {
  const { data } = await api.put<UserResponse>('/api/usuario/username', {
    username,
  })
  return data
}

/** Checagem de disponibilidade enquanto o usuário digita, sem gravar. */
export async function checkUsernameAvailability(
  username: string,
): Promise<UsernameAvailability> {
  const { data } = await api.get<UsernameAvailability>(
    '/api/usuario/username/disponivel',
    { params: { username } },
  )
  return data
}

/**
 * Atualiza as preferências do usuário autenticado. É tudo-ou-nada: o backend
 * exige os dois campos, então não dá para mudar só o horário.
 *
 * @param eveningNotificationTime hora local no formato "HH:mm".
 */
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

/**
 * Conclui (ou pula) o onboarding de primeiro acesso. As preferências são
 * OPCIONAIS: ao configurar, o frontend manda as duas; ao pular, manda só o
 * horário (21:00). O backend sempre marca o onboarding como feito e devolve o
 * usuário atualizado, com onboardingCompleted = true.
 */
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

// A partir daqui: ações da tela de Configurações (edição de conta).

export async function updateName(name: string): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/nome', { name })
  return data
}

/** Exige a senha atual — o backend confere antes de checar unicidade do novo e-mail. */
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

/** IANA time zone (ex.: "America/Sao_Paulo"). Validado no servidor. */
export async function updateTimezone(timezone: string): Promise<UserResponse> {
  const { data } = await api.patch<UserResponse>('/api/usuario/fuso', {
    timezone,
  })
  return data
}

/**
 * Exclusão de conta (soft delete): exige a senha atual como reautenticação
 * forte antes de uma ação irreversível. Depois desta chamada, o token atual
 * deixa de funcionar em qualquer endpoint que carregue o usuário — o chamador
 * ainda assim deve encerrar a sessão local explicitamente (logout()).
 */
export async function deleteAccount(currentPassword: string): Promise<void> {
  await api.delete('/api/usuario', { data: { currentPassword } })
}
