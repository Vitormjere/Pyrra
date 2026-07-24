import api from './api'
import type { ZeloAnswerResponse, ZeloQuestionRequest } from '../types/zelo'

// Pergunta livre ao Zelo sobre os próprios dados do usuário. A chamada pode levar
// alguns segundos: o backend agrega o contexto e consulta a API da Anthropic.
//
// Erros da API externa NÃO chegam aqui como falha: o backend os converte numa
// resposta amigável em `resposta`. O que sobe como erro HTTP é o 429 (limite
// diário atingido) e as validações (400) — a tela lê a mensagem do backend.
export async function askZelo(pergunta: string): Promise<string> {
  const { data } = await api.post<ZeloAnswerResponse>('/api/zelo/perguntar', {
    pergunta,
  } satisfies ZeloQuestionRequest)
  return data.resposta
}
