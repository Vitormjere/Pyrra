import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getApiErrorMessage } from '../../services/apiError'

// Espelha a regra do backend (AuthService.RegisterAsync rejeita senha com menos
// de 8 caracteres). Validar aqui evita uma ida à API só para ouvir "senha fraca";
// o backend continua sendo a autoridade, esta é só a checagem adiantada.
const MIN_PASSWORD_LENGTH = 8

// Checagem de formato deliberadamente frouxa: só descarta o que é claramente
// inválido. Se um e-mail existe de verdade, quem decide é o servidor.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

interface FieldErrors {
  name?: string
  email?: string
  password?: string
}

function validate(name: string, email: string, password: string): FieldErrors {
  const errors: FieldErrors = {}

  if (!name.trim()) {
    errors.name = 'Informe seu nome.'
  }

  if (!EMAIL_PATTERN.test(email.trim())) {
    errors.email = 'Informe um e-mail válido.'
  }

  if (password.length < MIN_PASSWORD_LENGTH) {
    errors.password = `A senha precisa ter pelo menos ${MIN_PASSWORD_LENGTH} caracteres.`
  }

  return errors
}

const inputClasses =
  'w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

export function Cadastro() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Some com os erros assim que o usuário começa a corrigir, mesmo comportamento
  // do Login: manter a mensagem enquanto ele reescreve passa a impressão de que a
  // correção não surtiu efeito.
  function clearErrors(field: keyof FieldErrors) {
    setFieldErrors((current) => {
      if (current[field] === undefined) {
        return current
      }
      const next = { ...current }
      delete next[field]
      return next
    })

    if (error !== null) {
      setError(null)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const validation = validate(name, email, password)
    setFieldErrors(validation)
    setError(null)

    // Nada é enviado enquanto houver erro de formato — o objetivo da validação
    // no cliente é justamente poupar a viagem.
    if (Object.keys(validation).length > 0) {
      return
    }

    setSubmitting(true)

    try {
      // register() já salva o token e carrega o usuário: o cadastro entra logado,
      // sem passar pela tela de login.
      await register(name.trim(), email.trim(), password)
      navigate('/hoje', { replace: true })
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 409: 'Este e-mail já está cadastrado.' },
          'Não foi possível criar sua conta. Tente novamente.',
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
          <p className="mt-2 text-sm text-slate-400">Crie sua conta.</p>
        </header>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="name" className="text-sm font-medium text-slate-300">
              Nome
            </label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(event) => {
                setName(event.target.value)
                clearErrors('name')
              }}
              autoComplete="name"
              placeholder="Como podemos te chamar?"
              aria-invalid={fieldErrors.name !== undefined}
              className={inputClasses}
            />
            {fieldErrors.name && (
              <p className="text-sm text-red-300">{fieldErrors.name}</p>
            )}
          </div>

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
                clearErrors('email')
              }}
              autoComplete="email"
              placeholder="voce@exemplo.com"
              aria-invalid={fieldErrors.email !== undefined}
              className={inputClasses}
            />
            {fieldErrors.email && (
              <p className="text-sm text-red-300">{fieldErrors.email}</p>
            )}
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
                clearErrors('password')
              }}
              autoComplete="new-password"
              placeholder="Pelo menos 8 caracteres"
              aria-invalid={fieldErrors.password !== undefined}
              className={inputClasses}
            />
            {fieldErrors.password && (
              <p className="text-sm text-red-300">{fieldErrors.password}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 focus-visible:ring-2 focus-visible:ring-brand-green focus-visible:ring-offset-2 focus-visible:ring-offset-brand-dark focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Criando conta...' : 'Criar conta'}
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
          Já tem conta?{' '}
          <Link
            to="/login"
            className="font-medium text-brand-green transition hover:brightness-110"
          >
            Entrar
          </Link>
        </p>
      </div>
    </main>
  )
}

export default Cadastro
