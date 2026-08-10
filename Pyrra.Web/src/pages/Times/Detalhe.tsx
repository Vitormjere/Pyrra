import { useCallback, useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  Award,
  ChevronLeft,
  Check,
  Copy,
  Crown,
  ImagePlus,
  Link2,
  Medal,
  Trash2,
  Trophy,
  UserMinus,
  UserPlus,
  Users,
  X,
} from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import ProgressBar from '../../components/ProgressBar'
import Segmented from '../../components/Segmented'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import { useAuth } from '../../hooks/useAuth'
import { useConfirm } from '../../hooks/useConfirm'
import { getFriends } from '../../services/friendService'
import {
  activateTeamCategory,
  approveSubmission,
  deactivateTeamCategory,
  getAvailableChallenges,
  getPendingSubmissions,
  getSubmissionPhotoUrl,
  getTeamCategories,
  getTeamRanking,
  rejectSubmission,
  submitChallengeProof,
} from '../../services/challengeService'
import {
  getAvailableTournamentChallenges,
  submitTournamentCatalogChallengeProof,
  submitTournamentOwnChallengeProof,
} from '../../services/tournamentChallengeService'
import {
  deleteTeam,
  getTeamDetails,
  inviteFriendToTeam,
  leaveTeam,
  removeTeamBannerImage,
  removeTeamMember,
  setTeamBannerTheme,
  setTeamVisibility,
  transferTeamOwnership,
  uploadTeamBannerImage,
} from '../../services/teamService'
import { getApiErrorMessage } from '../../services/apiError'
import { CHALLENGE_CATEGORY_SWATCH, CHALLENGE_CATEGORY_TEXT } from '../../utils/challengeCategoryColors'
import { getCategoryIcon } from '../../utils/challengeCategoryIcons'
import { formatNumber } from '../../utils/format'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { Friend } from '../../types/community'
import type { AvailableChallenge, PendingSubmission, TeamCategoryStatus, TeamMemberRanking } from '../../types/challenges'
import type { AvailableTournamentChallenge } from '../../types/tournamentChallenges'
import type { TeamBannerTheme, TeamDetails, TeamMember, TeamVisibility } from '../../types/teams'

const VISIBILITY_OPTIONS: readonly TeamVisibility[] = ['Privado', 'Publico']

const VISIBILITY_LABELS: Record<TeamVisibility, string> = {
  Privado: 'Privado',
  Publico: 'Público',
}

const MAX_BANNER_BYTES = 3 * 1024 * 1024
const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

const MAX_SUBMISSION_BYTES = 3 * 1024 * 1024
const ACCEPTED_SUBMISSION_TYPES = 'image/jpeg,image/png,image/webp'

function Avatar({ name }: { name: string }) {
  return (
    <span
      aria-hidden="true"
      className="flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-hi text-sm font-semibold text-slate-300 ring-1 ring-line"
    >
      {name.charAt(0).toUpperCase()}
    </span>
  )
}

const listClasses =
  'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

function MemberRow({
  member,
  canRemove,
  busy,
  onRemove,
  canTransfer,
  onTransfer,
}: {
  member: TeamMember
  canRemove: boolean
  busy: boolean
  onRemove: () => void
  canTransfer: boolean
  onTransfer: () => void
}) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <Avatar name={member.user.name} />
      <div className="min-w-0 flex-1">
        <p className="flex items-center gap-1.5 truncate font-medium text-ink">
          {member.user.name}
          {member.isOwner && <Crown size={13} className="shrink-0 text-brand-green" aria-hidden="true" />}
        </p>
        {member.user.username && (
          <p className="truncate text-xs text-slate-500">@{member.user.username}</p>
        )}
      </div>
      {canTransfer && (
        <button
          type="button"
          disabled={busy}
          onClick={onTransfer}
          className="shrink-0 rounded-lg px-2.5 py-1.5 text-xs font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:opacity-50"
        >
          Tornar dono
        </button>
      )}
      {canRemove && (
        <button
          type="button"
          disabled={busy}
          onClick={onRemove}
          aria-label={`Remover ${member.user.name}`}
          className="shrink-0 rounded-lg p-2 text-slate-500 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
        >
          <UserMinus size={16} />
        </button>
      )}
    </li>
  )
}

// Linha do ranking individual do time: posição (coroa no #1), avatar, nome/@username e o placar
// pessoal dentro DESSE time — mesmo estilo do ranking de Amigos, trocando o streak por pontos.
function TeamRankingRow({ entry, isSelf }: { entry: TeamMemberRanking; isSelf: boolean }) {
  const isTop = entry.position === 1

  return (
    <li
      className={[
        'flex items-center gap-3 px-4 py-3',
        isSelf ? 'bg-brand-green/10 ring-1 ring-inset ring-brand-green/30' : '',
      ].join(' ')}
    >
      <span
        aria-hidden="true"
        className={[
          'flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold tabular-nums',
          isTop ? 'bg-brand-green/15 text-brand-green' : 'text-slate-500',
        ].join(' ')}
      >
        {isTop ? <Medal size={14} /> : `#${entry.position}`}
      </span>
      <Avatar name={entry.user.name} />
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">
          {entry.user.name}
          {isSelf && <span className="ml-1.5 text-xs font-normal text-brand-green">(você)</span>}
        </p>
        {entry.user.username && (
          <p className="truncate text-xs text-slate-500">@{entry.user.username}</p>
        )}
      </div>
      <span className="shrink-0 text-sm font-semibold text-ink tabular-nums">{entry.points} pts</span>
    </li>
  )
}

