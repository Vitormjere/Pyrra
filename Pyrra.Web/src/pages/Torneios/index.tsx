import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Crown, Plus, Trophy } from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import { useAuth } from '../../hooks/useAuth'
import { getAllTournaments } from '../../services/tournamentService'
import { getApiErrorMessage } from '../../services/apiError'
import type { Tournament } from '../../types/tournaments'

// Mesma miniatura 4:3 usada em Times — TeamBanner é genérico o bastante (theme + imageUrl) pra
// servir também ao banner de torneio, que reaproveita TeamBannerTheme.
function TournamentRow({ tournament }: { tournament: Tournament }) {
  return (
    <li className="overflow-hidden rounded-md bg-surface ring-1 ring-line">
      <Link
        to={`/torneios/${tournament.id}`}
        className="flex items-center gap-3 p-3 transition hover:bg-surface-hi"
      >
        <TeamBanner
          theme={tournament.bannerTheme}
          imageUrl={tournament.bannerImageUrl}
          className="w-16 shrink-0 rounded-lg"
        />
        <div className="min-w-0 flex-1">
          <p className="flex items-center gap-1.5 truncate font-medium text-ink">
            {tournament.name}
            {tournament.isOwner && <Crown size={13} className="shrink-0 text-brand-green" aria-hidden="true" />}
          </p>
          {tournament.description && (
            <p className="truncate text-xs text-slate-500">{tournament.description}</p>
          )}
        </div>
      </Link>
    </li>
  )
}

export function Torneios() {
  const { user } = useAuth()
  const [tournaments, setTournaments] = useState<Tournament[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getAllTournaments()
        if (!active) return
        setTournaments(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os torneios.'))
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center justify-between gap-3">
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Torneios
        </h1>
        {/* Admin cria direto (POST /torneios, sem aprovação) — não faz sentido oferecer também
            "Solicitar" pra quem já pode criar na hora e aprovaria o próprio pedido. */}
        {user?.isAdmin ? (
          <Link
            to="/torneios/criar"
            className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95"
          >
            <Plus size={15} aria-hidden="true" />
            Criar torneio
          </Link>
        ) : (
          <Link
            to="/torneios/solicitar"
            className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95"
          >
            <Plus size={15} aria-hidden="true" />
            Solicitar torneio
          </Link>
        )}
      </header>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {loading ? (
        <Skeleton className="h-20" />
      ) : tournaments.length === 0 ? (
        <EmptyState
          icon={Trophy}
          title="Nenhum torneio no momento."
          description={
            user?.isAdmin
              ? 'Crie um torneio — você vira o dono na hora.'
              : 'Solicite a criação de um torneio — um admin revisa e aprova.'
          }
        />
      ) : (
        <ul className="flex flex-col gap-2">
          {tournaments.map((tournament) => (
            <TournamentRow key={tournament.id} tournament={tournament} />
          ))}
        </ul>
      )}
    </div>
  )
}

export default Torneios
