// único lugar que sabe como a sessão é persistida, isolado pra trocar de estratégia sem tocar no resto
// localStorage expõe o token a XSS, mas serve pro MVP — quando tiver refresh token, trocar por cookie httpOnly
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
