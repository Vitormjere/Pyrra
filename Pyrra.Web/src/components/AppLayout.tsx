import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import {
  Apple,
  CalendarDays,
  Dumbbell,
  Flame,
  LifeBuoy,
  ListChecks,
  Menu,
  NotebookPen,
  Settings,
  Shield,
  Sparkles,
  Trophy,
  User,
  Users,
  Wallet,
  X,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../hooks/useAuth'
import { useChatUnread } from '../hooks/useChatUnread'
import { useFriendRequests } from '../hooks/useFriendRequests'
import { useTeamInvites } from '../hooks/useTeamInvites'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

// índice completo do app, compartilhado pelo drawer (mobile) e pela sidebar permanente (desktop)
const ALL_SECTIONS: NavItem[] = [
  { to: '/hoje', label: 'Hoje', icon: Flame },
  { to: '/zelo', label: 'Zelo', icon: Sparkles },
  { to: '/agenda', label: 'Agenda', icon: CalendarDays },
  { to: '/treino', label: 'Treino', icon: Dumbbell },
  { to: '/tarefas', label: 'Tarefas', icon: ListChecks },
  { to: '/financas', label: 'Finanças', icon: Wallet },
  { to: '/nutricao', label: 'Nutrição', icon: Apple },
  { to: '/diario', label: 'Diário', icon: NotebookPen },
  { to: '/amigos', label: 'Amigos', icon: Users },
  // shield em vez de UsersRound: a variante arredondada de Users confundia com o ícone de Amigos
  { to: '/times', label: 'Times', icon: Shield },
  { to: '/torneios', label: 'Torneios', icon: Trophy },
  { to: '/perfil', label: 'Perfil', icon: User },
  // destino ocasional, não uso diário, por isso só entra no menu completo
  { to: '/suporte', label: 'Suporte', icon: LifeBuoy },
  // mesmo critério do Suporte acima: ocasional, fora dos 5 slots do QUICK_SECTIONS
  { to: '/configuracoes', label: 'Configurações', icon: Settings },
]

// barra inferior
const QUICK_ROUTES = ['/hoje', '/financas', '/zelo', '/nutricao', '/perfil']
const QUICK_SECTIONS: NavItem[] = QUICK_ROUTES.map(
  (route) => ALL_SECTIONS.find((section) => section.to === route)!,
)

function BottomNavItem({ to, label, icon: Icon }: NavItem) {
  return (
    <li className="flex-1">
      <NavLink
        to={to}
        className={({ isActive }) =>
          [
            'flex min-h-13 w-full flex-col items-center justify-center gap-1 transition',
            isActive
              ? 'text-brand-green'
              : 'text-slate-500 hover:text-slate-300',
          ].join(' ')
        }
      >
        {({ isActive }) => (
          <>
            <Icon
              size={19}
              strokeWidth={1.75}
              aria-hidden="true"
              className={isActive ? 'glow-icon' : undefined}
            />
            <span className="text-[10px] tracking-wide">{label}</span>
          </>
        )}
      </NavLink>
    </li>
  )
}

// índice de seções compartilhado entre drawer e sidebar fixa — onNavigate fecha o drawer ao clicar, a sidebar não passa nada porque não fecha
function SectionNav({ onNavigate }: { onNavigate?: () => void }) {
  // contagem de pedidos/convites/mensagens pendentes pros badges de Amigos, Times e Suporte
  const { count } = useFriendRequests()
  const { count: teamInviteCount } = useTeamInvites()
  const { count: chatUnreadCount } = useChatUnread()

  return (
    <ul className="flex-1 overflow-y-auto p-3">
      {ALL_SECTIONS.map(({ to, label, icon: Icon }) => {
        const badge =
          to === '/amigos' ? count : to === '/times' ? teamInviteCount : to === '/suporte' ? chatUnreadCount : 0

        return (
          <li key={to}>
            <NavLink
              to={to}
              // fecha ao navegar — é reação ao clique, não sincronização com a rota
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

// rodapé com a conta, compartilhado entre o drawer e a sidebar fixa
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

export function AppLayout() {
  const { user } = useAuth()
  const [drawerOpen, setDrawerOpen] = useState(false)
  // esc fecha, como em qualquer painel sobreposto
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
      {/* barra superior — só o gatilho do menu e o wordmark, só no mobile */}
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
        </div>
      </header>

      {/* sidebar permanente — só desktop, substitui hambúrguer + drawer + tab bar */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-72 flex-col border-r border-line bg-brand-dark lg:flex">
        <div className="flex h-14 items-center border-b border-line px-4">
          <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
        </div>
        <SectionNav />
        <AccountFooter name={user?.name} email={user?.email} />
      </aside>

      {/* no desktop a coluna fica mais larga mas contida, senão as linhas ficam longas demais */}
      <main className="w-full px-4 pt-5 pb-24 lg:pb-12 lg:pl-72">
        <div className="mx-auto w-full max-w-md lg:max-w-2xl">
          <Outlet />
        </div>
      </main>

      {/* drawer */}
      {drawerOpen && (
        <>
          {/* fundo clicável — fechar tocando fora é o gesto esperado */}
          <button
            type="button"
            aria-label="Fechar menu"
            onClick={() => setDrawerOpen(false)}
            className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm"
          />

          <nav
            aria-label="Todas as seções"
            className="fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-line bg-brand-dark"
          >
            <div className="flex h-14 items-center justify-between border-b border-line px-4">
              <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
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

      {/* barra inferior — monocromática, item ativo muda só a cor, sem pílula atrás; só mobile */}
      <nav
        aria-label="Navegação rápida"
        className="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-brand-dark/95 pb-[env(safe-area-inset-bottom)] backdrop-blur lg:hidden"
      >
        {/* cinco itens iguais, o Zelo no centro sem tratamento especial */}
        <ul className="mx-auto flex w-full max-w-md">
          {QUICK_SECTIONS.map((item) => (
            <BottomNavItem key={item.to} {...item} />
          ))}
        </ul>
      </nav>
    </div>
  )
}

export default AppLayout
