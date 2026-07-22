import { isAxiosError } from 'axios'

// A requisição nem chegou ao servidor: API fora do ar, CORS ou o certificado
// self-signed do https://localhost ainda não aceito no navegador.
export const NETWORK_ERROR_MESSAGE =
  'Não foi possível falar com o servidor. Verifique se a API está no ar.'

/**
 * Converte um erro de requisição na mensagem que o usuário vê.
 *
 * A ordem é deliberada: status conhecido pela tela vence, depois o { message }
 * que o backend manda nos erros tratados, e por último o texto genérico.
 *
 * @param statusMessages textos específicos da tela por código HTTP (ex.: { 409: '...' })
 * @param fallback usado quando nada mais explica a falha
 */
export function getApiErrorMessage(
  error: unknown,
  statusMessages: Record<number, string>,
  fallback: string,
): string {
  if (isAxiosError(error)) {
    if (!error.response) {
      return NETWORK_ERROR_MESSAGE
    }

    const knownMessage = statusMessages[error.response.status]
    if (knownMessage) {
      return knownMessage
    }

    const data = error.response.data as { message?: string } | undefined
    if (data?.message) {
      return data.message
    }
  }

  return fallback
}
