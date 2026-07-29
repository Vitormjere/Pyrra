import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ChevronLeft, Check } from 'lucide-react'
import { requestTournament } from '../../services/tournamentService'
import { getApiErrorMessage } from '../../services/apiError'

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-sm font-medium text-slate-300'

export function SolicitarTorneio() {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Sem tela de detalhes pra navegar: a solicitação não cria o torneio na hora — só um admin
  // aprovando cria de fato. Fica aqui mesmo mostrando a confirmação, em vez de redirecionar.
  const [sent, setSent] = useState(false)

  const canSubmit = name.trim().length > 0

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      await requestTournament(name.trim(), description.trim() || null)
      setSent(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a solicitação.'))
      setSaving(false)
    }
  }

  if (sent) {
    return (
      <div className="flex flex-col gap-5">
        <header className="flex items-center gap-2">
          <Link
            to="/torneios"
            aria-label="Voltar para Torneios"
            className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
          >
            <ChevronLeft size={22} />
          </Link>
          <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
            Solicitar Torneio
          </h1>
        </header>

        <div className="rounded-md bg-surface px-5 py-10 text-center ring-1 ring-line">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-brand-green/10 text-brand-green">
            <Check size={24} aria-hidden="true" />
          </span>
          <p className="mt-3 font-medium text-slate-200">Solicitação enviada.</p>
          <p className="mt-1.5 text-sm text-slate-400">
            Um admin vai revisar seu pedido. Assim que for aprovado, o torneio aparece na sua lista.
          </p>
        </div>

        <Link
          to="/torneios"
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 text-center font-semibold text-brand-dark transition hover:brightness-95"
        >
          Voltar para Torneios
        </Link>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        <Link
          to="/torneios"
          aria-label="Voltar para Torneios"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Solicitar Torneio
        </h1>
      </header>

      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 rounded-md bg-surface px-5 py-4 ring-1 ring-line"
      >
        <p className="text-xs text-slate-400">
          Sua solicitação passa por um admin antes do torneio ser criado — se aprovada, você vira o dono.
        </p>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="tournament-name" className={labelClasses}>
            Nome
          </label>
          <input
            id="tournament-name"
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={100}
            placeholder="Ex.: Copa Verão Pyrra"
            className={inputClasses}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="tournament-description" className={labelClasses}>
            Descrição <span className="font-normal text-slate-500">(opcional)</span>
          </label>
          <textarea
            id="tournament-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            maxLength={500}
            rows={3}
            placeholder="Sobre o que é esse torneio?"
            className={`${inputClasses} resize-none`}
          />
        </div>

        <button
          type="submit"
          disabled={!canSubmit || saving}
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? 'Enviando…' : 'Enviar solicitação'}
        </button>

        {error && (
          <p role="alert" className="text-center text-xs text-red-300">
            {error}
          </p>
        )}
      </form>
    </div>
  )
}

export default SolicitarTorneio
