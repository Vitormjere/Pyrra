import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AtSign, Check, ChevronLeft, LogOut, X } from 'lucide-react'
import Segmented from '../../components/Segmented'
import SectionHeader from '../../components/SectionHeader'
import DeleteAccountDialog from '../../components/DeleteAccountDialog'
import { useAuth } from '../../hooks/useAuth'
import {
  changeEmail,
  changePassword,
  checkUsernameAvailability,
  deleteAccount,
  setUsername as setUsernameApi,
  updateName,
  updatePreferences,
  updateProfileVisibility,
  updateTimezone,
} from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'
import { TIMEZONE_OPTIONS } from '../../utils/timezones'
import type { CommunicationTone, ProfileVisibility } from '../../types/auth'

const TONES: readonly CommunicationTone[] = ['Direto', 'Acolhedor', 'Desafiador']

const VISIBILITY_OPTIONS: readonly ProfileVisibility[] = ['Publico', 'SomenteAmigos']

const VISIBILITY_LABELS: Record<ProfileVisibility, string> = {
  Publico: 'Público',
  SomenteAmigos: 'Somente amigos',
}

const VISIBILITY_HINTS: Record<ProfileVisibility, string> = {
  Publico: 'Qualquer usuário logado pode ver seu perfil.',
  SomenteAmigos: 'Só quem é seu amigo confirmado pode ver.',
}

const TONE_HINTS: Record<CommunicationTone, string> = {
  Direto: 'Objetivo, sem rodeios.',
  Acolhedor: 'Gentil e compreensivo.',
  Desafiador: 'Provocador, te cutuca a ir além.',
}

// Mesma regra do backend (UsernameService): 3–20, minúsculas, números e underscore.
const USERNAME_PATTERN = /^[a-z0-9_]{3,20}$/

function normalizeUsername(raw: string): string {
  return raw.trim().replace(/^@+/, '').toLowerCase()
}

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const cardClasses = 'flex flex-col gap-3 rounded-md bg-surface px-5 py-4 ring-1 ring-line'

const saveButtonClasses =
  'w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60'

type UsernameAvailability =
  | { status: 'idle' }
  | { status: 'checking' }
  | { status: 'available' }
  | { status: 'unavailable'; reason: string }

// Tela de Configurações: tudo que é edição/administração da conta, separado do Perfil (que é só
// leitura + identidade social). Cada seção é seu próprio form com seu próprio save — nada aqui
// depende de outra seção, então salvar uma não arrisca perder o que está sendo digitado nas outras.
export function Configuracoes() {
  const { user, refreshUser, applyUser, logout } = useAuth()
  const navigate = useNavigate()

  if (!user) return null

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        <Link
          to="/perfil"
          aria-label="Voltar ao Perfil"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Configurações
        </h1>
      </header>

      <NameSection name={user.name} onSaved={refreshUser} />
      <EmailSection email={user.email} onSaved={refreshUser} />
      <PasswordSection />
      <PreferencesSection
        tone={user.communicationTone}
        notificationTime={user.eveningNotificationTime}
        timezone={user.timezone}
        onSaved={refreshUser}
      />
      <UsernameSection
        currentUsername={user.username}
        onSaved={applyUser}
      />
      <PrivacySection
        visibility={user.profileVisibility}
        onSaved={refreshUser}
      />

      <button
        type="button"
        onClick={logout}
        className="flex min-h-12 w-full items-center justify-center gap-2 rounded-md text-sm font-medium text-slate-300 ring-1 ring-line transition hover:bg-surface"
      >
        <LogOut size={18} aria-hidden="true" />
        Sair da conta
      </button>

      <DangerZone onDeleted={() => { logout(); navigate('/login', { replace: true }) }} />
    </div>
  )
}

// --- Nome ---

