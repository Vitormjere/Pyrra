import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent, ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AtSign, Check, ChevronLeft, ImagePlus, LogOut, Pencil, X } from 'lucide-react'
import Avatar from '../../components/Avatar'
import Segmented from '../../components/Segmented'
import SectionHeader from '../../components/SectionHeader'
import DeleteAccountDialog from '../../components/DeleteAccountDialog'
import PasswordInput from '../../components/PasswordInput'
import { useAuth } from '../../hooks/useAuth'
import {
  changeEmail,
  changePassword,
  checkUsernameAvailability,
  deleteAccount,
  removeProfilePicture,
  setUsername as setUsernameApi,
  updateName,
  updatePreferences,
  updateProfileVisibility,
  updateTimezone,
  uploadProfilePicture,
} from '../../services/userService'
import { getApiErrorMessage } from '../../services/apiError'
import { TIMEZONE_OPTIONS } from '../../utils/timezones'
import type { CommunicationTone, ProfileVisibility } from '../../types/auth'

// Mesma regra do backend (UserAccountService.SetProfilePictureAsync) — só pra dar feedback
// antes de enviar; o backend segue sendo a validação de verdade.
const MAX_PICTURE_BYTES = 3 * 1024 * 1024
const ACCEPTED_PICTURE_TYPES = 'image/jpeg,image/png,image/webp'

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

// espelha os textos do Onboarding, pro significado de cada tom ser o mesmo nos dois lugares
const TONE_HINTS: Record<CommunicationTone, string> = {
  Direto: 'Direto ao ponto, sem rodeios.',
  Acolhedor: 'Gentil, no seu ritmo.',
  Desafiador: 'Provoca pra te tirar da inércia.',
}

// mesma regra do backend (UsernameService): 3-20, minúsculas, números e underscore
const USERNAME_PATTERN = /^[a-z0-9_]{3,20}$/

function normalizeUsername(raw: string): string {
  return raw.trim().replace(/^@+/, '').toLowerCase()
}

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const cardClasses = 'flex flex-col gap-3 rounded-md bg-surface px-5 py-4 ring-1 ring-line'

const saveButtonClasses =
  'flex-1 rounded-xl bg-brand-green px-4 py-2.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60'

const cancelButtonClasses =
  'rounded-xl px-4 py-2.5 text-sm font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi'

type UsernameAvailability =
  | { status: 'idle' }
  | { status: 'checking' }
  | { status: 'available' }
  | { status: 'unavailable'; reason: string }

// linha compacta de modo leitura: valor atual + lápis. Clicar em qualquer ponto
// (texto ou ícone) abre o formulário de edição — mesmo alvo de clique, sem
// distinção, porque não há motivo pro usuário mirar só no lápis.
function FieldPreview({ value, onEdit }: { value: ReactNode; onEdit: () => void }) {
  return (
    <button
      type="button"
      onClick={onEdit}
      className="flex w-full items-center justify-between gap-3 rounded-md py-1 text-left transition hover:opacity-80"
    >
      <span className="min-w-0 truncate text-sm text-ink">{value}</span>
      <Pencil size={15} aria-hidden="true" className="shrink-0 text-slate-500" />
    </button>
  )
}

// edição/administração da conta (separado do Perfil, que é só leitura) — cada seção tem seu próprio form e save, independentes entre si
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

      <ProfilePictureSection
        name={user.name}
        imageUrl={user.profilePictureUrl}
        onSaved={applyUser}
      />
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

// --- Foto de perfil ---

// Sem modo leitura/edição (mesmo raciocínio da Privacidade): não há "rascunho" de uma foto pra
// esconder, os botões de enviar/remover já ficam sempre visíveis, mesmo padrão do Banner de time.
function ProfilePictureSection({
  name,
  imageUrl,
  onSaved,
}: {
  name: string
  imageUrl: string | null
  onSaved: (user: Awaited<ReturnType<typeof uploadProfilePicture>>) => void
}) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = '' // permite escolher o mesmo arquivo de novo depois de um erro
    if (!file) return

    if (file.size > MAX_PICTURE_BYTES) {
      setError('A imagem deve ter até 3MB.')
      return
    }

    setError(null)
    setBusy(true)
    uploadProfilePicture(file)
      .then(onSaved)
      .catch((err) => setError(getApiErrorMessage(err, {}, 'Não foi possível enviar a imagem.')))
      .finally(() => setBusy(false))
  }

  async function handleRemove() {
    setError(null)
    setBusy(true)
    try {
      const updated = await removeProfilePicture()
      onSaved(updated)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover a imagem.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className={cardClasses}>
      <SectionHeader>Foto de perfil</SectionHeader>
      <div className="flex items-center gap-4">
        <Avatar name={name} imageUrl={imageUrl} size="profile" />
        <div className="flex flex-1 flex-col gap-2">
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={() => fileInputRef.current?.click()}
              className="inline-flex shrink-0 items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-medium text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
            >
              <ImagePlus size={15} aria-hidden="true" />
              {busy ? 'Enviando…' : imageUrl ? 'Trocar foto' : 'Enviar foto'}
            </button>
            {imageUrl && (
              <button
                type="button"
                disabled={busy}
                onClick={handleRemove}
                className="inline-flex shrink-0 items-center gap-1.5 rounded-xl p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                aria-label="Remover foto de perfil"
              >
                <X size={15} />
              </button>
            )}
            <input
              ref={fileInputRef}
              type="file"
              accept={ACCEPTED_PICTURE_TYPES}
              onChange={handleFileChange}
              className="hidden"
            />
          </div>
          <p className="text-xs text-slate-400">
            Opcional — JPG, PNG ou WEBP, até 3MB. Sem foto, mostra sua inicial.
          </p>
          {error && <p role="alert" className="text-xs text-red-300">{error}</p>}
        </div>
      </div>
    </div>
  )
}

