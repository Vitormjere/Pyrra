import { useCallback, useEffect, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  Check,
  Copy,
  Crown,
  ImagePlus,
  Link2,
  ListChecks,
  Medal,
  Plus,
  Trash2,
  Trophy,
  UserPlus,
  X,
} from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import TeamEntryPicker from '../../components/TeamEntryPicker'
import { getSubmissionPhotoUrl } from '../../services/challengeService'
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
import {
  approveTournamentSubmission,
  createTournamentOwnChallenge,
  deleteTournamentOwnChallenge,
  getTournamentCatalog,
  getTournamentOwnChallenges,
  getTournamentPendingSubmissions,
  linkTournamentCatalogChallenge,
  rejectTournamentSubmission,
  unlinkTournamentCatalogChallenge,
} from '../../services/tournamentChallengeService'
import { getApiErrorMessage } from '../../services/apiError'
import { CHALLENGE_CATEGORY_SWATCH, CHALLENGE_CATEGORY_TEXT } from '../../utils/challengeCategoryColors'
import { getCategoryIcon } from '../../utils/challengeCategoryIcons'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { Team, TeamBannerTheme } from '../../types/teams'
import type { TournamentDetails, TournamentTeamEntry } from '../../types/tournaments'
import type {
  PendingTournamentSubmissionWithTeam,
  TournamentCatalogChallenge,
  TournamentOwnChallenge,
} from '../../types/tournamentChallenges'

const MAX_BANNER_BYTES = 3 * 1024 * 1024
const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

const listClasses =
  'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

// Agrupa o catálogo por categoria pra exibição em "Escolher do catálogo" — a API já devolve
// ordenado por nome da categoria e depois título, então só precisa juntar em blocos consecutivos.
function groupCatalogByCategory(catalog: TournamentCatalogChallenge[]) {
  const groups: { category: TournamentCatalogChallenge['category']; challenges: TournamentCatalogChallenge[] }[] = []

  for (const challenge of catalog) {
    const lastGroup = groups[groups.length - 1]
    if (lastGroup && lastGroup.category.id === challenge.category.id) {
      lastGroup.challenges.push(challenge)
    } else {
      groups.push({ category: challenge.category, challenges: [challenge] })
    }
  }

  return groups
}

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

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

// Checkbox de vínculo com o catálogo geral — sem ícone/cor de categoria por linha: a lista já
// vem agrupada por categoria (ver groupCatalogByCategory), o cabeçalho do grupo já mostra isso.
function CatalogChallengeRow({
  challenge,
  busy,
  onToggle,
}: {
  challenge: TournamentCatalogChallenge
  busy: boolean
  onToggle: () => void
}) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <button
        type="button"
        role="checkbox"
        aria-checked={challenge.isLinked}
        aria-label={`Vincular ${challenge.title} ao torneio`}
        disabled={busy}
        onClick={onToggle}
        className={[
          'flex size-5 shrink-0 items-center justify-center rounded transition disabled:opacity-50',
          challenge.isLinked ? 'bg-brand-green' : 'ring-1 ring-line hover:ring-slate-500',
        ].join(' ')}
      >
        {challenge.isLinked && <Check size={13} className="text-brand-dark" aria-hidden="true" />}
      </button>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{challenge.title}</p>
        {challenge.description && <p className="truncate text-xs text-slate-400">{challenge.description}</p>}
      </div>
      <span className="shrink-0 rounded-full bg-brand-green/10 px-2.5 py-1 text-xs font-semibold text-brand-green ring-1 ring-brand-green/20">
        +{challenge.points} pts
      </span>
    </li>
  )
}

// Desafio próprio do torneio — sem categoria (lista flat), só o dono remove.
function OwnChallengeRow({
  challenge,
  busy,
  onDelete,
}: {
  challenge: TournamentOwnChallenge
  busy: boolean
  onDelete: () => void
}) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{challenge.title}</p>
        {challenge.description && <p className="truncate text-xs text-slate-400">{challenge.description}</p>}
      </div>
      <span className="shrink-0 rounded-full bg-brand-green/10 px-2.5 py-1 text-xs font-semibold text-brand-green ring-1 ring-brand-green/20">
        +{challenge.points} pts
      </span>
      <button
        type="button"
        disabled={busy}
        onClick={onDelete}
        aria-label={`Remover ${challenge.title}`}
        className="shrink-0 rounded-xl p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
      >
        <Trash2 size={15} />
      </button>
    </li>
  )
}

