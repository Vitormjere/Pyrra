import api from './api'
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserResponse,
} from '../types/auth'

export async function login(payload: LoginRequest): Promise<AuthResponse> {
  const { data } = await api.post<AuthResponse>('/api/auth/login', payload)
  return data
}

export async function register(payload: RegisterRequest): Promise<AuthResponse> {
  const { data } = await api.post<AuthResponse>('/api/auth/register', payload)
  return data
}

// idToken vem do Google Identity Services (botão "Entrar com Google") — o backend confere a
// assinatura antes de logar/criar/vincular a conta
export async function loginWithGoogle(idToken: string): Promise<AuthResponse> {
  const { data } = await api.post<AuthResponse>('/api/auth/google', { idToken })
  return data
}

// serve como validação do token salvo, se expirou o endpoint responde 401
export async function me(): Promise<UserResponse> {
  const { data } = await api.get<UserResponse>('/api/auth/me')
  return data
}

export async function confirmEmail(token: string): Promise<void> {
  await api.post('/api/auth/confirmar-email', { token })
}

// sempre resolve com sucesso — a API responde 200 com a mesma mensagem genérica exista ou não
// o e-mail, de propósito (evita descobrir contas cadastradas por essa via)
export async function forgotPassword(email: string): Promise<void> {
  await api.post('/api/auth/esqueci-senha', { email })
}

export async function resetPassword(token: string, newPassword: string): Promise<void> {
  await api.post('/api/auth/redefinir-senha', { token, newPassword })
}
