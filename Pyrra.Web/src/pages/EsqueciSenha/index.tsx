import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { forgotPassword } from '../../services/authService'
import { getApiErrorMessage } from '../../services/apiError'

// Pública, sem sessão — mesmo raciocínio do Login/Cadastro. Sempre termina em sucesso genérico
// (a API responde 200 igual exista ou não o e-mail), então não há um estado de "e-mail não
// encontrado" pra tratar aqui — só rede fora do ar cai no catch.
export function EsqueciSenha() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!email.trim() || submitting) return

    setSubmitting(true)
    setError(null)
    try {
      await forgotPassword(email.trim())
      setSent(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar o link. Tente novamente.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <header className="mb-10 text-center">
          <h1 className="font-display text-4xl font-semibold tracking-tight text-ink">
            Esqueceu a senha?
          </h1>
          <p className="mt-2 text-sm text-slate-400">
            Informe seu e-mail e mandamos um link pra você criar uma nova.
          </p>
        </header>

        {sent ? (
          <p role="status" className="rounded-lg bg-brand-green/10 px-4 py-3 text-center text-sm text-brand-green ring-1 ring-brand-green/20">
            Se esse e-mail estiver cadastrado, você vai receber um link pra redefinir a senha em instantes.
          </p>
        ) : (
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
                  if (error) setError(null)
                }}
                autoComplete="email"
                autoFocus
                required
                placeholder="voce@exemplo.com"
                className="w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
              />
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting ? 'Enviando...' : 'Enviar link'}
            </button>

            {error && (
              <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20">
                {error}
              </p>
            )}
          </form>
        )}

        <p className="mt-8 text-center text-sm text-slate-400">
          <Link to="/login" className="font-medium text-brand-green transition hover:brightness-110">
            Voltar pro login
          </Link>
        </p>
      </div>
    </main>
  )
}

export default EsqueciSenha
