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
