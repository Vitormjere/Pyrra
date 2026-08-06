import { useCallback, useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Search, ShieldPlus, Trash2, UserCog } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'
import Segmented from '../../../components/Segmented'
import Skeleton from '../../../components/Skeleton'
import { useConfirm } from '../../../hooks/useConfirm'
import { createAdminAccount, deleteUser, getAllUsers } from '../../../services/adminUserService'
import { getApiErrorMessage } from '../../../services/apiError'
import type { AdminUser } from '../../../types/admin'

const listClasses = 'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

const ACCOUNT_TABS = ['ativos', 'excluidos'] as const
type AccountTab = (typeof ACCOUNT_TABS)[number]

/** "22/07/2026" — createdAt/deletedAt vêm como DateTime ISO completo, não DateOnly. */
function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR')
}

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

function UserRow({
  user,
  busy,
  onDelete,
}: {
  user: AdminUser
  busy: boolean
  onDelete: () => void
}) {
  const excluded = user.deletedAt !== null

  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <Avatar name={user.name} />
      <div className="min-w-0 flex-1">
        <p className="flex flex-wrap items-center gap-1.5 truncate font-medium text-ink">
          {user.name}
          {user.isAdmin && (
            <span className="rounded-full bg-brand-green/15 px-1.5 py-0.5 text-[10px] font-semibold tracking-wide text-brand-green ring-1 ring-brand-green/30">
              ADMIN
            </span>
          )}
          {excluded && (
            <span className="rounded-full bg-red-500/10 px-1.5 py-0.5 text-[10px] font-semibold tracking-wide text-red-300 ring-1 ring-red-500/20">
              EXCLUÍDA
            </span>
          )}
        </p>
        <p className="truncate text-xs text-slate-500">
          {user.email}
          {user.username && ` — @${user.username}`}
        </p>
      </div>
      <span className="shrink-0 text-xs text-slate-500 tabular-nums">{formatDateTime(user.createdAt)}</span>
      {!user.isAdmin && !excluded && (
        <button
          type="button"
          disabled={busy}
          onClick={onDelete}
          aria-label={`Excluir conta de ${user.name}`}
          className="shrink-0 rounded-xl p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
        >
          <Trash2 size={15} />
        </button>
      )}
    </li>
  )
}

