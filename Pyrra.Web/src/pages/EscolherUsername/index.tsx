import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { AtSign, Check, X } from 'lucide-react'
import { useAuth } from '../../hooks/useAuth'
import {
  checkUsernameAvailability,
  setUsername as setUsernameApi,
} from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'

// mesma regra do backend (UsernameService): 3-20, letras minúsculas, números e underscore
const USERNAME_PATTERN = /^[a-z0-9_]{3,20}$/

// tira "@" e espaços e baixa pra minúsculas — o que será de fato gravado
function normalize(raw: string): string {
  return raw.trim().replace(/^@+/, '').toLowerCase()
}

type Availability =
  | { status: 'idle' }
  | { status: 'checking' }
  | { status: 'available' }
  | { status: 'unavailable'; reason: string }

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 pl-9 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

// primeiro acesso de quem ainda não tem username, checa disponibilidade enquanto digita
export function EscolherUsername() {
  const { user, applyUser } = useAuth()
  const navigate = useNavigate()

  const [value, setValue] = useState('')
  const [availability, setAvailability] = useState<Availability>({ status: 'idle' })
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const normalized = normalize(value)
  const formatValid = USERNAME_PATTERN.test(normalized)

  // debounce pra não bater no servidor a cada tecla, só consulta quando o formato já é válido
  useEffect(() => {
    if (!formatValid) {
      return
    }

    let active = true
    const timer = setTimeout(async () => {
      // "verificando" só aparece quando a checagem de fato começa, depois do debounce
      setAvailability({ status: 'checking' })
      try {
        const result = await checkUsernameAvailability(normalized)
        if (!active) return
        setAvailability(
          result.available
            ? { status: 'available' }
            : { status: 'unavailable', reason: result.reason ?? 'Indisponível.' },
        )
      } catch {
        if (active) setAvailability({ status: 'idle' })
      }
    }, 400)

    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [normalized, formatValid])

  // quem já tem username não precisa desta tela (ex.: voltou pela URL)
  if (user?.username) {
    return <Navigate to="/hoje" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!formatValid || submitting) return

    setSubmitting(true)
    setError(null)

    try {
      const updated = await setUsernameApi(normalized)
      // aplica direto no contexto, o gate de username libera sem precisar de um GET a mais
      applyUser(updated)
      navigate('/hoje', { replace: true })
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 409: 'Esse username já está em uso.' },
          'Não foi possível salvar seu username.',
        ),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <header className="mb-8 text-center">
          <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">
            Escolha seu @
          </h1>
          <p className="mt-2 text-sm text-slate-400">
            É como seus amigos vão te encontrar no Pyrra.
          </p>
        </header>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3" noValidate>
          <div className="flex flex-col gap-1.5">
            <div className="relative">
              <AtSign
                size={16}
                aria-hidden="true"
                className="absolute top-1/2 left-3 -translate-y-1/2 text-slate-500"
              />
              <input
                id="username"
                type="text"
                value={value}
                onChange={(event) => {
                  setValue(event.target.value)
                  if (error) setError(null)
                }}
                autoComplete="off"
                autoCapitalize="none"
                spellCheck={false}
                autoFocus
                placeholder="seunome"
                aria-label="Username"
                aria-invalid={availability.status === 'unavailable'}
                className={inputClasses}
              />
            </div>

            {/* Feedback vivo: formato, verificação e disponibilidade. */}
            {normalized.length > 0 && !formatValid && (
              <p className="text-xs text-slate-500">
                3 a 20 caracteres: letras, números ou underscore.
              </p>
            )}
            {/* formatValid && guarda contra um availability desatualizado de antes do usuário
                digitar um formato inválido — ver comentário no efeito acima. */}

            {formatValid && availability.status === 'checking' && (
              <p className="text-xs text-slate-500">Verificando…</p>
            )}
            {formatValid && availability.status === 'available' && (
              <p className="flex items-center gap-1.5 text-xs text-brand-green">
                <Check size={13} aria-hidden="true" />@{normalized} está livre
              </p>
            )}
            {formatValid && availability.status === 'unavailable' && (
              <p className="flex items-center gap-1.5 text-xs text-red-300">
                <X size={13} aria-hidden="true" />
                {availability.reason}
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={
              submitting || !formatValid || availability.status === 'unavailable'
            }
            className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Salvando…' : 'Continuar'}
          </button>

          {error && (
            <p
              role="alert"
              className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
            >
              {error}
            </p>
          )}
        </form>
      </div>
    </main>
  )
}

export default EscolherUsername
