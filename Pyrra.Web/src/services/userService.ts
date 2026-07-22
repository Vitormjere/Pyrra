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
