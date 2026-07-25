import api from './api'
import type { CommunicationTone, UserResponse } from '../types/auth'

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
