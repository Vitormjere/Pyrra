import { useCallback, useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Check, Copy, Crown, ImagePlus, Link2, Medal, Trophy, UserPlus, X } from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import TeamEntryPicker from '../../components/TeamEntryPicker'
import { getMyEligibleTeamsForTournament } from '../../services/teamService'
import {
  approveEntry,
  getPendingEntries,
  getTournamentDetails,
  getTournamentRanking,
  rejectEntry,
  removeTournamentBannerImage,
  requestTeamEntry,
  setTournamentBannerTheme,
  uploadTournamentBannerImage,
} from '../../services/tournamentService'
import { getApiErrorMessage } from '../../services/apiError'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { Team, TeamBannerTheme } from '../../types/teams'
import type { TournamentDetails, TournamentTeamEntry } from '../../types/tournaments'

const MAX_BANNER_BYTES = 3 * 1024 * 1024
const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

const listClasses =
  'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

function RankingRow({ entry, position }: { entry: TournamentTeamEntry; position: number }) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span className="w-5 shrink-0 text-center text-sm font-semibold tabular-nums text-slate-400">
        {position}
      </span>
      <TeamBanner
        theme={entry.teamBannerTheme}
        imageUrl={entry.teamBannerImageUrl}
        className="w-12 shrink-0 rounded-md"
      />
      <p className="min-w-0 flex-1 truncate font-medium text-ink">{entry.teamName}</p>
      <span className="shrink-0 text-sm font-semibold text-brand-green tabular-nums">
        {entry.score} pts
      </span>
    </li>
  )
}

function PendingEntryRow({
  entry,
  busy,
  onApprove,
  onReject,
}: {
  entry: TournamentTeamEntry
  busy: boolean
  onApprove: () => void
  onReject: () => void
}) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <TeamBanner
        theme={entry.teamBannerTheme}
        imageUrl={entry.teamBannerImageUrl}
        className="w-12 shrink-0 rounded-md"
      />
      <p className="min-w-0 flex-1 truncate font-medium text-ink">{entry.teamName}</p>
      <div className="flex shrink-0 items-center gap-2">
        <button
          type="button"
          disabled={busy}
          onClick={onApprove}
          className="inline-flex items-center gap-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
        >
          <Check size={15} aria-hidden="true" />
          Aprovar
        </button>
        <button
          type="button"
          disabled={busy}
          onClick={onReject}
          aria-label={`Recusar entrada de ${entry.teamName}`}
          className="rounded-xl p-2 text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
        >
          <X size={15} />
        </button>
      </div>
    </li>
  )
}

