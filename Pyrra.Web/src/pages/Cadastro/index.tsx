import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import HCaptcha from '@hcaptcha/react-hcaptcha'
import { Check, Circle } from 'lucide-react'
import PasswordInput from '../../components/PasswordInput'
import { useAuth } from '../../hooks/useAuth'
import { getApiErrorMessage } from '../../services/apiError'

// site key é pública (vai pro HTML, qualquer um vê no devtools), mas mesmo assim não fica
// commitada — vem de Pyrra.Web/.env.local (gitignorado) em dev e de uma env var do Vercel em
// produção, mesma lógica de "nunca no repo" usada pro resto das chaves do projeto
const HCAPTCHA_SITE_KEY = import.meta.env.VITE_HCAPTCHA_SITE_KEY ?? ''

// lista viva de requisitos — o backend exige 8 caracteres + maiúscula + número; o caractere
// especial é regra só do frontend (mais estrita, não conflitante)
const PASSWORD_RULES: readonly { label: string; test: (value: string) => boolean }[] = [
  { label: 'Pelo menos 8 caracteres', test: (v) => v.length >= 8 },
  { label: 'Uma letra maiúscula', test: (v) => /[A-Z]/.test(v) },
  { label: 'Um número', test: (v) => /[0-9]/.test(v) },
  { label: 'Um caractere especial (ex: !@#$)', test: (v) => /[^A-Za-z0-9]/.test(v) },
]

// checagem frouxa de propósito — só descarta o claramente inválido, quem decide se o e-mail existe é o servidor
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

interface FieldErrors {
  name?: string
  email?: string
  password?: string
  confirmPassword?: string
  captcha?: string
}

function validate(
  name: string,
  email: string,
  password: string,
  confirmPassword: string,
  captchaToken: string | null,
): FieldErrors {
  const errors: FieldErrors = {}

  if (!name.trim()) {
    errors.name = 'Informe seu nome.'
  }

  if (!EMAIL_PATTERN.test(email.trim())) {
    errors.email = 'Informe um e-mail válido.'
  }

  if (PASSWORD_RULES.some((rule) => !rule.test(password))) {
    errors.password = 'A senha não atende a todos os requisitos abaixo.'
  }

  // igualdade é só client-side, o backend não conhece o campo de confirmação
  if (!confirmPassword) {
    errors.confirmPassword = 'Confirme sua senha.'
  } else if (password !== confirmPassword) {
    errors.confirmPassword = 'As senhas não coincidem.'
  }

  if (!captchaToken) {
    errors.captcha = 'Confirme que você não é um robô.'
  }

  return errors
}

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

export function Cadastro() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const captchaRef = useRef<HCaptcha>(null)

  // some com os erros assim que o usuário começa a corrigir, mesmo comportamento do Login
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

    const validation = validate(name, email, password, confirmPassword, captchaToken)
    setFieldErrors(validation)
    setError(null)

    // nada é enviado enquanto houver erro de formato — a validação no cliente existe pra poupar a viagem
    if (Object.keys(validation).length > 0) {
      return
    }

    setSubmitting(true)

    try {
      // register() já salva o token e carrega o usuário — o cadastro entra logado direto
      await register(name.trim(), email.trim(), password, captchaToken as string)
      navigate('/hoje', { replace: true })
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 409: 'Este e-mail já está cadastrado.' },
          'Não foi possível criar sua conta. Tente novamente.',
        ),
      )
      // token do hCaptcha só serve uma vez — falhou por qualquer motivo, precisa resolver de novo
      captchaRef.current?.resetCaptcha()
      setCaptchaToken(null)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <header className="mb-10 text-center">
          <h1 className="font-display text-5xl font-semibold tracking-tight text-ink">
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
            <PasswordInput
              id="password"
              value={password}
              onChange={(event) => {
                setPassword(event.target.value)
                clearErrors('password')
                // mudar a senha invalida um "não coincidem" anterior
                clearErrors('confirmPassword')
              }}
              autoComplete="new-password"
              placeholder="Crie uma senha forte"
              aria-invalid={fieldErrors.password !== undefined}
            />
            {fieldErrors.password && (
              <p className="text-sm text-red-300">{fieldErrors.password}</p>
            )}

            {/* Lista viva de requisitos: cada critério fica verde assim que é
                atendido, dando feedback enquanto o usuário digita. */}
            <ul className="mt-1 flex flex-col gap-1" aria-label="Requisitos da senha">
              {PASSWORD_RULES.map((rule) => {
                const met = rule.test(password)
                return (
                  <li key={rule.label} className="flex items-center gap-2 text-xs">
                    {met ? (
                      <Check
                        size={14}
                        className="shrink-0 text-brand-green"
                        aria-hidden="true"
                      />
                    ) : (
                      <Circle
                        size={14}
                        className="shrink-0 text-slate-600"
                        aria-hidden="true"
                      />
                    )}
                    <span className={met ? 'text-slate-300' : 'text-slate-500'}>
                      {rule.label}
                    </span>
                  </li>
                )
              })}
            </ul>
          </div>

          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="confirmPassword"
              className="text-sm font-medium text-slate-300"
            >
              Confirmar senha
            </label>
            <PasswordInput
              id="confirmPassword"
              value={confirmPassword}
              onChange={(event) => {
                setConfirmPassword(event.target.value)
                clearErrors('confirmPassword')
              }}
              autoComplete="new-password"
              placeholder="Repita a senha"
              aria-invalid={fieldErrors.confirmPassword !== undefined}
            />
            {fieldErrors.confirmPassword && (
              <p className="text-sm text-red-300">{fieldErrors.confirmPassword}</p>
            )}
          </div>

          <div className="flex flex-col items-center gap-1.5">
            <HCaptcha
              ref={captchaRef}
              sitekey={HCAPTCHA_SITE_KEY}
              onVerify={(token) => {
                setCaptchaToken(token)
                clearErrors('captcha')
              }}
              onExpire={() => setCaptchaToken(null)}
              onError={() => setCaptchaToken(null)}
            />
            {fieldErrors.captcha && (
              <p className="text-sm text-red-300">{fieldErrors.captcha}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="mt-2 w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 focus-visible:ring-2 focus-visible:ring-brand-green focus-visible:ring-offset-2 focus-visible:ring-offset-brand-dark focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Criando conta...' : 'Criar conta'}
          </button>

          {/* Aceite: o cadastro é o ato de concordância com os Termos. Link abre
              a página pública /termos, acessível sem sessão. */}
          <p className="text-center text-xs leading-relaxed text-slate-500">
            Ao criar sua conta, você concorda com os{' '}
            <Link
              to="/termos"
              className="font-medium text-brand-green transition hover:brightness-110"
            >
              Termos de Uso
            </Link>
            .
          </p>

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
