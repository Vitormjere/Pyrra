import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import PasswordInput from '../../components/PasswordInput'
import { resetPassword } from '../../services/authService'
import { getApiErrorMessage } from '../../services/apiError'

// Pública — chegado do link de e-mail, token vem na query string (?token=...), não em sessão
export function RedefinirSenha() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // mesmo critério do resto do app (Cadastro/Configurações): 8+ caracteres, 1 maiúscula, 1 número
  const passwordValid = newPassword.length >= 8 && /[A-Z]/.test(newPassword) && /[0-9]/.test(newPassword)
  const confirmMatches = confirmPassword.length > 0 && confirmPassword === newPassword
  const canSubmit = Boolean(token) && passwordValid && confirmMatches

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || submitting) return

    setSubmitting(true)
    setError(null)
    try {
      await resetPassword(token, newPassword)
      setDone(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível redefinir sua senha.'))
    } finally {
      setSubmitting(false)
    }
  }

  if (!token) {
    return (
      <Centered>
        <p className="text-sm text-red-300">Link inválido — falta o token de redefinição.</p>
        <RequestNewLink />
      </Centered>
    )
  }

  if (done) {
    return (
      <Centered>
        <p role="status" className="text-sm text-brand-green">Senha redefinida! Já pode entrar com ela.</p>
        <Link
          to="/login"
          className="mt-6 inline-block w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para o login
        </Link>
      </Centered>
    )
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <header className="mb-10 text-center">
          <h1 className="font-display text-4xl font-semibold tracking-tight text-ink">
            Nova senha
          </h1>
          <p className="mt-2 text-sm text-slate-400">Escolha uma nova senha pra sua conta.</p>
        </header>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="nova-senha" className="text-sm font-medium text-slate-300">
              Nova senha
            </label>
            <PasswordInput
              id="nova-senha"
              value={newPassword}
              onChange={(event) => {
                setNewPassword(event.target.value)
                if (error) setError(null)
              }}
              autoComplete="new-password"
              autoFocus
              placeholder="Crie uma senha forte"
            />
            <p className="text-xs text-slate-500">Mínimo de 8 caracteres, com 1 letra maiúscula e 1 número.</p>
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="confirmar-senha" className="text-sm font-medium text-slate-300">
              Confirmar nova senha
            </label>
            <PasswordInput
              id="confirmar-senha"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              autoComplete="new-password"
              placeholder="Repita a senha"
            />
            {confirmPassword.length > 0 && !confirmMatches && (
              <p className="text-xs text-red-300">As senhas não coincidem.</p>
            )}
          </div>

          <button
            type="submit"
            disabled={!canSubmit || submitting}
            className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Salvando...' : 'Redefinir senha'}
          </button>

          {error && (
            <>
              <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20">
                {error}
              </p>
              <RequestNewLink />
            </>
          )}
        </form>
      </div>
    </main>
  )
}

// aparece quando o token é ausente/inválido/expirado — a mensagem de erro do backend não diz
// qual dos três é, então o caminho seguro é sempre oferecer pedir um link novo
function RequestNewLink() {
  return (
    <p className="mt-4 text-center text-sm text-slate-400">
      <Link to="/esqueci-senha" className="font-medium text-brand-green transition hover:brightness-110">
        Solicitar um novo link
      </Link>
    </p>
  )
}

function Centered({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12 text-center">
      <div className="w-full max-w-sm">{children}</div>
    </main>
  )
}

export default RedefinirSenha