function CategoryRow({
  category,
  busy,
  onToggle,
}: {
  category: TeamCategoryStatus
  busy: boolean
  onToggle: () => void
}) {
  const Icon = getCategoryIcon(category.icon)
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span
        aria-hidden="true"
        className={[
          'flex size-9 shrink-0 items-center justify-center rounded-full text-brand-dark',
          CHALLENGE_CATEGORY_SWATCH[category.color],
        ].join(' ')}
      >
        <Icon size={16} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{category.name}</p>
        {category.description && (
          <p className="truncate text-xs text-slate-500">{category.description}</p>
        )}
      </div>
      <button
        type="button"
        disabled={busy}
        onClick={onToggle}
        aria-pressed={category.isActive}
        className={[
          'shrink-0 rounded-xl px-3 py-1.5 text-xs font-semibold transition disabled:opacity-50',
          category.isActive
            ? 'text-slate-400 ring-1 ring-line hover:bg-surface-hi hover:text-ink'
            : 'bg-brand-green text-brand-dark hover:brightness-95',
        ].join(' ')}
      >
        {category.isActive ? 'Desativar' : 'Ativar'}
      </button>
    </li>
  )
}

function ChallengeCard({
  challenge,
  busy,
  onSubmitProof,
}: {
  challenge: AvailableChallenge
  busy: boolean
  onSubmitProof: (file: File) => void
}) {
  const Icon = getCategoryIcon(challenge.category.icon)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [fileError, setFileError] = useState<string | null>(null)

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = '' // permite escolher o mesmo arquivo de novo depois de um erro
    if (!file) return

    if (file.size > MAX_SUBMISSION_BYTES) {
      setFileError('A imagem deve ter até 3MB.')
      return
    }

    setFileError(null)
    onSubmitProof(file)
  }

  return (
    <li className="flex items-start gap-3 px-4 py-3">
      <span
        aria-hidden="true"
        className={[
          'mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full text-brand-dark',
          CHALLENGE_CATEGORY_SWATCH[challenge.category.color],
        ].join(' ')}
      >
        <Icon size={16} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="font-medium text-ink">{challenge.title}</p>
        {challenge.description && <p className="text-xs text-slate-400">{challenge.description}</p>}
        <p className={['mt-1 text-xs font-medium', CHALLENGE_CATEGORY_TEXT[challenge.category.color]].join(' ')}>
          {challenge.category.name}
        </p>
        {fileError && <p className="mt-1 text-xs text-red-300">{fileError}</p>}
      </div>
      <div className="flex shrink-0 flex-col items-end gap-2">
        <span className="rounded-full bg-brand-green/10 px-2.5 py-1 text-xs font-semibold text-brand-green ring-1 ring-brand-green/20">
          +{challenge.points} pts
        </span>

        {challenge.mySubmissionStatus === 'Pendente' ? (
          <span className="rounded-lg px-2.5 py-1 text-xs font-medium text-slate-400 ring-1 ring-line">
            Aguardando aprovação
          </span>
        ) : challenge.mySubmissionStatus === 'Aprovado' ? (
          <span className="inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-xs font-medium text-brand-green">
            <Check size={13} aria-hidden="true" />
            Aprovado
          </span>
        ) : (
          <button
            type="button"
            disabled={busy}
            onClick={() => fileInputRef.current?.click()}
            className="inline-flex items-center gap-1 rounded-xl px-2.5 py-1.5 text-xs font-semibold text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
          >
            <ImagePlus size={13} aria-hidden="true" />
            {busy ? 'Enviando…' : challenge.mySubmissionStatus === 'Recusado' ? 'Enviar de novo' : 'Enviar foto'}
          </button>
        )}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept={ACCEPTED_SUBMISSION_TYPES}
        onChange={handleFileChange}
        className="hidden"
      />
    </li>
  )
}

