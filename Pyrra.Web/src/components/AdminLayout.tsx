import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import {
  ClipboardList,
  Menu,
  MessageSquare,
  Settings,
  Shield,
  Trophy,
  User,
  UserCog,
  X,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../hooks/useAuth'
import { useChatUnread } from '../hooks/useChatUnread'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

// índice do menu admin — times e torneios são as mesmas telas do app comum, sem os itens operacionais do dia a dia (ver RequireNotAdmin)
const ADMIN_SECTIONS: NavItem[] = [
  { to: '/times', label: 'Times', icon: Shield },
  { to: '/torneios', label: 'Torneios', icon: Trophy },
  { to: '/admin/contas', label: 'Contas', icon: UserCog },
  { to: '/admin/solicitacoes', label: 'Solicitações', icon: ClipboardList },
  { to: '/admin/mensagens', label: 'Mensagens', icon: MessageSquare },
  { to: '/perfil', label: 'Perfil', icon: User },
  { to: '/configuracoes', label: 'Configurações', icon: Settings },
]

// selo "ADMIN" ao lado do wordmark, reaproveitado no header mobile, na sidebar fixa e no drawer
function AdminBadge() {
  return (
    <span className="rounded-full bg-brand-green/15 px-2 py-0.5 text-[10px] font-semibold tracking-wide text-brand-green ring-1 ring-brand-green/30">
      ADMIN
    </span>
  )
}

function SectionNav({ onNavigate }: { onNavigate?: () => void }) {
  // contagem de mensagens não lidas pro badge de "Mensagens" — mesmo provider do "Suporte" no AppLayout
  const { count: chatUnreadCount } = useChatUnread()

  return (
    <ul className="flex-1 overflow-y-auto p-3">
      {ADMIN_SECTIONS.map(({ to, label, icon: Icon }) => {
        const badge = to === '/admin/mensagens' ? chatUnreadCount : 0

        return (
          <li key={to}>
            <NavLink
              to={to}
              onClick={onNavigate}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition',
                  isActive
                    ? 'bg-surface font-medium text-ink'
                    : 'text-slate-400 hover:bg-surface hover:text-slate-200',
                ].join(' ')
              }
            >
              {({ isActive }) => (
                <>
                  <Icon
                    size={18}
                    aria-hidden="true"
                    className={isActive ? 'text-brand-green' : undefined}
                  />
                  <span className="flex-1">{label}</span>
                  {badge > 0 && (
                    <span className="rounded-full bg-brand-green px-1.5 py-0.5 text-[10px] font-semibold text-brand-dark tabular-nums">
                      {badge}
                    </span>
                  )}
                </>
              )}
            </NavLink>
          </li>
        )
      })}
    </ul>
  )
}

function AccountFooter({ name, email }: { name?: string; email?: string }) {
  return (
    <div className="flex items-center gap-3 border-t border-line px-4 py-3">
      <span
        aria-hidden="true"
        className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface text-xs font-semibold text-slate-300 ring-1 ring-line"
      >
        {name?.charAt(0).toUpperCase() ?? '?'}
      </span>
      <div className="min-w-0">
        <p className="truncate text-sm font-medium">{name ?? 'Conta'}</p>
        <p className="truncate text-xs text-slate-500">{email}</p>
      </div>
    </div>
  )
}

// layout paralelo ao AppLayout pra contas admin (ver RootLayout) — mesmo esqueleto responsivo, mas sem a barra inferior de atalhos
export function AdminLayout() {
  const { user } = useAuth()
  const [drawerOpen, setDrawerOpen] = useState(false)

  useEffect(() => {
    if (!drawerOpen) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setDrawerOpen(false)
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [drawerOpen])

  return (
    <div className="min-h-screen">
      {/* barra superior — só mobile, mesmo padrão do AppLayout */}
      <header className="sticky top-0 z-30 border-b border-line bg-brand-dark/90 backdrop-blur lg:hidden">
        <div className="mx-auto flex h-14 w-full max-w-md items-center gap-3 px-4">
          <button
            type="button"
            onClick={() => setDrawerOpen(true)}
            aria-label="Abrir menu"
            aria-expanded={drawerOpen}
            className="-ml-2 rounded-lg p-2 text-slate-300 transition hover:bg-surface hover:text-ink"
          >
            <Menu size={20} />
          </button>
          <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
          <AdminBadge />
        </div>
      </header>

      {/* sidebar permanente — só desktop */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-72 flex-col border-r border-line bg-brand-dark lg:flex">
        <div className="flex h-14 items-center gap-2 border-b border-line px-4">
          <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
          <AdminBadge />
        </div>
        <SectionNav />
        <AccountFooter name={user?.name} email={user?.email} />
      </aside>

      {/* sem barra inferior aqui, por isso pb-12 em vez de pb-24 */}
      <main className="w-full px-4 pt-5 pb-12 lg:pl-72">
        <div className="mx-auto w-full max-w-md lg:max-w-2xl">
          <Outlet />
        </div>
      </main>

      {/* drawer */}
      {drawerOpen && (
        <>
          <button
            type="button"
            aria-label="Fechar menu"
            onClick={() => setDrawerOpen(false)}
            className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm"
          />

          <nav
            aria-label="Menu de administração"
            className="fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-line bg-brand-dark"
          >
            <div className="flex h-14 items-center justify-between border-b border-line px-4">
              <div className="flex items-center gap-2">
                <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
                <AdminBadge />
              </div>
              <button
                type="button"
                onClick={() => setDrawerOpen(false)}
                aria-label="Fechar menu"
                className="-mr-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
              >
                <X size={18} />
              </button>
            </div>

            <SectionNav onNavigate={() => setDrawerOpen(false)} />

            <AccountFooter name={user?.name} email={user?.email} />
          </nav>
        </>
      )}
    </div>
  )
}

export default AdminLayout