// --- Nome ---

function NameSection({ name, onSaved }: { name: string; onSaved: () => Promise<void> }) {
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState(name)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const changed = value.trim() !== name && value.trim().length > 0

  function startEditing() {
    setValue(name)
    setError(null)
    setEditing(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!changed || saving) return

    setSaving(true)
    setError(null)
    try {
      await updateName(value.trim())
      await onSaved()
      setEditing(false)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar seu nome.'))
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <div className={cardClasses}>
        <SectionHeader>Nome</SectionHeader>
        <FieldPreview value={name} onEdit={startEditing} />
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Nome</SectionHeader>
      <input
        type="text"
        value={value}
        onChange={(event) => setValue(event.target.value)}
        maxLength={100}
        aria-label="Nome"
        autoFocus
        className={inputClasses}
      />
      <div className="flex gap-2">
        <button type="submit" disabled={!changed || saving} className={saveButtonClasses}>
          {saving ? 'Salvando…' : 'Salvar nome'}
        </button>
        <button type="button" onClick={() => setEditing(false)} className={cancelButtonClasses}>
          Cancelar
        </button>
      </div>
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- E-mail ---

function EmailSection({ email, onSaved }: { email: string; onSaved: () => Promise<void> }) {
  const [editing, setEditing] = useState(false)
  const [newEmail, setNewEmail] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const canSubmit = newEmail.trim().length > 0 && currentPassword.length > 0

  function startEditing() {
    setNewEmail('')
    setCurrentPassword('')
    setError(null)
    setEditing(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      await changeEmail(newEmail.trim(), currentPassword)
      await onSaved()
      setEditing(false)
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

  if (!editing) {
    return (
      <div className={cardClasses}>
        <SectionHeader>E-mail</SectionHeader>
        <FieldPreview value={email} onEdit={startEditing} />
      </div>
    )
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
          onChange={(event) => setNewEmail(event.target.value)}
          autoComplete="email"
          autoFocus
          placeholder="novo@exemplo.com"
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="senha-email" className="text-xs font-medium text-slate-400">
          Senha atual (para confirmar)
        </label>
        <PasswordInput
          id="senha-email"
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
          autoComplete="current-password"
        />
      </div>

      <div className="flex gap-2">
        <button type="submit" disabled={!canSubmit || saving} className={saveButtonClasses}>
          {saving ? 'Salvando…' : 'Trocar e-mail'}
        </button>
        <button type="button" onClick={() => setEditing(false)} className={cancelButtonClasses}>
          Cancelar
        </button>
      </div>
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Senha ---

function PasswordSection() {
  const [editing, setEditing] = useState(false)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const newPasswordValid = newPassword.length >= 8
  // confirmação é só client-side, mesmo critério do Cadastro
  const confirmMatches = confirmPassword.length > 0 && confirmPassword === newPassword
  const canSubmit = currentPassword.length > 0 && newPasswordValid && confirmMatches

  function startEditing() {
    setCurrentPassword('')
    setNewPassword('')
    setConfirmPassword('')
    setError(null)
    setEditing(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      await changePassword(currentPassword, newPassword)
      setEditing(false)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível trocar sua senha.'))
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <div className={cardClasses}>
        <SectionHeader>Senha</SectionHeader>
        {/* nada real pra mostrar aqui — os pontos só sinalizam "existe uma senha", não o valor */}
        <FieldPreview value="••••••••" onEdit={startEditing} />
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className={cardClasses}>
      <SectionHeader>Senha</SectionHeader>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="senha-atual" className="text-xs font-medium text-slate-400">
          Senha atual
        </label>
        <PasswordInput
          id="senha-atual"
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
          autoComplete="current-password"
          autoFocus
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="nova-senha" className="text-xs font-medium text-slate-400">
          Nova senha
        </label>
        <PasswordInput
          id="nova-senha"
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
          autoComplete="new-password"
        />
        <p className="text-xs text-slate-500">Mínimo de 8 caracteres.</p>
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="confirmar-senha" className="text-xs font-medium text-slate-400">
          Confirmar nova senha
        </label>
        <PasswordInput
          id="confirmar-senha"
          value={confirmPassword}
          onChange={(event) => setConfirmPassword(event.target.value)}
          autoComplete="new-password"
        />
        {confirmPassword.length > 0 && !confirmMatches && (
          <p className="text-xs text-red-300">As senhas não coincidem.</p>
        )}
      </div>

      <div className="flex gap-2">
        <button type="submit" disabled={!canSubmit || saving} className={saveButtonClasses}>
          {saving ? 'Salvando…' : 'Trocar senha'}
        </button>
        <button type="button" onClick={() => setEditing(false)} className={cancelButtonClasses}>
          Cancelar
        </button>
      </div>
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
  const [editing, setEditing] = useState(false)
  const [tone, setTone] = useState(initialTone)
  const [notificationTime, setNotificationTime] = useState(initialTime)
  const [timezone, setTimezone] = useState(initialTimezone)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function startEditing() {
    setTone(initialTone)
    setNotificationTime(initialTime)
    setTimezone(initialTimezone)
    setError(null)
    setEditing(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      // dois endpoints no backend (tom/horário e fuso são desacoplados), mas uma única ação de salvar aqui
      await Promise.all([
        updatePreferences(tone, notificationTime),
        updateTimezone(timezone),
      ])
      await onSaved()
      setEditing(false)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar suas preferências.'))
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    const timezoneLabel =
      TIMEZONE_OPTIONS.find((option) => option.value === initialTimezone)?.label ?? initialTimezone

    return (
      <div className={cardClasses}>
        <SectionHeader>Preferências</SectionHeader>
        <FieldPreview
          value={`${initialTone} · ${initialTime} · ${timezoneLabel}`}
          onEdit={startEditing}
        />
      </div>
    )
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
          onChange={setTone}
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
          onChange={(event) => setNotificationTime(event.target.value)}
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
          onChange={(event) => setTimezone(event.target.value)}
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

      <div className="flex gap-2">
        <button type="submit" disabled={saving} className={saveButtonClasses}>
          {saving ? 'Salvando…' : 'Salvar preferências'}
        </button>
        <button type="button" onClick={() => setEditing(false)} className={cancelButtonClasses}>
          Cancelar
        </button>
      </div>
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
  const [editing, setEditing] = useState(false)
  const [username, setUsername] = useState(currentUsername ?? '')
  const [availability, setAvailability] = useState<UsernameAvailability>({ status: 'idle' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const normalized = normalizeUsername(username)
  const formatValid = USERNAME_PATTERN.test(normalized)
  const changed = normalized !== currentUsername

  // checagem de disponibilidade com debounce, só quando o formato é válido, mudou do atual
  // e a seção está mesmo aberta — sem o guard de `editing`, o timer seguiria rodando escondido
  useEffect(() => {
    if (!editing || !formatValid || !changed) {
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
  }, [editing, normalized, formatValid, changed])

  function startEditing() {
    setUsername(currentUsername ?? '')
    setAvailability({ status: 'idle' })
    setError(null)
    setEditing(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!formatValid || !changed || saving) return

    setSaving(true)
    setError(null)
    try {
      const updated = await setUsernameApi(normalized)
      onSaved(updated)
      setEditing(false)
    } catch (err) {
      setError(
        getApiErrorMessage(err, { 409: 'Esse username já está em uso.' }, 'Não foi possível salvar seu username.'),
      )
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <div className={cardClasses}>
        <SectionHeader>Username</SectionHeader>
        <FieldPreview
          value={currentUsername ? `@${currentUsername}` : 'Não definido'}
          onEdit={startEditing}
        />
      </div>
    )
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
            if (error) setError(null)
          }}
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          autoFocus
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

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={saving || !formatValid || !changed || availability.status === 'unavailable'}
          className={saveButtonClasses}
        >
          {saving ? 'Salvando…' : 'Salvar username'}
        </button>
        <button type="button" onClick={() => setEditing(false)} className={cancelButtonClasses}>
          Cancelar
        </button>
      </div>
      {error && <p role="alert" className="text-center text-xs text-red-300">{error}</p>}
    </form>
  )
}

// --- Privacidade do perfil ---

// Fica sempre visível (sem modo leitura/edição): é um seletor de opção única que
// já mostra o valor atual destacado e salva no clique, mesma lógica de um toggle
// — não tem "rascunho" pra esconder, então o padrão de campo de texto não se aplica.
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

  // salva assim que o usuário escolhe outra opção, igual ao tom em Preferências — é a única coisa da seção
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
