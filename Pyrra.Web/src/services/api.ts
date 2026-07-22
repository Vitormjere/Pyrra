import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { clearToken, getToken } from './tokenStorage'

const LOGIN_ROUTE = '/login'

// O default cobre o dev que ainda não copiou o .env; qualquer outro ambiente
// define VITE_API_URL no build (Vite injeta em tempo de compilação, não em runtime).
const baseURL = import.meta.env.VITE_API_URL ?? 'https://localhost:7294'

export const api = axios.create({ baseURL })

// Toda chamada sai com o Bearer quando há sessão. Ler o token a cada requisição
// (em vez de fixar no header default na criação) faz login e logout valerem
// imediatamente, sem recriar a instância.
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
    // 401 = token ausente, inválido ou expirado. Não adianta manter o que o backend
    // já recusou, então limpa e manda para o login.
    if (error.response?.status === 401) {
      clearToken()

      // window.location, e não useNavigate: o interceptor vive fora da árvore de
      // componentes e não tem acesso aos hooks do router. O custo é um reload
      // completo, aceitável porque a sessão acabou de ser descartada de qualquer forma.
      //
      // A guarda evita laço: sem ela, um 401 vindo da própria tela de login
      // (credencial errada) recarregaria a página e engoliria a mensagem de erro.
      if (window.location.pathname !== LOGIN_ROUTE) {
        window.location.assign(LOGIN_ROUTE)
      }
    }

    return Promise.reject(error)
  },
)

export default api
