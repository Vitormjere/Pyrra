import api from './api'
import type {
  ZeloPlanChatResponse,
  ZeloPlanChatMessageResponse,
  ZeloPlanPreviewResponse,
  ZeloPlanSessionResponse,
} from '../types/zeloPlan'

// inicia uma sessão nova ou retoma a ativa (Coletando ou PlanoGerado, não expirada) — mesma sessão serve Treino e Nutrição
export async function startZeloPlan(): Promise<ZeloPlanSessionResponse> {
  const { data } = await api.post<ZeloPlanSessionResponse>('/api/zelo/plano/iniciar')
  return data
}

export async function answerZeloPlanQuestion(sessionId: string, resposta: string): Promise<ZeloPlanSessionResponse> {
  const { data } = await api.post<ZeloPlanSessionResponse>(`/api/zelo/plano/${sessionId}/responder`, { resposta })
  return data
}

// chame depois de uma falha de geração (status ainda Coletando, nextQuestion nulo, error preenchido)
export async function retryZeloPlanGeneration(sessionId: string): Promise<ZeloPlanSessionResponse> {
  const { data } = await api.post<ZeloPlanSessionResponse>(`/api/zelo/plano/${sessionId}/tentar-novamente`)
  return data
}

export async function getZeloPlanPreview(sessionId: string): Promise<ZeloPlanPreviewResponse> {
  const { data } = await api.get<ZeloPlanPreviewResponse>(`/api/zelo/plano/${sessionId}/preview`)
  return data
}

// sobrescreve o Plano da Semana (Treino) e o plano de Nutrição do usuário com o plano gerado
export async function applyZeloPlan(sessionId: string): Promise<void> {
  await api.post(`/api/zelo/plano/${sessionId}/aplicar`)
}

// descarta o plano gerado, mantém o que o usuário já tinha
export async function discardZeloPlan(sessionId: string): Promise<void> {
  await api.post(`/api/zelo/plano/${sessionId}/descartar`)
}

export async function getZeloPlanMessages(sessionId: string): Promise<ZeloPlanChatMessageResponse[]> {
  const { data } = await api.get<ZeloPlanChatMessageResponse[]>(`/api/zelo/plano/${sessionId}/mensagens`)
  return data
}

export async function sendZeloPlanMessage(sessionId: string, mensagem: string): Promise<ZeloPlanChatResponse> {
  const { data } = await api.post<ZeloPlanChatResponse>(`/api/zelo/plano/${sessionId}/mensagens`, { mensagem })
  return data
}

// aplica a edição proposta numa mensagem do Zelo (EditStatus precisa estar 'Proposta') direto no
// Treino/Nutrição já aplicados
export async function confirmZeloPlanEdit(sessionId: string, messageId: string): Promise<void> {
  await api.post(`/api/zelo/plano/${sessionId}/mensagens/${messageId}/confirmar`)
}

// descarta a edição proposta sem aplicar nada
export async function dismissZeloPlanEdit(sessionId: string, messageId: string): Promise<void> {
  await api.post(`/api/zelo/plano/${sessionId}/mensagens/${messageId}/descartar`)
}
