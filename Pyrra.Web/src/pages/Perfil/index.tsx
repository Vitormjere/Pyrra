import { useState } from 'react'
import type { FormEvent } from 'react'
import { LogOut } from 'lucide-react'
import Segmented from '../../components/Segmented'
import SectionHeader from '../../components/SectionHeader'
import { useAuth } from '../../hooks/useAuth'
import { updatePreferences } from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'
import type { CommunicationTone } from '../../types/auth'

const TONES: readonly CommunicationTone[] = [
  'Direto',
  'Acolhedor',
  'Desafiador',
]

// O que cada tom significa na prática — sem isso a escolha é adivinhação.
const TONE_HINTS: Record<CommunicationTone, string> = {
  Direto: 'Objetivo, sem rodeios.',
  Acolhedor: 'Gentil e compreensivo.',
  Desafiador: 'Provocador, te cutuca a ir além.',
}

export function Perfil() {
  const { user, refreshUser, logout } = useAuth()

  // Inicializa do contexto. O usuário já vem carregado — o ProtectedRoute só
  // renderiza esta tela depois que a sessão foi verificada.
  const [tone, setTone] = useState<CommunicationTone>(
    user?.communicationTone ?? 'Direto',
  )
  const [notificationTime, setNotificationTime] = useState(
    user?.eveningNotificationTime ?? '21:00',
  )
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  if (!user) return null

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    setSaving(true)
    setError(null)
    setSaved(false)

    try {
      await updatePreferences(tone, notificationTime)
      // Sincroniza o contexto: sem isso, sair da tela e voltar mostraria os
      // valores antigos, já que o formulário inicializa a partir dele.
      await refreshUser()
      setSaved(true)
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível salvar suas preferências.'),
      )
    } finally {
      setSaving(false)
    }
  }

  function handleLogout() {
    // window.confirm é síncrono e bloqueia a UI, mas para uma ação isolada e
    // reversível (basta entrar de novo) não compensa manter um modal próprio.
    if (window.confirm('Deseja sair da sua conta?')) {
      logout()
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Perfil</h1>
      </header>

      {/* DADOS DA CONTA */}
      <section className="rounded-md bg-surface px-5 py-4 ring-1 ring-line">
        <p className="text-lg font-semibold text-ink">{user.name}</p>
        <p className="mt-0.5 text-sm text-slate-400">{user.email}</p>
        <span className="mt-3 inline-block rounded-full bg-brand-green/10 px-3 py-1 text-xs font-medium text-brand-green">
          Plano {user.plan}
        </span>
      </section>

      {/* PREFERÊNCIAS */}
      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 rounded-md bg-surface px-5 py-4 ring-1 ring-line"
      >
        <SectionHeader>Preferências</SectionHeader>

        <div className="flex flex-col gap-2">
          <p className="text-xs font-medium text-slate-400">
            Tom das mensagens
          </p>
          <Segmented
            label="Tom de comunicação"
            options={TONES}
            value={tone}
            onChange={(next) => {
              setTone(next)
              setSaved(false)
            }}
          />
          <p className="text-xs text-slate-500">{TONE_HINTS[tone]}</p>
        </div>

        <div className="flex flex-col gap-1.5">
          <label
            htmlFor="horario-notificacao"
            className="text-xs font-medium text-slate-400"
          >
            Horário da mensagem noturna
          </label>
          <input
            id="horario-notificacao"
            type="time"
            value={notificationTime}
            onChange={(event) => {
              setNotificationTime(event.target.value)
              setSaved(false)
            }}
            required
            className="w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none focus:ring-2 focus:ring-brand-green"
          />
        </div>

        <button
          type="submit"
          disabled={saving}
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? 'Salvando...' : 'Salvar preferências'}
        </button>

        {saved && (
          <p role="status" className="text-center text-sm text-brand-green">
            Preferências salvas.
          </p>
        )}

        {error && (
          <p
            role="alert"
            className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
          >
            {error}
          </p>
        )}
      </form>

      {/* SAIR — separado do resto e em vermelho: é a única ação destrutiva
          da tela e não deve ser confundida com o botão de salvar. */}
      <button
        type="button"
        onClick={handleLogout}
        className="flex min-h-12 w-full items-center justify-center gap-2 rounded-md text-sm font-medium text-red-400 ring-1 ring-red-400/20 transition hover:bg-red-500/10"
      >
        <LogOut size={18} aria-hidden="true" />
        Sair da conta
      </button>
    </div>
  )
}

export default Perfil
