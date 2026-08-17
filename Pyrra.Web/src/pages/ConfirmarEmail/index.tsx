import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Check, Mail, X } from 'lucide-react'
import { confirmEmail } from '../../services/authService'

type State = 'loading' | 'done' | 'error' | 'missing-token'

// Pública, sem exigir sessão — chega de um link de e-mail, pode ser aberta num navegador/
// dispositivo diferente de onde a conta foi criada
export function ConfirmarEmail() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')
  const [state, setState] = useState<State>(token ? 'loading' : 'missing-token')

  // evita confirmar duas vezes (StrictMode remonta o efeito em dev)
  const attempted = useRef(false)

  useEffect(() => {
    if (!token || attempted.current) return
    attempted.current = true

    void (async () => {
      try {
        await confirmEmail(token)
        setState('done')
      } catch {
        setState('error')
      }
    })()
  }, [token])

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm text-center">
        <span
          className={[
            'mx-auto mb-5 flex size-14 items-center justify-center rounded-full',
            state === 'done' ? 'bg-brand-green/10 text-brand-green' : 'bg-surface text-slate-400',
          ].join(' ')}
        >
          {state === 'done' ? <Check size={26} /> : state === 'error' || state === 'missing-token' ? <X size={26} /> : <Mail size={26} />}
        </span>

        <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
          Confirmação de e-mail
        </h1>

        <p className="mt-2 text-sm text-slate-400">
          {state === 'loading' && 'Confirmando seu e-mail…'}
          {state === 'done' && 'E-mail confirmado com sucesso!'}
          {state === 'error' && 'Link inválido ou expirado.'}
          {state === 'missing-token' && 'Link inválido — falta o token de confirmação.'}
        </p>

        <Link
          to="/hoje"
          className="mt-8 inline-block w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para o Pyrra
        </Link>
      </div>
    </main>
  )
}

export default ConfirmarEmail