function NameSection({ name, onSaved }: { name: string; onSaved: () => Promise<void> }) {
  const [value, setValue] = useState(name)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const changed = value.trim() !== name && value.trim().length > 0

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!changed || saving) return

    setSaving(true)
    setError(null)
    try {
      await updateName(value.trim())
      await onSaved()
      setSaved(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar seu nome.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Nome</SectionHeader>
      <input
        type="text"
        value={value}
        onChange={(event) => {
          setValue(event.target.value)
          setSaved(false)
        }}
        maxLength={100}
        aria-label="Nome"
        className={inputClasses}
      />
      <button type="submit" disabled={!changed || saving} className={saveButtonClasses}>
        {saving ? 'Salvando…' : 'Salvar nome'}
      </button>
      {saved && <p role="status" className="text-center text-xs text-brand-green">Nome atualizado.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- E-mail ---

function EmailSection({ email, onSaved }: { email: string; onSaved: () => Promise<void> }) {
  const [newEmail, setNewEmail] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const canSubmit = newEmail.trim().length > 0 && currentPassword.length > 0

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      await changeEmail(newEmail.trim(), currentPassword)
      await onSaved()
      setNewEmail('')
      setCurrentPassword('')
      setSaved(true)
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 409: 'Esse e-mail já está em uso.' },
          'Não foi possível trocar seu e-mail.',
        ),
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>E-mail</SectionHeader>
      <p className="text-sm text-slate-400">Atual: {email}</p>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="novo-email" className="text-xs font-medium text-slate-400">
          Novo e-mail
        </label>
        <input
          id="novo-email"
          type="email"
          value={newEmail}
          onChange={(event) => {
            setNewEmail(event.target.value)
            setSaved(false)
          }}
          autoComplete="email"
          placeholder="novo@exemplo.com"
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="senha-email" className="text-xs font-medium text-slate-400">
          Senha atual (para confirmar)
        </label>
        <input
          id="senha-email"
          type="password"
          value={currentPassword}
          onChange={(event) => {
            setCurrentPassword(event.target.value)
            setSaved(false)
            if (error) setError(null)
          }}
          autoComplete="current-password"
          className={inputClasses}
        />
      </div>

      <button type="submit" disabled={!canSubmit || saving} className={saveButtonClasses}>
        {saving ? 'Salvando…' : 'Trocar e-mail'}
      </button>
      {saved && <p role="status" className="text-center text-xs text-brand-green">E-mail atualizado.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Senha ---

function PasswordSection() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const newPasswordValid = newPassword.length >= 8
  // Confirmação é só client-side, mesmo critério do Cadastro: o backend não conhece esse campo.
  const confirmMatches = confirmPassword.length > 0 && confirmPassword === newPassword
  const canSubmit = currentPassword.length > 0 && newPasswordValid && confirmMatches

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      await changePassword(currentPassword, newPassword)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSaved(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível trocar sua senha.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Senha</SectionHeader>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="senha-atual" className="text-xs font-medium text-slate-400">
          Senha atual
        </label>
        <input
          id="senha-atual"
          type="password"
          value={currentPassword}
          onChange={(event) => {
            setCurrentPassword(event.target.value)
            setSaved(false)
            if (error) setError(null)
          }}
          autoComplete="current-password"
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="nova-senha" className="text-xs font-medium text-slate-400">
          Nova senha
        </label>
        <input
          id="nova-senha"
          type="password"
          value={newPassword}
          onChange={(event) => {
            setNewPassword(event.target.value)
            setSaved(false)
          }}
          autoComplete="new-password"
          className={inputClasses}
        />
        <p className="text-xs text-slate-500">Mínimo de 8 caracteres.</p>
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="confirmar-senha" className="text-xs font-medium text-slate-400">
          Confirmar nova senha
        </label>
        <input
          id="confirmar-senha"
          type="password"
          value={confirmPassword}
          onChange={(event) => {
            setConfirmPassword(event.target.value)
            setSaved(false)
          }}
          autoComplete="new-password"
          className={inputClasses}
        />
        {confirmPassword.length > 0 && !confirmMatches && (
          <p className="text-xs text-red-300">As senhas não coincidem.</p>
        )}
      </div>

      <button type="submit" disabled={!canSubmit || saving} className={saveButtonClasses}>
        {saving ? 'Salvando…' : 'Trocar senha'}
      </button>
      {saved && <p role="status" className="text-center text-xs text-brand-green">Senha atualizada.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Preferências (tom + horário + fuso) ---

function PreferencesSection({
  tone: initialTone,
  notificationTime: initialTime,
  timezone: initialTimezone,
  onSaved,
}: {
  tone: CommunicationTone
  notificationTime: string
  timezone: string
  onSaved: () => Promise<void>
}) {
  const [tone, setTone] = useState(initialTone)
  const [notificationTime, setNotificationTime] = useState(initialTime)
  const [timezone, setTimezone] = useState(initialTimezone)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      // Dois endpoints (o backend mantém tom/horário e fuso decoupled), uma única ação de salvar
      // na tela: o usuário vê isso como "minhas preferências", não como dois recursos distintos.
      await Promise.all([
        updatePreferences(tone, notificationTime),
        updateTimezone(timezone),
      ])
      await onSaved()
      setSaved(true)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar suas preferências.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Preferências</SectionHeader>

      <div className="flex flex-col gap-2">
        <p className="text-xs font-medium text-slate-400">Tom das mensagens</p>
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
        <label htmlFor="horario-notificacao" className="text-xs font-medium text-slate-400">
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
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="fuso-horario" className="text-xs font-medium text-slate-400">
          Fuso horário
        </label>
        <select
          id="fuso-horario"
          value={timezone}
          onChange={(event) => {
            setTimezone(event.target.value)
            setSaved(false)
          }}
          className={inputClasses}
        >
          {/* Se o fuso salvo não estiver na lista curada, mostra-o mesmo assim — trocar de select
              não pode apagar silenciosamente um valor válido que só não está no menu. */}
          {!TIMEZONE_OPTIONS.some((option) => option.value === timezone) && (
            <option value={timezone}>{timezone}</option>
          )}
          {TIMEZONE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <button type="submit" disabled={saving} className={saveButtonClasses}>
        {saving ? 'Salvando…' : 'Salvar preferências'}
      </button>
      {saved && <p role="status" className="text-center text-xs text-brand-green">Preferências salvas.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Username (movido do Perfil) ---

function UsernameSection({
  currentUsername,
  onSaved,
}: {
  currentUsername: string | null
  onSaved: (user: Awaited<ReturnType<typeof setUsernameApi>>) => void
}) {
  const [username, setUsername] = useState(currentUsername ?? '')
  const [availability, setAvailability] = useState<UsernameAvailability>({ status: 'idle' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const normalized = normalizeUsername(username)
  const formatValid = USERNAME_PATTERN.test(normalized)
  const changed = normalized !== currentUsername

  // Checagem de disponibilidade com debounce, só quando o formato é válido E mudou em relação ao
  // atual. Formato inválido/sem mudança não zera `availability` aqui (setState síncrono no corpo
  // do efeito, que a regra react-hooks/set-state-in-effect não permite); a renderização abaixo
  // ignora `availability` sempre que uma das duas condições falha.
  useEffect(() => {
    if (!formatValid || !changed) {
      return
    }

    let active = true
    const timer = setTimeout(async () => {
      setAvailability({ status: 'checking' })
      try {
        const result = await checkUsernameAvailability(normalized)
        if (!active) return
        setAvailability(
          result.available
            ? { status: 'available' }
            : { status: 'unavailable', reason: result.reason ?? 'Indisponível.' },
        )
      } catch {
        if (active) setAvailability({ status: 'idle' })
      }
    }, 400)

    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [normalized, formatValid, changed])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!formatValid || !changed || saving) return

    setSaving(true)
    setError(null)
    try {
      const updated = await setUsernameApi(normalized)
      onSaved(updated)
      setSaved(true)
    } catch (err) {
      setError(
        getApiErrorMessage(err, { 409: 'Esse username já está em uso.' }, 'Não foi possível salvar seu username.'),
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Username</SectionHeader>

      <div className="relative">
        <AtSign
          size={16}
          aria-hidden="true"
          className="absolute top-1/2 left-3 -translate-y-1/2 text-slate-500"
        />
        <input
          type="text"
          value={username}
          onChange={(event) => {
            setUsername(event.target.value)
            setSaved(false)
            if (error) setError(null)
          }}
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          placeholder="seunome"
          aria-label="Username"
          aria-invalid={availability.status === 'unavailable'}
          className={`${inputClasses} pl-9`}
        />
      </div>

      {normalized.length > 0 && !formatValid && (
        <p className="text-xs text-slate-500">3 a 20 caracteres: letras, números ou underscore.</p>
      )}
      {formatValid && changed && availability.status === 'checking' && (
        <p className="text-xs text-slate-500">Verificando…</p>
      )}
      {formatValid && changed && availability.status === 'available' && (
        <p className="flex items-center gap-1.5 text-xs text-brand-green">
          <Check size={13} aria-hidden="true" />@{normalized} está livre
        </p>
      )}
      {formatValid && changed && availability.status === 'unavailable' && (
        <p className="flex items-center gap-1.5 text-xs text-red-300">
          <X size={13} aria-hidden="true" />
          {availability.reason}
        </p>
      )}

      <button
        type="submit"
        disabled={saving || !formatValid || !changed || availability.status === 'unavailable'}
        className={saveButtonClasses}
      >
        {saving ? 'Salvando…' : 'Salvar username'}
      </button>
      {saved && <p role="status" className="text-center text-xs text-brand-green">Username atualizado.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Privacidade do perfil ---

function PrivacySection({
  visibility: initialVisibility,
  onSaved,
}: {
  visibility: ProfileVisibility
  onSaved: () => Promise<void>
}) {
  const [visibility, setVisibility] = useState(initialVisibility)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Salva assim que o usuário escolhe outra opção — igual ao tom em Preferências, mas aqui é a
  // ÚNICA coisa da seção, então um toggle que já persiste é mais direto que exigir outro clique.
  async function handleChange(next: ProfileVisibility) {
    if (next === visibility || saving) return

    setVisibility(next)
    setSaving(true)
    setError(null)
    setSaved(false)

    try {
      await updateProfileVisibility(next)
      await onSaved()
      setSaved(true)
    } catch (err) {
      setVisibility(initialVisibility)
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar a privacidade.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className={cardClasses}>
      <SectionHeader>Privacidade do perfil</SectionHeader>

      <Segmented
        label="Quem pode ver seu perfil"
        options={VISIBILITY_OPTIONS}
        labels={VISIBILITY_LABELS}
        value={visibility}
        onChange={handleChange}
      />
      <p className="text-xs text-slate-500">{VISIBILITY_HINTS[visibility]}</p>

      {saving && <p className="text-center text-xs text-slate-500">Salvando…</p>}
      {saved && <p role="status" className="text-center text-xs text-brand-green">Privacidade atualizada.</p>}
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </section>
  )
}

// --- Excluir conta ---

function DangerZone({ onDeleted }: { onDeleted: () => void }) {
  const [dialogOpen, setDialogOpen] = useState(false)

  async function handleConfirm(currentPassword: string) {
    await deleteAccount(currentPassword)
    onDeleted()
  }

  return (
    <section className="flex flex-col gap-2 rounded-md bg-surface px-5 py-4 ring-1 ring-red-500/20">
      <SectionHeader>Zona de risco</SectionHeader>
      <p className="text-sm text-slate-400">
        Excluir sua conta é irreversível: você perde acesso a todos os seus dados no Pyrra.
      </p>
      <button
        type="button"
        onClick={() => setDialogOpen(true)}
        className="flex min-h-12 w-full items-center justify-center rounded-md text-sm font-medium text-red-400 ring-1 ring-red-400/20 transition hover:bg-red-500/10"
      >
        Excluir conta
      </button>

      {dialogOpen && (
        <DeleteAccountDialog
          onConfirm={handleConfirm}
          onCancel={() => setDialogOpen(false)}
        />
      )}
    </section>
  )
}

export default Configuracoes
