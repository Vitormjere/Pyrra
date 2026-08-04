import api from './api'
import type { ZeloAnswerResponse, ZeloQuestionRequest } from '../types/zelo'

// pergunta livre ao Zelo sobre os dados do usuário, pode demorar porque o backend agrega contexto e consulta a Anthropic
// erros da API externa viram resposta amigável no campo `resposta`, só 429 (limite diário) e 400 sobem como erro HTTP
export async function askZelo(pergunta: string): Promise<string> {
  const { data } = await api.post<ZeloAnswerResponse>('/api/zelo/perguntar', {
    pergunta,
  } satisfies ZeloQuestionRequest)
  return data.resposta
}
