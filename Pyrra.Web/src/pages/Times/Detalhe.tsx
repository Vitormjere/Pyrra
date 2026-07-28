import { useCallback, useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  ChevronLeft,
  Check,
  Copy,
  Crown,
  ImagePlus,
  Link2,
  Trash2,
  UserMinus,
  UserPlus,
  Users,
  X,
} from 'lucide-react'
import EmptyState from '../../components/EmptyState'
import Segmented from '../../components/Segmented'
import Skeleton from '../../components/Skeleton'
import TeamBanner from '../../components/TeamBanner'
import { useConfirm } from '../../hooks/useConfirm'
import { getFriends } from '../../services/friendService'
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
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { Friend } from '../../types/community'
import type { TeamBannerTheme, TeamDetails, TeamMember, TeamVisibility } from '../../types/teams'

const VISIBILITY_OPTIONS: readonly TeamVisibility[] = ['Privado', 'Publico']

const VISIBILITY_LABELS: Record<TeamVisibility, string> = {
  Privado: 'Privado',
  Publico: 'Público',
}

const MAX_BANNER_BYTES = 3 * 1024 * 1024
const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

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

export function TimeDetalhe() {
  const { id } = useParams<{ id: string }>()
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