// Submissão pendente de um desafio DE TORNEIO, de qualquer time participante — mostra de qual
// time veio (pode ser mais de um), diferente da fila de um time só em Times/Detalhe.tsx.
function PendingTournamentSubmissionRow({
  submission,
  busy,
  onApprove,
  onReject,
}: {
  submission: PendingTournamentSubmissionWithTeam
  busy: boolean
  onApprove: () => void
  onReject: () => void
}) {
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null

    void getSubmissionPhotoUrl(submission.teamId, submission.id).then((url) => {
      if (!active) {
        URL.revokeObjectURL(url)
        return
      }
      objectUrl = url
      setPhotoUrl(url)
    })

    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [submission.teamId, submission.id])

  return (
    <li className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center">
      {photoUrl ? (
        <img
          src={photoUrl}
          alt={`Prova enviada por ${submission.submitter.name} para ${submission.challengeTitle}`}
          className="h-32 w-full shrink-0 rounded-lg object-cover ring-1 ring-line sm:h-14 sm:w-14"
        />
      ) : (
        <div
          aria-hidden="true"
          className="h-32 w-full shrink-0 animate-pulse rounded-lg bg-surface-hi ring-1 ring-line sm:h-14 sm:w-14"
        />
      )}
      <div className="min-w-0 flex-1">
        <p className="font-medium text-ink">{submission.challengeTitle}</p>
        <p className="truncate text-xs text-slate-400">
          Enviado por {submission.submitter.name}
          {submission.submitter.username && ` (@${submission.submitter.username})`} — time{' '}
          <span className="font-medium text-slate-300">{submission.teamName}</span>
        </p>
        <p className="mt-1 text-xs font-semibold text-brand-green">+{submission.challengePoints} pts</p>
      </div>
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
          aria-label={`Recusar submissão de ${submission.submitter.name}`}
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

  // Desafios do torneio — catálogo geral com status de vínculo + desafios próprios (só o dono vê)
  const [catalog, setCatalog] = useState<TournamentCatalogChallenge[] | null>(null)
  const [catalogBusyId, setCatalogBusyId] = useState<string | null>(null)
  const [ownChallenges, setOwnChallenges] = useState<TournamentOwnChallenge[] | null>(null)
  const [ownChallengeBusyId, setOwnChallengeBusyId] = useState<string | null>(null)

  // Formulário "Criar desafio"
  const [newTitle, setNewTitle] = useState('')
  const [newDescription, setNewDescription] = useState('')
  const [newPoints, setNewPoints] = useState('')
  const [createBusy, setCreateBusy] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  // Submissões pendentes de desafios DE TORNEIO, cruzando todos os times participantes (só o
  // dono vê) — separado das entradas pendentes acima, que são sobre times entrando no torneio.
  const [tournamentPending, setTournamentPending] = useState<PendingTournamentSubmissionWithTeam[] | null>(null)
  const [tournamentPendingBusyId, setTournamentPendingBusyId] = useState<string | null>(null)

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

  const loadCatalog = useCallback(async () => {
    if (!id) return
    try {
      setCatalog(await getTournamentCatalog(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o catálogo de desafios.'))
    }
  }, [id])

  const loadOwnChallenges = useCallback(async () => {
    if (!id) return
    try {
      setOwnChallenges(await getTournamentOwnChallenges(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os desafios próprios.'))
    }
  }, [id])

  const loadTournamentPending = useCallback(async () => {
    if (!id) return
    try {
      setTournamentPending(await getTournamentPendingSubmissions(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as submissões pendentes.'))
    }
  }, [id])

  // Ranking e times elegíveis, todo mundo vê. O resto (entradas pendentes, desafios do torneio,
  // submissões pendentes), só depois de saber que é dono — membro comum recebe 404 desses
  // endpoints (mesmo critério de Times/Detalhe.tsx com categorias/submissões).
  useEffect(() => {
    if (!details) return
    void loadRanking()
    void loadEligibleTeams()
    if (details.tournament.isOwner) {
      void loadPendingEntries()
      void loadCatalog()
      void loadOwnChallenges()
      void loadTournamentPending()
    }
  }, [
    details,
    loadRanking,
    loadEligibleTeams,
    loadPendingEntries,
    loadCatalog,
    loadOwnChallenges,
    loadTournamentPending,
  ])

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

  async function handleToggleCatalogLink(challenge: TournamentCatalogChallenge) {
    if (!id) return
    setCatalogBusyId(challenge.id)
    setError(null)
    try {
      if (challenge.isLinked) {
        await unlinkTournamentCatalogChallenge(id, challenge.id)
      } else {
        await linkTournamentCatalogChallenge(id, challenge.id)
      }
      await loadCatalog()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível atualizar o vínculo com o catálogo.'))
    } finally {
      setCatalogBusyId(null)
    }
  }

  async function handleCreateOwnChallenge(event: FormEvent) {
    event.preventDefault()
    if (!id) return

    const points = Number(newPoints)
    if (!newTitle.trim()) {
      setCreateError('O título do desafio é obrigatório.')
      return
    }
    if (!Number.isInteger(points) || points <= 0) {
      setCreateError('A pontuação precisa ser um número inteiro maior que zero.')
      return
    }

    setCreateBusy(true)
    setCreateError(null)
    try {
      await createTournamentOwnChallenge(id, newTitle.trim(), newDescription.trim() || null, points)
      setNewTitle('')
      setNewDescription('')
      setNewPoints('')
      await loadOwnChallenges()
    } catch (err) {
      setCreateError(getApiErrorMessage(err, {}, 'Não foi possível criar o desafio.'))
    } finally {
      setCreateBusy(false)
    }
  }

  async function handleDeleteOwnChallenge(challengeId: string) {
    if (!id) return
    setOwnChallengeBusyId(challengeId)
    setError(null)
    try {
      await deleteTournamentOwnChallenge(id, challengeId)
      await loadOwnChallenges()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover o desafio.'))
    } finally {
      setOwnChallengeBusyId(null)
    }
  }

  async function handleApproveTournamentSubmission(submission: PendingTournamentSubmissionWithTeam) {
    if (!id) return
    setTournamentPendingBusyId(submission.id)
    setError(null)
    try {
      await approveTournamentSubmission(submission.teamId, id, submission.id)
      await Promise.all([loadTournamentPending(), loadRanking()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aprovar a submissão.'))
    } finally {
      setTournamentPendingBusyId(null)
    }
  }

  async function handleRejectTournamentSubmission(submission: PendingTournamentSubmissionWithTeam) {
    if (!id) return
    setTournamentPendingBusyId(submission.id)
    setError(null)
    try {
      await rejectTournamentSubmission(submission.teamId, id, submission.id)
      await loadTournamentPending()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar a submissão.'))
    } finally {
      setTournamentPendingBusyId(null)
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

      {/* DESAFIOS DO TORNEIO — só o dono gerencia: escolher do catálogo geral ou criar um
          desafio próprio, exclusivo deste torneio (Fase 5b). */}
      {tournament.isOwner && (
        <section className="flex flex-col gap-4">
          <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
            <ListChecks size={15} className="text-brand-green" aria-hidden="true" />
            Desafios do torneio
          </h2>

          {/* Escolher do catálogo */}
          <div className="flex flex-col gap-2">
            <h3 className={labelClasses}>Escolher do catálogo</h3>
            {catalog === null ? (
              <Skeleton className="h-16" />
            ) : catalog.length === 0 ? (
              <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
                Nenhum desafio no catálogo geral ainda.
              </p>
            ) : (
              <div className="flex flex-col gap-3">
                {groupCatalogByCategory(catalog).map((group) => {
                  const CategoryIcon = getCategoryIcon(group.category.icon)
                  return (
                    <div key={group.category.id} className="flex flex-col gap-1.5">
                      <div className="flex items-center gap-1.5 px-1">
                        <span
                          aria-hidden="true"
                          className={[
                            'flex size-5 shrink-0 items-center justify-center rounded-full text-brand-dark',
                            CHALLENGE_CATEGORY_SWATCH[group.category.color],
                          ].join(' ')}
                        >
                          <CategoryIcon size={11} />
                        </span>
                        <h4 className={['text-xs font-semibold', CHALLENGE_CATEGORY_TEXT[group.category.color]].join(' ')}>
                          {group.category.name}
                        </h4>
                      </div>
                      <ul className={listClasses}>
                        {group.challenges.map((challenge) => (
                          <CatalogChallengeRow
                            key={challenge.id}
                            challenge={challenge}
                            busy={catalogBusyId === challenge.id}
                            onToggle={() => handleToggleCatalogLink(challenge)}
                          />
                        ))}
                      </ul>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Criar desafio */}
          <div className="flex flex-col gap-2">
            <h3 className={labelClasses}>Criar desafio</h3>
            <form onSubmit={handleCreateOwnChallenge} className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
              <div className="flex flex-col gap-1">
                <label htmlFor="own-challenge-title" className={labelClasses}>
                  Título
                </label>
                <input
                  id="own-challenge-title"
                  type="text"
                  value={newTitle}
                  onChange={(event) => setNewTitle(event.target.value)}
                  placeholder="Ex.: Correr 10km no torneio"
                  className={inputClasses}
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="own-challenge-description" className={labelClasses}>
                  Descrição <span className="font-normal text-slate-500">(opcional)</span>
                </label>
                <textarea
                  id="own-challenge-description"
                  value={newDescription}
                  onChange={(event) => setNewDescription(event.target.value)}
                  rows={2}
                  className={`${inputClasses} resize-none`}
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="own-challenge-points" className={labelClasses}>
                  Pontos
                </label>
                <input
                  id="own-challenge-points"
                  type="number"
                  inputMode="numeric"
                  min={1}
                  value={newPoints}
                  onChange={(event) => setNewPoints(event.target.value)}
                  placeholder="Ex.: 20"
                  className={`${inputClasses} max-w-32`}
                />
              </div>
              {createError && (
                <p role="alert" className="text-xs text-red-300">
                  {createError}
                </p>
              )}
              <button
                type="submit"
                disabled={createBusy}
                className="mt-1 inline-flex items-center justify-center gap-1.5 self-start rounded-xl bg-brand-green px-4 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
              >
                <Plus size={15} aria-hidden="true" />
                {createBusy ? 'Criando…' : 'Criar desafio'}
              </button>
            </form>

            {ownChallenges === null ? (
              <Skeleton className="h-16" />
            ) : ownChallenges.length > 0 ? (
              <ul className={listClasses}>
                {ownChallenges.map((challenge) => (
                  <OwnChallengeRow
                    key={challenge.id}
                    challenge={challenge}
                    busy={ownChallengeBusyId === challenge.id}
                    onDelete={() => handleDeleteOwnChallenge(challenge.id)}
                  />
                ))}
              </ul>
            ) : null}
          </div>
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

      {/* SUBMISSÕES PENDENTES DE DESAFIOS DE TORNEIO — cruza todos os times participantes, só o
          dono do torneio aprova/recusa (Fase 5b). Separado das entradas acima, que são sobre
          times pedindo pra entrar no torneio, não sobre desafios. */}
      {tournament.isOwner && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-medium text-slate-300">Submissões pendentes</h2>
          {tournamentPending === null ? (
            <Skeleton className="h-16" />
          ) : tournamentPending.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
              Nenhuma submissão aguardando aprovação.
            </p>
          ) : (
            <ul className={listClasses}>
              {tournamentPending.map((submission) => (
                <PendingTournamentSubmissionRow
                  key={submission.id}
                  submission={submission}
                  busy={tournamentPendingBusyId === submission.id}
                  onApprove={() => handleApproveTournamentSubmission(submission)}
                  onReject={() => handleRejectTournamentSubmission(submission)}
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