export function TorneioDetalhe() {
  const { id } = useParams<{ id: string }>()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [details, setDetails] = useState<TournamentDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [copied, setCopied] = useState(false)

  const [bannerBusy, setBannerBusy] = useState(false)
  const [bannerFileError, setBannerFileError] = useState<string | null>(null)

  const [ranking, setRanking] = useState<TournamentTeamEntry[] | null>(null)
  const [pendingEntries, setPendingEntries] = useState<TournamentTeamEntry[] | null>(null)
  const [entryBusyId, setEntryBusyId] = useState<string | null>(null)

  // "Solicitar entrada" — visível a qualquer usuário (mesmo quem não é dono do torneio), desde
  // que tenha pelo menos um time próprio sem entrada ativa em outro torneio agora.
  const [eligibleTeams, setEligibleTeams] = useState<Team[] | null>(null)
  const [showEntryPicker, setShowEntryPicker] = useState(false)
  const [directEntryBusy, setDirectEntryBusy] = useState(false)
  const [directEntrySent, setDirectEntrySent] = useState(false)
  const [directEntryError, setDirectEntryError] = useState<string | null>(null)

  const loadDetails = useCallback(async () => {
    if (!id) return
    try {
      const data = await getTournamentDetails(id)
      setDetails(data)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o torneio.'))
    }
  }, [id])

  useEffect(() => {
    let active = true

    async function run() {
      if (!id) return
      try {
        const data = await getTournamentDetails(id)
        if (!active) return
        setDetails(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o torneio.'))
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [id])

  const loadRanking = useCallback(async () => {
    if (!id) return
    try {
      setRanking(await getTournamentRanking(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o ranking.'))
    }
  }, [id])

  const loadPendingEntries = useCallback(async () => {
    if (!id) return
    try {
      setPendingEntries(await getPendingEntries(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as entradas pendentes.'))
    }
  }, [id])

  const loadEligibleTeams = useCallback(async () => {
    try {
      setEligibleTeams(await getMyEligibleTeamsForTournament())
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar seus times.'))
    }
  }, [])

  // Ranking e times elegíveis, todo mundo vê. Entradas pendentes, só depois de saber que é dono —
  // membro comum recebe 404 desse endpoint (mesmo critério de Times/Detalhe.tsx com
  // categorias/submissões).
  useEffect(() => {
    if (!details) return
    void loadRanking()
    void loadEligibleTeams()
    if (details.tournament.isOwner) {
      void loadPendingEntries()
    }
  }, [details, loadRanking, loadEligibleTeams, loadPendingEntries])

  // Caminho direto (um único time elegível — sem precisar escolher). O picker de múltiplos times
  // (TeamEntryPicker) cuida do próprio busy/sucesso/erro por linha; aqui é só o caso de 1 time.
  async function handleDirectRequestEntry(teamId: string) {
    if (!id) return
    setDirectEntryBusy(true)
    setDirectEntryError(null)
    try {
      await requestTeamEntry(id, teamId)
      setDirectEntrySent(true)
    } catch (err) {
      setDirectEntryError(getApiErrorMessage(err, {}, 'Não foi possível solicitar a entrada.'))
    } finally {
      setDirectEntryBusy(false)
    }
  }

  async function handleApproveEntry(entryId: string) {
    if (!id) return
    setEntryBusyId(entryId)
    setError(null)
    try {
      await approveEntry(id, entryId)
      await Promise.all([loadPendingEntries(), loadRanking()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aprovar a entrada.'))
    } finally {
      setEntryBusyId(null)
    }
  }

  async function handleRejectEntry(entryId: string) {
    if (!id) return
    setEntryBusyId(entryId)
    setError(null)
    try {
      await rejectEntry(id, entryId)
      await loadPendingEntries()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar a entrada.'))
    } finally {
      setEntryBusyId(null)
    }
  }

  const inviteUrl = details ? `${window.location.origin}${details.invitePath}` : null

  async function handleCopy() {
    if (!inviteUrl) return
    try {
      await navigator.clipboard.writeText(inviteUrl)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setError('Não foi possível copiar o link.')
    }
  }

  async function handleBannerThemeChange(theme: TeamBannerTheme) {
    if (!id) return
    setError(null)
    setBannerBusy(true)
    try {
      await setTournamentBannerTheme(id, theme)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível alterar a cor do banner.'))
    } finally {
      setBannerBusy(false)
    }
  }

  function handleBannerFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || !id) return

    if (file.size > MAX_BANNER_BYTES) {
      setBannerFileError('A imagem deve ter até 3MB.')
      return
    }

    setBannerFileError(null)
    setError(null)
    setBannerBusy(true)
    void uploadTournamentBannerImage(id, file)
      .then(() => loadDetails())
      .catch((err) => setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a imagem.')))
      .finally(() => setBannerBusy(false))
  }

  async function handleRemoveBannerImage() {
    if (!id) return
    setError(null)
    setBannerBusy(true)
    try {
      await removeTournamentBannerImage(id)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover a imagem.'))
    } finally {
      setBannerBusy(false)
    }
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-5">
        <Skeleton className="h-10 w-40" />
        <Skeleton className="h-20" />
        <Skeleton className="h-40" />
      </div>
    )
  }

  if (!details) {
    return (
      <div className="flex flex-col gap-5">
        <Link
          to="/torneios"
          aria-label="Voltar para Torneios"
          className="-ml-2 inline-flex w-fit rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <Trophy size={22} />
        </Link>
        <EmptyState
          icon={Trophy}
          title="Torneio não encontrado."
          description={error ?? 'Esse torneio não existe.'}
        />
      </div>
    )
  }

  const { tournament } = details

  return (
    <div className="flex flex-col gap-5">
      <TeamBanner
        theme={tournament.bannerTheme}
        imageUrl={tournament.bannerImageUrl}
        className="w-full rounded-md ring-1 ring-line"
      />

      <header className="flex items-center gap-2">
        <Link
          to="/torneios"
          aria-label="Voltar para Torneios"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <Trophy size={20} />
        </Link>
        <h1 className="glow-ink flex min-w-0 items-center gap-2 font-display text-3xl font-semibold tracking-tight text-ink">
          <span className="truncate">{tournament.name}</span>
          {tournament.isOwner && <Crown size={20} className="shrink-0 text-brand-green" aria-hidden="true" />}
        </h1>
      </header>

      {tournament.description && <p className="text-sm text-slate-400">{tournament.description}</p>}

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* LINK DE CONVITE — compartilhe com o dono de um time pra ele solicitar entrada. */}
      <section className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
        <div className="flex items-center gap-2 text-sm font-medium text-ink">
          <Link2 size={16} className="text-brand-green" aria-hidden="true" />
          Link de convite
        </div>
        <p className="text-xs text-slate-400">
          Quem abrir esse link pode solicitar a entrada de um time seu neste torneio — a entrada só
          é confirmada depois que o dono do torneio aprovar.
        </p>
        <div className="flex gap-2">
          <input
            type="text"
            readOnly
            value={inviteUrl ?? ''}
            aria-label="Link de convite do torneio"
            onFocus={(event) => event.target.select()}
            className="min-w-0 flex-1 rounded-md bg-surface-hi px-3 py-2 text-xs text-slate-300 ring-1 ring-line outline-none"
          />
          <button
            type="button"
            onClick={handleCopy}
            className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95"
          >
            {copied ? <Check size={15} /> : <Copy size={15} />}
            {copied ? 'Copiado' : 'Copiar'}
          </button>
        </div>
      </section>

      {/* SOLICITAR ENTRADA — visível a qualquer usuário, mesmo quem não é dono do torneio, desde
          que tenha pelo menos um time próprio sem entrada ativa em outro torneio agora. */}
      <section className="flex flex-col gap-2">
        {eligibleTeams === null ? (
          <Skeleton className="h-12" />
        ) : eligibleTeams.length === 0 ? (
          <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
            Nenhum time seu disponível para solicitar entrada — crie um time, ou aguarde um que já
            está em outro torneio ficar livre.
          </p>
        ) : eligibleTeams.length === 1 ? (
          <div className="flex flex-col gap-2">
            {directEntrySent ? (
              <p className="flex items-center gap-1.5 rounded-md bg-surface px-4 py-3 text-sm font-medium text-brand-green ring-1 ring-line">
                <Check size={15} aria-hidden="true" />
                Entrada solicitada para {eligibleTeams[0].name}.
              </p>
            ) : (
              <button
                type="button"
                disabled={directEntryBusy}
                onClick={() => handleDirectRequestEntry(eligibleTeams[0].id)}
                className="inline-flex items-center justify-center gap-1.5 rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
              >
                <UserPlus size={16} aria-hidden="true" />
                {directEntryBusy ? 'Solicitando…' : `Solicitar entrada com ${eligibleTeams[0].name}`}
              </button>
            )}
            {directEntryError && (
              <p role="alert" className="text-xs text-red-300">
                {directEntryError}
              </p>
            )}
          </div>
        ) : showEntryPicker ? (
          <div className="flex flex-col gap-2">
            <h2 className="text-sm font-medium text-slate-300">Escolha um time seu</h2>
            <TeamEntryPicker teams={eligibleTeams} onRequest={(teamId) => requestTeamEntry(id!, teamId)} />
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setShowEntryPicker(true)}
            className="inline-flex items-center justify-center gap-1.5 rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95"
          >
            <UserPlus size={16} aria-hidden="true" />
            Solicitar entrada
          </button>
        )}
      </section>

      {/* BANNER — só o dono altera */}
      {tournament.isOwner && (
        <section className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
          <span className="text-sm font-medium text-ink">Banner</span>

          <div className="flex flex-wrap items-center gap-2">
            {TEAM_BANNER_THEMES.map((theme) => (
              <button
                key={theme}
                type="button"
                aria-label={theme}
                aria-pressed={tournament.bannerTheme === theme}
                disabled={bannerBusy}
                onClick={() => handleBannerThemeChange(theme)}
                className={[
                  'flex size-9 shrink-0 items-center justify-center rounded-full ring-2 transition disabled:opacity-50',
                  TEAM_BANNER_SWATCH[theme],
                  tournament.bannerTheme === theme ? 'ring-ink' : 'ring-transparent',
                ].join(' ')}
              >
                {tournament.bannerTheme === theme && <Check size={16} className="text-brand-dark" />}
              </button>
            ))}

            <span className="mx-1 h-9 w-px bg-line" aria-hidden="true" />

            <button
              type="button"
              disabled={bannerBusy}
              onClick={() => fileInputRef.current?.click()}
              className="inline-flex shrink-0 items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-medium text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
            >
              <ImagePlus size={15} aria-hidden="true" />
              {bannerBusy ? 'Enviando…' : 'Enviar imagem'}
            </button>

            {tournament.bannerImageUrl && (
              <button
                type="button"
                disabled={bannerBusy}
                onClick={handleRemoveBannerImage}
                className="inline-flex shrink-0 items-center gap-1.5 rounded-xl p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                aria-label="Remover imagem do banner"
              >
                <X size={15} />
              </button>
            )}

            <input
              ref={fileInputRef}
              type="file"
              accept={ACCEPTED_BANNER_TYPES}
              onChange={handleBannerFileChange}
              className="hidden"
            />
          </div>

          <p className="text-xs text-slate-400">
            {tournament.bannerImageUrl
              ? 'A imagem personalizada tem prioridade sobre a cor — remova a imagem para usar a cor escolhida.'
              : 'Opcional — JPG, PNG ou WEBP, até 3MB.'}
          </p>
          {bannerFileError && (
            <p role="alert" className="text-xs text-red-300">
              {bannerFileError}
            </p>
          )}
        </section>
      )}

      {/* ENTRADAS PENDENTES — só o dono aprova/recusa */}
      {tournament.isOwner && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-medium text-slate-300">Entradas pendentes</h2>
          {pendingEntries === null ? (
            <Skeleton className="h-16" />
          ) : pendingEntries.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
              Nenhum time aguardando aprovação.
            </p>
          ) : (
            <ul className={listClasses}>
              {pendingEntries.map((entry) => (
                <PendingEntryRow
                  key={entry.id}
                  entry={entry}
                  busy={entryBusyId === entry.id}
                  onApprove={() => handleApproveEntry(entry.id)}
                  onReject={() => handleRejectEntry(entry.id)}
                />
              ))}
            </ul>
          )}
        </section>
      )}

      {/* RANKING — visível a todo mundo */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <Medal size={15} className="text-brand-green" aria-hidden="true" />
          Ranking
        </h2>
        {ranking === null ? (
          <Skeleton className="h-16" />
        ) : ranking.length === 0 ? (
          <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
            Nenhum time aprovado neste torneio ainda.
          </p>
        ) : (
          <ul className={listClasses}>
            {ranking.map((entry, index) => (
              <RankingRow key={entry.id} entry={entry} position={index + 1} />
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

export default TorneioDetalhe
