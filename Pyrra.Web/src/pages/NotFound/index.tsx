import { Link } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import NotFoundScene from './Scene'

// pública, fora do guard de sessão — é o catch-all de qualquer URL desconhecida, então precisa
// funcionar tanto para quem está logado quanto para quem não está
export function NotFound() {
  const { user } = useAuth()
  const backTo = user ? '/hoje' : '/login'

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div className="h-56 w-56 sm:h-64 sm:w-64">
        <NotFoundScene />
      </div>

      <div className="w-full max-w-sm text-center">
        <p className="font-display text-6xl font-semibold tracking-tight text-brand-green">
          404
        </p>
        <h1 className="mt-4 font-display text-2xl font-semibold tracking-tight text-ink">
          Página não encontrada
        </h1>
        <p className="mt-2 text-sm text-slate-400">
          O link que você seguiu pode estar quebrado, ou a página pode ter sido movida.
        </p>

        <Link
          to={backTo}
          className="mt-8 inline-block w-full rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Voltar para o início
        </Link>
      </div>
    </main>
  )
}

export default NotFound