// Desafio de UM torneio específico — mesmo layout de ChallengeCard, sem categoria (desafios de
// torneio não têm uma) e com um selo neutro no lugar do ícone/cor de categoria. Quando tem meta
// (Fase 5c), mostra a barra de progresso do TIME e exige a quantidade antes de enviar a foto.
function TournamentChallengeCard({
  challenge,
  busy,
  onSubmitProof,
}: {
  challenge: AvailableTournamentChallenge
  busy: boolean
  onSubmitProof: (file: File, quantity: number | null) => void
}) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const [quantityInput, setQuantityInput] = useState('')

  const hasGoal = challenge.goal !== null
  const progress = challenge.progress ?? 0
  const goalReached = hasGoal && progress >= challenge.goal!

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = '' // permite escolher o mesmo arquivo de novo depois de um erro
    if (!file) return

    if (file.size > MAX_SUBMISSION_BYTES) {
      setFileError('A imagem deve ter até 3MB.')
      return
    }

    if (hasGoal) {
      const quantity = Number(quantityInput.replace(',', '.'))
      if (!quantityInput.trim() || !Number.isFinite(quantity) || quantity <= 0) {
        setFileError(`Informe a quantidade (${challenge.unit}) da sua contribuição.`)
        return
      }
      setFileError(null)
      onSubmitProof(file, quantity)
      setQuantityInput('')
      return
    }

    setFileError(null)
    onSubmitProof(file, null)
  }

  return (
    <li className="flex items-start gap-3 px-4 py-3">
      <span
        aria-hidden="true"
        className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-brand-green/15 text-brand-green"
      >
        <Award size={16} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="font-medium text-ink">{challenge.title}</p>
        {challenge.description && <p className="text-xs text-slate-400">{challenge.description}</p>}
        <p className="mt-1 text-xs font-medium text-slate-500">
          {challenge.source === 'TorneioProprio' ? 'Desafio exclusivo do torneio' : 'Desafio do catálogo, vinculado ao torneio'}
        </p>

        {hasGoal && (
          <div className="mt-2 flex flex-col gap-1">
            <div className="flex items-center justify-between gap-2 text-xs">
              <span className="font-medium text-slate-300">
                {formatNumber(progress)} / {formatNumber(challenge.goal!)} {challenge.unit}
              </span>
              {goalReached && <span className="shrink-0 font-semibold text-brand-green">Meta atingida</span>}
            </div>
            <ProgressBar percent={(progress / challenge.goal!) * 100} />
          </div>
        )}

        {fileError && <p className="mt-1 text-xs text-red-300">{fileError}</p>}
      </div>
      <div className="flex shrink-0 flex-col items-end gap-2">
        <span className="rounded-full bg-brand-green/10 px-2.5 py-1 text-xs font-semibold text-brand-green ring-1 ring-brand-green/20">
          +{challenge.points} pts
        </span>

        {hasGoal ? (
          // Com meta, o desafio nunca fecha — aceita novos envios mesmo com uma contribuição
          // anterior Pendente ou já Aprovada (Fase 5c), então o status só vira um rótulo
          // informativo da ÚLTIMA contribuição, sem bloquear o formulário de envio.
          <div className="flex flex-col items-end gap-1.5">
            {challenge.mySubmissionStatus && (
              <span
                className={[
                  'text-[11px] font-medium',
                  challenge.mySubmissionStatus === 'Aprovado'
                    ? 'text-brand-green'
                    : challenge.mySubmissionStatus === 'Pendente'
                      ? 'text-slate-400'
                      : 'text-red-300',
                ].join(' ')}
              >
                Última contribuição:{' '}
                {challenge.mySubmissionStatus === 'Aprovado'
                  ? 'aprovada'
                  : challenge.mySubmissionStatus === 'Pendente'
                    ? 'aguardando aprovação'
                    : 'recusada'}
              </span>
            )}
            <div className="flex items-center gap-1.5">
              <input
                type="text"
                inputMode="decimal"
                value={quantityInput}
                onChange={(event) => setQuantityInput(event.target.value)}
                placeholder={challenge.unit ?? ''}
                aria-label={`Quantidade (${challenge.unit}) da contribuição para ${challenge.title}`}
                className="w-16 rounded-lg bg-surface-hi px-2 py-1.5 text-xs text-ink ring-1 ring-line outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
              />
              <button
                type="button"
                disabled={busy}
                onClick={() => fileInputRef.current?.click()}
                className="inline-flex items-center gap-1 rounded-xl px-2.5 py-1.5 text-xs font-semibold text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
              >
                <ImagePlus size={13} aria-hidden="true" />
                {busy ? 'Enviando…' : 'Enviar foto'}
              </button>
            </div>
          </div>
        ) : challenge.mySubmissionStatus === 'Pendente' ? (
          <span className="rounded-lg px-2.5 py-1 text-xs font-medium text-slate-400 ring-1 ring-line">
            Aguardando aprovação
          </span>
        ) : challenge.mySubmissionStatus === 'Aprovado' ? (
          <span className="inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-xs font-medium text-brand-green">
            <Check size={13} aria-hidden="true" />
            Aprovado
          </span>
        ) : (
          <button
            type="button"
            disabled={busy}
            onClick={() => fileInputRef.current?.click()}
            className="inline-flex items-center gap-1 rounded-xl px-2.5 py-1.5 text-xs font-semibold text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
          >
            <ImagePlus size={13} aria-hidden="true" />
            {busy ? 'Enviando…' : challenge.mySubmissionStatus === 'Recusado' ? 'Enviar de novo' : 'Enviar foto'}
          </button>
        )}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept={ACCEPTED_SUBMISSION_TYPES}
        onChange={handleFileChange}
        className="hidden"
      />
    </li>
  )
}

