import { useCallback, useEffect, useMemo, useState } from 'react'
import { Check, ClipboardList, History, X } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'
import Skeleton from '../../../components/Skeleton'
import { useConfirm } from '../../../hooks/useConfirm'
import {
  approveTournamentRequest,
  getAllTournamentRequests,
  rejectTournamentRequest,
} from '../../../services/tournamentService'
import { getApiErrorMessage } from '../../../services/apiError'
import type { Tournament, TournamentRequest } from '../../../types/tournaments'

const listClasses = 'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

/** "22/07/2026" — createdAt vem como DateTime ISO completo, não DateOnly. */
function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR')
}

function StatusBadge({ status }: { status: TournamentRequest['status'] }) {
  if (status === 'Aprovado') {
    return (
      <span className="rounded-full bg-brand-green/15 px-1.5 py-0.5 text-[10px] font-semibold tracking-wide text-brand-green ring-1 ring-brand-green/30">
        APROVADA
      </span>
    )
  }
  if (status === 'Recusado') {
    return (
      <span className="rounded-full bg-red-500/10 px-1.5 py-0.5 text-[10px] font-semibold tracking-wide text-red-300 ring-1 ring-red-500/20">
        RECUSADA
      </span>
    )
  }
  return null
}

// Linha de solicitação — compartilhada entre Pendentes (com ações) e Histórico (sem ações,
// onApprove/onReject ausentes).
function RequestRow({
  request,
  busy,
  onApprove,
  onReject,
}: {
  request: TournamentRequest
  busy?: boolean
  onApprove?: () => void
  onReject?: () => void
}) {
  return (
    <li className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center">
      <div className="min-w-0 flex-1">
        <p className="flex flex-wrap items-center gap-1.5 font-medium text-ink">
          {request.proposedName}
          <StatusBadge status={request.status} />
        </p>
        {request.proposedDescription && (
          <p className="truncate text-xs text-slate-400">{request.proposedDescription}</p>
        )}
        <p className="truncate text-xs text-slate-500">
          Solicitado por {request.requester.name}
          {request.requester.username && ` (@${request.requester.username})`} — {formatDateTime(request.createdAt)}
        </p>
      </div>
      {onApprove && onReject && (
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
            aria-label={`Recusar solicitação de ${request.requester.name}`}
            className="rounded-xl p-2 text-slate-400 ring-1 ring-line transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
          >
            <X size={15} />
          </button>
        </div>
      )}
    </li>
  )
}

// Fila de solicitações de torneio (Fase Admin-3) — aprova/recusa direto pela tela, sem API
// manual. Histórico simples logo abaixo, só pra não perder o controle do que já foi decidido.
export function AdminSolicitacoes() {
  const { confirm, dialog } = useConfirm()

  const [requests, setRequests] = useState<TournamentRequest[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [approvedFeedback, setApprovedFeedback] = useState<Tournament | null>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getAllTournamentRequests()
        if (!active) return
        setRequests(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as solicitações.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  const loadRequests = useCallback(async () => {
    try {
      setRequests(await getAllTournamentRequests())
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recarregar as solicitações.'))
    }
  }, [])

  async function handleApprove(request: TournamentRequest) {
    const ok = await confirm({
      title: 'Aprovar solicitação',
      message: `Aprovar "${request.proposedName}", solicitado por ${request.requester.name}? O torneio é criado na hora, já com essa pessoa como dono.`,
      confirmLabel: 'Aprovar',
    })
    if (!ok) return

    setBusyId(request.id)
    setError(null)
    setApprovedFeedback(null)
    try {
      const tournament = await approveTournamentRequest(request.id)
      setApprovedFeedback(tournament)
      await loadRequests()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível aprovar a solicitação.'))
    } finally {
      setBusyId(null)
    }
  }

  async function handleReject(request: TournamentRequest) {
    const ok = await confirm({
      title: 'Recusar solicitação',
      message: `Recusar "${request.proposedName}"? Essa ação não pode ser desfeita.`,
      confirmLabel: 'Recusar',
      destructive: true,
    })
    if (!ok) return

    setBusyId(request.id)
    setError(null)
    setApprovedFeedback(null)
    try {
      await rejectTournamentRequest(request.id)
      await loadRequests()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recusar a solicitação.'))
    } finally {
      setBusyId(null)
    }
  }

  const pending = useMemo(() => requests?.filter((r) => r.status === 'Pendente') ?? null, [requests])
  const history = useMemo(() => requests?.filter((r) => r.status !== 'Pendente') ?? null, [requests])

  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Solicitações
      </h1>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {approvedFeedback && (
        <p className="rounded-lg bg-brand-green/10 px-3 py-2 text-center text-sm text-brand-green ring-1 ring-brand-green/20">
          Torneio "{approvedFeedback.name}" criado — dono: {approvedFeedback.owner.name}
          {approvedFeedback.owner.username && ` (@${approvedFeedback.owner.username})`}.
        </p>
      )}

      {/* PENDENTES */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <ClipboardList size={15} className="text-brand-green" aria-hidden="true" />
          Pendentes
        </h2>

        {pending === null ? (
          <Skeleton className="h-16" />
        ) : pending.length === 0 ? (
          <EmptyState icon={ClipboardList} title="Nenhuma solicitação aguardando avaliação." />
        ) : (
          <ul className={listClasses}>
            {pending.map((request) => (
              <RequestRow
                key={request.id}
                request={request}
                busy={busyId === request.id}
                onApprove={() => handleApprove(request)}
                onReject={() => handleReject(request)}
              />
            ))}
          </ul>
        )}
      </section>

      {/* HISTÓRICO — só leitura, pra não perder o controle do que já foi decidido. */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <History size={15} className="text-brand-green" aria-hidden="true" />
          Histórico
        </h2>

        {history === null ? (
          <Skeleton className="h-16" />
        ) : history.length === 0 ? (
          <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
            Nenhuma solicitação avaliada ainda.
          </p>
        ) : (
          <ul className={listClasses}>
            {history.map((request) => (
              <RequestRow key={request.id} request={request} />
            ))}
          </ul>
        )}
      </section>

      {dialog}
    </div>
  )
}

export default AdminSolicitacoes
