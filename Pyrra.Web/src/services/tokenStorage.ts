// Único lugar que conhece COMO a sessão é persistida. Isolar aqui permite trocar
// localStorage por outra estratégia (cookie httpOnly, sessionStorage) sem tocar no
// axios nem no contexto de autenticação.
//
// localStorage é aceitável para este MVP, mas fica exposto a XSS: qualquer script
// injetado na página consegue ler o token. Quando houver refresh token, o par
// natural é cookie httpOnly + refresh silencioso.
const TOKEN_KEY = 'pyrra.token'

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}
