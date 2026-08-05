import { isAxiosError } from 'axios'

// requisição nem chegou ao servidor: API fora do ar, CORS ou certificado self-signed não aceito
export const NETWORK_ERROR_MESSAGE =
  'Não foi possível falar com o servidor. Verifique se a API está no ar.'

// converte um erro de requisição na mensagem pro usuário: status conhecido vence, depois o message do backend, por último o fallback
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
