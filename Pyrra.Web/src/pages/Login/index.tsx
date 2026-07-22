import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getApiErrorMessage } from '../../services/apiError'

export function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Some com o erro assim que o usuário começa a corrigir o que digitou: manter a
  // mensagem na tela enquanto ele reescreve o e-mail passa a impressão de que a
  // correção não surtiu efeito. O guard evita disparar setState a cada tecla
  // quando já não há erro nenhum para limpar.
  function clearError() {
    if (error !== null) {
      setError(null)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    setError(null)
    setSubmitting(true)

    try {
      await login(email, password)
      // replace: o login não deve ficar no histórico — o "voltar" a partir de
      // /hoje levaria de volta a um formulário que não faz mais sentido.
      navigate('/hoje', { replace: true })
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 401: 'E-mail ou senha incorretos.' },
          'Não foi possível entrar. Tente novamente.',
        ),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <header className="mb-10 text-center">
          <h1 className="font-display text-5xl tracking-tight text-brand-green">
            Pyrra
          </h1>
          <p className="mt-2 text-sm text-slate-400">Bem-vindo de volta.</p>
        </header>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="email" className="text-sm font-medium text-slate-300">
              E-mail
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(event) => {
                setEmail(event.target.value)
                clearError()
              }}
              autoComplete="email"
              required
              placeholder="voce@exemplo.com"
              className="w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="password"
              className="text-sm font-medium text-slate-300"
            >
              Senha
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(event) => {
                setPassword(event.target.value)
                clearError()
              }}
              autoComplete="current-password"
              required
              placeholder="••••••••"
              className="w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 focus-visible:ring-2 focus-visible:ring-brand-green focus-visible:ring-offset-2 focus-visible:ring-offset-brand-dark focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Entrando...' : 'Entrar'}
          </button>

          {/* role=alert faz o leitor de tela anunciar o erro assim que ele aparece. */}
          {error && (
            <p
              role="alert"
              className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
            >
              {error}
            </p>
          )}
        </form>

        <p className="mt-8 text-center text-sm text-slate-400">
          Ainda não tem conta?{' '}
          <Link
            to="/cadastro"
            className="font-medium text-brand-green transition hover:brightness-110"
          >
            Criar conta
          </Link>
        </p>
      </div>
    </main>
  )
}

export default Login
