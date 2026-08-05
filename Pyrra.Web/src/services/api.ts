import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { clearToken, getToken } from './tokenStorage'

const LOGIN_ROUTE = '/login'

// default cobre quem ainda não copiou o .env, outros ambientes definem VITE_API_URL no build
export const baseURL = import.meta.env.VITE_API_URL ?? 'https://localhost:7294'

export const api = axios.create({ baseURL })

// lê o token a cada requisição, em vez de fixar no header na criação, pra login/logout valerem na hora
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // 401 = token ausente, inválido ou expirado, então limpa e manda pro login
    if (error.response?.status === 401) {
      clearToken()

      // window.location porque o interceptor fica fora da árvore de componentes e não tem os hooks do router
      // a guarda evita loop: sem ela um 401 da própria tela de login recarregaria a página e engoliria o erro
      if (window.location.pathname !== LOGIN_ROUTE) {
        window.location.assign(LOGIN_ROUTE)
      }
    }

    return Promise.reject(error)
  },
)

export default api