function PendingSubmissionRow({
  teamId,
  submission,
  busy,
  onApprove,
  onReject,
}: {
  teamId: string
  submission: PendingSubmission
  busy: boolean
  onApprove: () => void
  onReject: () => void
}) {
  // Container privado: sem URL pública, a foto é buscada autenticada e virou um object URL local
  // — precisa ser revogado ao trocar de submissão ou desmontar, senão vaza memória.
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null

    void getSubmissionPhotoUrl(teamId, submission.id).then((url) => {
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
  }, [teamId, submission.id])

  return (
    <li className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center">
      {photoUrl ? (
        <img
          src={photoUrl}
          alt={`Prova enviada por ${submission.submitter.name} para ${submission.challenge.title}`}
          className="h-32 w-full shrink-0 rounded-lg object-cover ring-1 ring-line sm:h-14 sm:w-14"
        />
      ) : (
        <div
          aria-hidden="true"
          className="h-32 w-full shrink-0 animate-pulse rounded-lg bg-surface-hi ring-1 ring-line sm:h-14 sm:w-14"
        />
      )}
      <div className="min-w-0 flex-1">
        <p className="font-medium text-ink">{submission.challenge.title}</p>
        <p className="truncate text-xs text-slate-400">
          Enviado por {submission.submitter.name}
          {submission.submitter.username && ` (@${submission.submitter.username})`}
        </p>
        <p className="mt-1 text-xs font-semibold text-brand-green">+{submission.challenge.points} pts</p>
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

export function TimeDetalhe() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { confirm, dialog } = useConfirm()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [details, setDetails] = useState<TeamDetails | null>(null)
  const [loading, setLoading] = useState(true)
  // Estado inicial (não efeito): erro de upload vindo da tela de Criar Time, se houver — o time
  // já existia quando o upload falhou, então a navegação aconteceu mesmo assim, e a mensagem
  // chega aqui via location.state pra dar uma chance de tentar de novo.
  const [error, setError] = useState<string | null>(
    () => (location.state as { bannerError?: string } | null)?.bannerError ?? null,
  )
  const [busyId, setBusyId] = useState<string | null>(null)

  const [copied, setCopied] = useState(false)

  // Banner: troca de cor/imagem
  const [bannerBusy, setBannerBusy] = useState(false)
  const [bannerFileError, setBannerFileError] = useState<string | null>(null)

  // Convidar amigo
  const [showInvitePicker, setShowInvitePicker] = useState(false)
  const [friends, setFriends] = useState<Friend[] | null>(null)
  const [invitedIds, setInvitedIds] = useState<string[]>([])

  // Categorias ativas — só carregado pro dono (única visão que existe pra essa seção)
  const [categories, setCategories] = useState<TeamCategoryStatus[] | null>(null)
  const [categoryBusyId, setCategoryBusyId] = useState<string | null>(null)

  // Desafios disponíveis — carregado pra qualquer membro
  const [challenges, setChallenges] = useState<AvailableChallenge[] | null>(null)
  const [submitBusyId, setSubmitBusyId] = useState<string | null>(null)

  // Submissões pendentes — só carregado pro dono (única visão que existe pra essa seção)
  const [pendingSubmissions, setPendingSubmissions] = useState<PendingSubmission[] | null>(null)
  const [pendingBusyId, setPendingBusyId] = useState<string | null>(null)

  // Ranking individual do time — carregado pra qualquer membro (dono ou não)
  const [ranking, setRanking] = useState<TeamMemberRanking[] | null>(null)

  // Desafios de cada torneio em que o time está Aprovado — separados dos desafios normais do
  // time acima (Fase 5b). Chaveado por tournamentId porque agora pode haver vários ao mesmo tempo.
  const [tournamentChallenges, setTournamentChallenges] = useState<Record<string, AvailableTournamentChallenge[] | null>>({})
  const [tournamentSubmitBusyKey, setTournamentSubmitBusyKey] = useState<string | null>(null)

  const loadDetails = useCallback(async () => {
    if (!id) return
    try {
      const data = await getTeamDetails(id)
      setDetails(data)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o time.'))
    }
  }, [id])

  useEffect(() => {
    let active = true

    async function run() {
      if (!id) return
      try {
        const data = await getTeamDetails(id)
        if (!active) return
        setDetails(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o time.'))
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [id])

  const loadCategories = useCallback(async () => {
    if (!id) return
    try {
      setCategories(await getTeamCategories(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as categorias.'))
    }
  }, [id])

  const loadChallenges = useCallback(async () => {
    if (!id) return
    try {
      setChallenges(await getAvailableChallenges(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os desafios.'))
    }
  }, [id])

  const loadPendingSubmissions = useCallback(async () => {
    if (!id) return
    try {
      setPendingSubmissions(await getPendingSubmissions(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as submissões pendentes.'))
    }
  }, [id])

  const loadRanking = useCallback(async () => {
    if (!id) return
    try {
      setRanking(await getTeamRanking(id))
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o ranking.'))
    }
  }, [id])

  // Dispara só depois que os detalhes chegam (precisa saber se é dono antes de pedir as
  // categorias/submissões — membro comum recebe 404 desses endpoints). Desafios disponíveis e
  // ranking, todo membro vê.
  useEffect(() => {
    if (!details) return
    void loadChallenges()
    void loadRanking()
    if (details.team.isOwner) {
      void loadCategories()
      void loadPendingSubmissions()
    }
  }, [details, loadChallenges, loadRanking, loadCategories, loadPendingSubmissions])

  const loadTournamentChallenges = useCallback(
    async (tournamentId: string) => {
      if (!id) return
      try {
        const data = await getAvailableTournamentChallenges(id, tournamentId)
        setTournamentChallenges((current) => ({ ...current, [tournamentId]: data }))
      } catch (err) {
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os desafios do torneio.'))
      }
    },
    [id],
  )

  // Um time pode estar Aprovado em vários torneios ao mesmo tempo (Fase 5b) — carrega os
  // desafios de cada um separadamente.
  useEffect(() => {
    if (!details) return
    for (const activeTournament of details.activeTournaments) {
      if (activeTournament.status === 'Aprovado') {
        void loadTournamentChallenges(activeTournament.tournamentId)
      }
    }
  }, [details, loadTournamentChallenges])

  async function handleSubmitProof(challengeId: string, file: File) {
    if (!id) return
    setSubmitBusyId(challengeId)
    setError(null)
    try {
      await submitChallengeProof(id, challengeId, file)
      await loadChallenges()
      // O dono também pode enviar prova pro próprio time (sem restrição de auto-aprovação) — se
      // for o caso, a fila de pendentes que ele já está vendo precisa refletir o envio na hora.
      if (details?.team.isOwner) {
        await loadPendingSubmissions()
      }
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a foto.'))
    } finally {
      setSubmitBusyId(null)
    }
  }

  async function handleSubmitTournamentProof(
    tournamentId: string,
    challenge: AvailableTournamentChallenge,
    file: File,
    quantity: number | null,
  ) {
    if (!id) return
    const busyKey = `${tournamentId}:${challenge.id}`
    setTournamentSubmitBusyKey(busyKey)
    setError(null)
    try {
      if (challenge.source === 'TorneioProprio') {
        await submitTournamentOwnChallengeProof(id, tournamentId, challenge.id, file, quantity)
      } else {
        await submitTournamentCatalogChallengeProof(id, tournamentId, challenge.id, file, quantity)
      }
      await loadTournamentChallenges(tournamentId)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a foto.'))
    } finally {
      setTournamentSubmitBusyKey(null)
    }
  }

  async function handleApproveSubmission(submissionId: string) {
    if (!id) return
    setPendingBusyId(submissionId)
    setError(null)
    try {
      await approveSubmission(id, submissionId)
      await Promise.all([loadPendingSubmissions(), loadChallenges(), loadRanking()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aprovar a submissão.'))
    } finally {
      setPendingBusyId(null)
    }
  }

  async function handleRejectSubmission(submissionId: string) {
    if (!id) return
    setPendingBusyId(submissionId)
    setError(null)
    try {
      await rejectSubmission(id, submissionId)
      await Promise.all([loadPendingSubmissions(), loadChallenges()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar a submissão.'))
    } finally {
      setPendingBusyId(null)
    }
  }

  async function handleToggleCategory(category: TeamCategoryStatus) {
    if (!id) return
    setCategoryBusyId(category.id)
    setError(null)
    try {
      if (category.isActive) {
        await deactivateTeamCategory(id, category.id)
      } else {
        await activateTeamCategory(id, category.id)
      }
      await Promise.all([loadCategories(), loadChallenges()])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível atualizar a categoria.'))
    } finally {
      setCategoryBusyId(null)
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

  async function openInvitePicker() {
    setShowInvitePicker(true)
    if (friends === null) {
      try {
        setFriends(await getFriends())
      } catch {
        setFriends([])
      }
    }
  }

  async function handleInvite(friendUserId: string) {
    if (!id) return
    setBusyId(friendUserId)
    setError(null)
    try {
      await inviteFriendToTeam(id, friendUserId)
      setInvitedIds((current) => [...current, friendUserId])
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível convidar esse amigo.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleRemoveMember(member: TeamMember) {
    if (!id) return
    const ok = await confirm({
      title: 'Remover membro',
      message: `Remover ${member.user.name} do time?`,
      confirmLabel: 'Remover',
      destructive: true,
    })
    if (!ok) return

    setBusyId(member.userId)
    setError(null)
    try {
      await removeTeamMember(id, member.userId)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover esse membro.'))
    } finally {
      setBusyId(null)
    }
  }

  // Alteração direta ao trocar, sem confirmação — reversível a qualquer momento, mesmo espírito
  // das preferências de Configurações.
  async function handleVisibilityChange(visibility: TeamVisibility) {
    if (!id) return
    setError(null)
    try {
      await setTeamVisibility(id, visibility)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível alterar a visibilidade.'))
    }
  }

  // Mesmo padrão sem-confirmação da Visibilidade. Continua funcionando com uma imagem já
  // definida — só não aparece até a imagem ser removida.
  async function handleBannerThemeChange(theme: TeamBannerTheme) {
    if (!id) return
    setError(null)
    setBannerBusy(true)
    try {
      await setTeamBannerTheme(id, theme)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível alterar a cor do banner.'))
    } finally {
      setBannerBusy(false)
    }
  }

  function handleBannerFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = '' // permite escolher o mesmo arquivo de novo depois de um erro
    if (!file || !id) return

    if (file.size > MAX_BANNER_BYTES) {
      setBannerFileError('A imagem deve ter até 3MB.')
      return
    }

    setBannerFileError(null)
    setError(null)
    setBannerBusy(true)
    void uploadTeamBannerImage(id, file)
      .then(() => loadDetails())
      .catch((err) => setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a imagem.')))
      .finally(() => setBannerBusy(false))
  }

  async function handleRemoveBannerImage() {
    if (!id) return
    setError(null)
    setBannerBusy(true)
    try {
      await removeTeamBannerImage(id)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover a imagem.'))
    } finally {
      setBannerBusy(false)
    }
  }

  async function handleTransfer(member: TeamMember) {
    if (!id) return
    const ok = await confirm({
      title: 'Transferir titularidade',
      message: `Transferir a titularidade do time para ${member.user.name}? Você continua no time como membro comum.`,
      confirmLabel: 'Transferir',
    })
    if (!ok) return

    setBusyId(member.userId)
    setError(null)
    try {
      await transferTeamOwnership(id, member.userId)
      await loadDetails()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível transferir a titularidade.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleLeave() {
    if (!id || !details) return
    const ok = await confirm({
      title: 'Sair do time',
      message: `Sair de ${details.team.name}?`,
      confirmLabel: 'Sair',
      destructive: true,
    })
    if (!ok) return

    setError(null)
    try {
      await leaveTeam(id)
      navigate('/times', { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível sair do time.'))
    }
  }

  async function handleDelete() {
    if (!id || !details) return
    const ok = await confirm({
      title: 'Excluir time',
      message: `Excluir ${details.team.name}? Essa ação remove o time para todos os membros e não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      destructive: true,
    })
    if (!ok) return

    setError(null)
    try {
      await deleteTeam(id)
      navigate('/times', { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível excluir o time.'))
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
          to="/times"
          aria-label="Voltar para Times"
          className="-ml-2 inline-flex w-fit rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <EmptyState
          icon={Users}
          title="Time não encontrado."
          description={error ?? 'Esse time não existe ou você não faz mais parte dele.'}
        />
      </div>
    )
  }

  const { team, members } = details
  const memberIds = new Set(members.map((m) => m.userId))
  const availableFriends = (friends ?? []).filter(
    (f) => !memberIds.has(f.user.id) && !invitedIds.includes(f.user.id),
  )

  return (
    <div className="flex flex-col gap-5">
      {/* Nome continua no header abaixo (não sobreposto): aqui há espaço de sobra na página, e o
          dono já mostra a coroa junto do nome — duplicar essa composição dentro do banner só
          pra economizar uma linha não compensa a complexidade. A proporção 4:3 e o fade contra
          corte agressivo (ver TeamBanner) já resolvem o problema relatado. */}
      <TeamBanner
        theme={team.bannerTheme}
        imageUrl={team.bannerImageUrl}
        className="w-full rounded-md ring-1 ring-line"
      />

      <header className="flex items-center gap-2">
        <Link
          to="/times"
          aria-label="Voltar para Times"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink flex min-w-0 items-center gap-2 font-display text-3xl font-semibold tracking-tight text-ink">
          <span className="truncate">{team.name}</span>
          {team.isOwner && <Crown size={20} className="shrink-0 text-brand-green" aria-hidden="true" />}
        </h1>
      </header>

      {team.description && <p className="text-sm text-slate-400">{team.description}</p>}

      {/* AVISOS DE TORNEIO — um time pode estar em vários agora (Fase 5b, até 5 simultâneos). A
          aprovação de desafios DAQUELE torneio migra pro dono dele enquanto a entrada estiver
          Pendente ou Aprovado (ver TeamChallengeService). */}
      {details.activeTournaments.map((activeTournament) => (
        <Link
          key={activeTournament.tournamentId}
          to={`/torneios/${activeTournament.tournamentId}`}
          className="flex items-center gap-2 rounded-md bg-brand-green/10 px-4 py-3 text-sm ring-1 ring-brand-green/20 transition hover:bg-brand-green/15"
        >
          <Trophy size={16} className="shrink-0 text-brand-green" aria-hidden="true" />
          <span className="min-w-0 flex-1 text-slate-200">
            {activeTournament.status === 'Aprovado' ? (
              <>
                Este time está no torneio <strong className="font-semibold text-ink">{activeTournament.tournamentName}</strong> — a
                aprovação de desafios desse torneio agora é responsabilidade do dono dele.
              </>
            ) : (
              <>
                Entrada solicitada no torneio{' '}
                <strong className="font-semibold text-ink">{activeTournament.tournamentName}</strong>, aguardando aprovação do dono.
              </>
            )}
          </span>
        </Link>
      ))}

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* LINK DE CONVITE */}
      <section className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
        <div className="flex items-center gap-2 text-sm font-medium text-ink">
          <Link2 size={16} className="text-brand-green" aria-hidden="true" />
          Link de convite
        </div>
        <p className="text-xs text-slate-400">
          Quem abrir esse link entra direto no time, respeitando o limite de membros ({team.memberCount}/{team.memberLimit}).
        </p>
        <div className="flex gap-2">
          <input
            type="text"
            readOnly
            value={inviteUrl ?? ''}
            aria-label="Link de convite do time"
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

      {/* VISIBILIDADE — só o dono altera */}
      {team.isOwner && (
        <section className="flex flex-col gap-1.5 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
          <span className="text-sm font-medium text-ink">Visibilidade</span>
          <Segmented
            label="Visibilidade do time"
            options={VISIBILITY_OPTIONS}
            value={team.visibility}
            onChange={handleVisibilityChange}
            labels={VISIBILITY_LABELS}
          />
          <p className="text-xs text-slate-400">
            {team.visibility === 'Publico'
              ? 'Aparece na aba Explorar — qualquer um entra direto.'
              : 'Só entra por convite direto ou link.'}
          </p>
        </section>
      )}

      {/* BANNER — só o dono altera */}
      {team.isOwner && (
        <section className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
          <span className="text-sm font-medium text-ink">Banner</span>

          <div className="flex flex-wrap items-center gap-2">
            {TEAM_BANNER_THEMES.map((theme) => (
              <button
                key={theme}
                type="button"
                aria-label={theme}
                aria-pressed={team.bannerTheme === theme}
                disabled={bannerBusy}
                onClick={() => handleBannerThemeChange(theme)}
                className={[
                  'flex size-9 shrink-0 items-center justify-center rounded-full ring-2 transition disabled:opacity-50',
                  TEAM_BANNER_SWATCH[theme],
                  team.bannerTheme === theme ? 'ring-ink' : 'ring-transparent',
                ].join(' ')}
              >
                {team.bannerTheme === theme && <Check size={16} className="text-brand-dark" />}
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

            {team.bannerImageUrl && (
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
            {team.bannerImageUrl
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

      {/* CATEGORIAS ATIVAS — só o dono ativa/desativa. Escondida de quem não é dono: o conteúdo
          (quais categorias estão ativas) já aparece implícito em "Desafios disponíveis", que todo
          membro vê logo abaixo — duplicar aqui sem nenhuma ação disponível não agregaria nada. */}
      {team.isOwner && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-medium text-slate-300">Categorias ativas</h2>
          {categories === null ? (
            <Skeleton className="h-16" />
          ) : categories.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
              Nenhuma categoria disponível no momento.
            </p>
          ) : (
            <ul className={listClasses}>
              {categories.map((category) => (
                <CategoryRow
                  key={category.id}
                  category={category}
                  busy={categoryBusyId === category.id}
                  onToggle={() => handleToggleCategory(category)}
                />
              ))}
            </ul>
          )}
        </section>
      )}

      {/* CONVIDAR AMIGO */}
      <section className="flex flex-col gap-2">
        {!showInvitePicker ? (
          <button
            type="button"
            onClick={openInvitePicker}
            className="inline-flex items-center justify-center gap-1.5 rounded-xl px-4 py-2.5 text-sm font-semibold text-ink ring-1 ring-line transition hover:bg-surface"
          >
            <UserPlus size={16} aria-hidden="true" />
            Convidar amigo
          </button>
        ) : (
          <div className="flex flex-col gap-2">
            <h2 className="text-sm font-medium text-slate-300">Convidar amigo</h2>
            {friends === null ? (
              <Skeleton className="h-16" />
            ) : availableFriends.length === 0 ? (
              <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
                Nenhum amigo disponível para convidar — ou já estão no time, ou você ainda não tem amigos.
              </p>
            ) : (
              <ul className={listClasses}>
                {availableFriends.map((friend) => (
                  <li key={friend.user.id} className="flex items-center gap-3 px-4 py-3">
                    <Avatar name={friend.user.name} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-medium text-ink">{friend.user.name}</p>
                    </div>
                    <button
                      type="button"
                      disabled={busyId === friend.user.id}
                      onClick={() => handleInvite(friend.user.id)}
                      className="inline-flex shrink-0 items-center gap-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
                    >
                      <UserPlus size={15} />
                      Convidar
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </section>

      {/* MEMBROS */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">
          Membros ({team.memberCount}/{team.memberLimit})
        </h2>
        <ul className={listClasses}>
          {members.map((member) => (
            <MemberRow
              key={member.userId}
              member={member}
              busy={busyId === member.userId}
              canRemove={team.isOwner && !member.isOwner}
              onRemove={() => handleRemoveMember(member)}
              canTransfer={team.isOwner && !member.isOwner}
              onTransfer={() => handleTransfer(member)}
            />
          ))}
        </ul>
      </section>

      {/* SUBMISSÕES PENDENTES — só o dono aprova/recusa */}
      {team.isOwner && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-medium text-slate-300">Submissões pendentes</h2>
          {pendingSubmissions === null ? (
            <Skeleton className="h-16" />
          ) : pendingSubmissions.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
              Nenhuma submissão aguardando aprovação.
            </p>
          ) : (
            <ul className={listClasses}>
              {pendingSubmissions.map((submission) => (
                <PendingSubmissionRow
                  key={submission.id}
                  teamId={id!}
                  submission={submission}
                  busy={pendingBusyId === submission.id}
                  onApprove={() => handleApproveSubmission(submission.id)}
                  onReject={() => handleRejectSubmission(submission.id)}
                />
              ))}
            </ul>
          )}
        </section>
      )}

      {/* DESAFIOS DISPONÍVEIS — visível a todo membro */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">Desafios disponíveis</h2>
        {challenges === null ? (
          <Skeleton className="h-16" />
        ) : challenges.length === 0 ? (
          <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
            {team.isOwner
              ? 'Nenhuma categoria ativa, ative uma categoria acima para liberar desafios pro time.'
              : 'Nenhum desafio disponível, o dono do time ainda não ativou nenhuma categoria.'}
          </p>
        ) : (
          <ul className={listClasses}>
            {challenges.map((challenge) => (
              <ChallengeCard
                key={challenge.id}
                challenge={challenge}
                busy={submitBusyId === challenge.id}
                onSubmitProof={(file) => handleSubmitProof(challenge.id, file)}
              />
            ))}
          </ul>
        )}
      </section>

      {/* DESAFIOS DE CADA TORNEIO — separados dos desafios normais do time acima, um bloco por
          torneio Aprovado (pode haver vários agora, Fase 5b). Pendente não entra aqui: só depois
          que a entrada é Aprovada os desafios daquele torneio ficam disponíveis. */}
      {details.activeTournaments
        .filter((activeTournament) => activeTournament.status === 'Aprovado')
        .map((activeTournament) => {
          const list = tournamentChallenges[activeTournament.tournamentId]
          return (
            <section key={activeTournament.tournamentId} className="flex flex-col gap-2">
              <h2 className="flex flex-wrap items-center gap-1.5 text-sm font-medium text-slate-300">
                <Trophy size={15} className="shrink-0 text-brand-green" aria-hidden="true" />
                <span>Desafios do torneio</span>
                <Link
                  to={`/torneios/${activeTournament.tournamentId}`}
                  className="font-semibold text-brand-green hover:underline"
                >
                  {activeTournament.tournamentName}
                </Link>
              </h2>
              {list === undefined || list === null ? (
                <Skeleton className="h-16" />
              ) : list.length === 0 ? (
                <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
                  Nenhum desafio disponível nesse torneio ainda.
                </p>
              ) : (
                <ul className={listClasses}>
                  {list.map((challenge) => (
                    <TournamentChallengeCard
                      key={challenge.id}
                      challenge={challenge}
                      busy={tournamentSubmitBusyKey === `${activeTournament.tournamentId}:${challenge.id}`}
                      onSubmitProof={(file, quantity) => handleSubmitTournamentProof(activeTournament.tournamentId, challenge, file, quantity)}
                    />
                  ))}
                </ul>
              )}
            </section>
          )
        })}

      {/* RANKING DO TIME — placar INDIVIDUAL dentro desse time, visível a todo membro (dono ou
          não). Não é o TotalPoints coletivo do time (que continua existindo, ver header/listagem
          de Times) — é quem mais contribuiu ganhando desafios aqui dentro. */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <Medal size={15} className="text-brand-green" aria-hidden="true" />
          Ranking do time
        </h2>
        {ranking === null ? (
          <Skeleton className="h-16" />
        ) : (
          <ul className={listClasses}>
            {ranking.map((entry) => (
              <TeamRankingRow key={entry.user.id} entry={entry} isSelf={entry.user.id === user?.id} />
            ))}
          </ul>
        )}
      </section>

      {/* AÇÕES */}
      {team.isOwner ? (
        <button
          type="button"
          onClick={handleDelete}
          className="inline-flex items-center justify-center gap-1.5 rounded-xl px-4 py-2.5 text-sm font-semibold text-red-400 ring-1 ring-red-500/20 transition hover:bg-red-500/10"
        >
          <Trash2 size={16} aria-hidden="true" />
          Excluir time
        </button>
      ) : (
        <button
          type="button"
          onClick={handleLeave}
          className="inline-flex items-center justify-center gap-1.5 rounded-xl px-4 py-2.5 text-sm font-semibold text-red-400 ring-1 ring-red-500/20 transition hover:bg-red-500/10"
        >
          Sair do time
        </button>
      )}

      {dialog}
    </div>
  )
}

export default TimeDetalhe