// cria novas contas admin e lista/exclui contas de jogadores
export function AdminContas() {
  const { confirm, dialog } = useConfirm()

  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [tab, setTab] = useState<AccountTab>('ativos')

  const [newEmail, setNewEmail] = useState('')
  const [newName, setNewName] = useState('')
  const [newUsername, setNewUsername] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [createBusy, setCreateBusy] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [createSuccess, setCreateSuccess] = useState(false)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getAllUsers()
        if (!active) return
        setUsers(data)
      } catch (err) {
        if (!active) return
        setError(getApiErrorMessage(err, {}, 'Não foi possível carregar os usuários.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  const loadUsers = useCallback(async () => {
    try {
      setUsers(await getAllUsers())
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível recarregar os usuários.'))
    }
  }, [])

  async function handleCreateAdmin(event: FormEvent) {
    event.preventDefault()

    if (!newName.trim() || !newEmail.trim() || !newUsername.trim() || !newPassword) {
      setCreateError('Preencha todos os campos.')
      return
    }

    setCreateBusy(true)
    setCreateError(null)
    setCreateSuccess(false)
    try {
      await createAdminAccount({
        email: newEmail.trim(),
        name: newName.trim(),
        username: newUsername.trim(),
        password: newPassword,
      })
      setNewEmail('')
      setNewName('')
      setNewUsername('')
      setNewPassword('')
      setCreateSuccess(true)
      await loadUsers()
    } catch (err) {
      setCreateError(
        getApiErrorMessage(
          err,
          {
            409: 'Esse e-mail ou username já está em uso.',
          },
          'Não foi possível criar a conta.',
        ),
      )
    } finally {
      setCreateBusy(false)
    }
  }

  async function handleDelete(user: AdminUser) {
    const ok = await confirm({
      title: 'Excluir conta',
      message: `Excluir a conta de ${user.name} (${user.email})? Essa ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      destructive: true,
    })
    if (!ok) return

    setBusyId(user.id)
    setError(null)
    try {
      await deleteUser(user.id)
      await loadUsers()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível excluir a conta.'))
    } finally {
      setBusyId(null)
    }
  }

  // busca continua igual, some as duas abas — só a exibição é que separa por deletedAt
  const searchedUsers = useMemo(() => {
    if (!users) return null
    const term = search.trim().toLowerCase()
    if (!term) return users
    return users.filter(
      (u) =>
        u.name.toLowerCase().includes(term) ||
        u.email.toLowerCase().includes(term) ||
        (u.username?.toLowerCase().includes(term) ?? false),
    )
  }, [users, search])

  const activeUsers = useMemo(() => searchedUsers?.filter((u) => u.deletedAt === null) ?? null, [searchedUsers])
  const deletedUsers = useMemo(() => searchedUsers?.filter((u) => u.deletedAt !== null) ?? null, [searchedUsers])
  const filteredUsers = tab === 'ativos' ? activeUsers : deletedUsers

  // contagem no rótulo da aba reflete a busca atual, não o total geral
  const tabLabels = {
    ativos: `Ativos${activeUsers ? ` (${activeUsers.length})` : ''}`,
    excluidos: `Excluídos${deletedUsers ? ` (${deletedUsers.length})` : ''}`,
  }

  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Contas
      </h1>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* CRIAR CONTA ADMIN */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <ShieldPlus size={15} className="text-brand-green" aria-hidden="true" />
          Criar conta admin
        </h2>
        <form onSubmit={handleCreateAdmin} className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line">
          <div className="flex flex-col gap-1">
            <label htmlFor="new-admin-name" className={labelClasses}>
              Nome
            </label>
            <input
              id="new-admin-name"
              type="text"
              value={newName}
              onChange={(event) => setNewName(event.target.value)}
              placeholder="Ex.: Maria Souza"
              className={inputClasses}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="new-admin-email" className={labelClasses}>
              E-mail
            </label>
            <input
              id="new-admin-email"
              type="email"
              value={newEmail}
              onChange={(event) => setNewEmail(event.target.value)}
              placeholder="maria@pyrra.com.br"
              autoCapitalize="none"
              spellCheck={false}
              className={inputClasses}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="new-admin-username" className={labelClasses}>
              Username
            </label>
            <input
              id="new-admin-username"
              type="text"
              value={newUsername}
              onChange={(event) => setNewUsername(event.target.value)}
              placeholder="mariasouza"
              autoCapitalize="none"
              spellCheck={false}
              className={inputClasses}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="new-admin-password" className={labelClasses}>
              Senha
            </label>
            <input
              id="new-admin-password"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="Mínimo 8 caracteres"
              autoComplete="new-password"
              className={inputClasses}
            />
            <p className="text-xs text-slate-500">
              A senha vai direto por HTTPS até o backend, que a transforma em hash antes de guardar
              — nunca fica salva como digitada. Combine-a com a nova pessoa admin por um canal
              separado (não fica registrada em lugar nenhum depois de criada).
            </p>
          </div>
          {createError && (
            <p role="alert" className="text-xs text-red-300">
              {createError}
            </p>
          )}
          {createSuccess && (
            <p className="text-xs font-medium text-brand-green">Conta admin criada com sucesso.</p>
          )}
          <button
            type="submit"
            disabled={createBusy}
            className="mt-1 inline-flex items-center justify-center gap-1.5 self-start rounded-xl bg-brand-green px-4 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:opacity-60"
          >
            <ShieldPlus size={15} aria-hidden="true" />
            {createBusy ? 'Criando…' : 'Criar conta admin'}
          </button>
        </form>
      </section>

      {/* LISTA DE USUÁRIOS */}
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-medium text-slate-300">
          <UserCog size={15} className="text-brand-green" aria-hidden="true" />
          Usuários
        </h2>

        <div className="relative">
          <Search
            size={16}
            aria-hidden="true"
            className="absolute top-1/2 left-3 -translate-y-1/2 text-slate-500"
          />
          <input
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar por nome, e-mail ou username"
            aria-label="Buscar usuários"
            autoCapitalize="none"
            spellCheck={false}
            className={`${inputClasses} pl-9`}
          />
        </div>

        <Segmented label="Usuários ativos ou excluídos" options={ACCOUNT_TABS} labels={tabLabels} value={tab} onChange={setTab} />

        {filteredUsers === null ? (
          <Skeleton className="h-16" />
        ) : filteredUsers.length === 0 ? (
          <EmptyState
            icon={UserCog}
            title={tab === 'ativos' ? 'Nenhum usuário ativo encontrado.' : 'Nenhuma conta excluída encontrada.'}
            description={search.trim() ? 'Tente buscar por outro termo.' : undefined}
          />
        ) : (
          <ul className={listClasses}>
            {filteredUsers.map((user) => (
              <UserRow
                key={user.id}
                user={user}
                busy={busyId === user.id}
                onDelete={() => handleDelete(user)}
              />
            ))}
          </ul>
        )}
      </section>

      {dialog}
    </div>
  )
}

export default AdminContas
