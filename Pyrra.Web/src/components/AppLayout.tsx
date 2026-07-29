import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import {
  Apple,
  CalendarDays,
  Dumbbell,
  Flame,
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
import { useFriendRequests } from '../hooks/useFriendRequests'
import { useTeamInvites } from '../hooks/useTeamInvites'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

// Todas as seções — índice completo do app, compartilhado pelo drawer (mobile)
// e pela sidebar permanente (desktop).
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
  // Shield (não UsersRound): a variante arredondada de Users era fácil de confundir com o ícone
  // de Amigos logo acima — Shield tem silhueta bem distinta e combina com o "emblema" de time.
  { to: '/times', label: 'Times', icon: Shield },
  { to: '/torneios', label: 'Torneios', icon: Trophy },
  { to: '/perfil', label: 'Perfil', icon: User },
  // Destino ocasional (edição de conta), não uso diário — por isso só entra aqui (menu completo),
  // não em QUICK_SECTIONS, que já tem seus 5 slots de telas de consulta diária ocupados.
  { to: '/configuracoes', label: 'Configurações', icon: Settings },
]

// Barra inferior
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

// Índice de seções, reutilizado pelo drawer e pela sidebar fixa. onNavigate
// deixa o drawer fechar ao clicar num item; a sidebar permanente não passa nada,
// pois não fecha. Manter uma cópia única evita as duas navegações divergirem.
function SectionNav({ onNavigate }: { onNavigate?: () => void }) {
  // Contagem de pedidos/convites pendentes para os badges de "Amigos" e "Times". O SectionNav
  // sempre é renderizado dentro dos dois providers (que envolvem o AppLayout), então os hooks
  // estão disponíveis.
  const { count } = useFriendRequests()
  const { count: teamInviteCount } = useTeamInvites()

  return (
    <ul className="flex-1 overflow-y-auto p-3">
      {ALL_SECTIONS.map(({ to, label, icon: Icon }) => {
        const badge = to === '/amigos' ? count : to === '/times' ? teamInviteCount : 0

        return (
          <li key={to}>
            <NavLink
              to={to}
              // Fecha ao navegar. Feito no clique, e não num efeito sobre a rota:
              // é reação a uma ação do usuário, não sincronização com sistema
              // externo. Ausente na sidebar fixa (onNavigate undefined = no-op).
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

// Rodapé com a conta — âncora de "de quem é este app". Compartilhado entre o
// drawer e a sidebar fixa.
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
  // Esc fecha, como em qualquer painel sobreposto.
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
      {/* BARRA SUPERIOR — só o gatilho do menu e o wordmark. Só no mobile: no
          desktop (lg+) a sidebar permanente ocupa seu lugar. */}
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

      {/* SIDEBAR PERMANENTE — só no desktop (lg+). Substitui hambúrguer + drawer
          + tab bar, reusando o mesmo índice de seções e o mesmo rodapé de conta.
          Fixa à esquerda; o <main> reserva o espaço dela com lg:pl-72. */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-72 flex-col border-r border-line bg-brand-dark lg:flex">
        <div className="flex h-14 items-center border-b border-line px-4">
          <span className="font-display text-lg font-semibold tracking-tight">Pyrra</span>
        </div>
        <SectionNav />
        <AccountFooter name={user?.name} email={user?.email} />
      </aside>

      {/* No desktop o conteúdo desloca para além da sidebar (lg:pl-72) e ganha
          uma coluna mais larga, porém contida (lg:max-w-2xl) e centralizada — a
          largura cheia deixaria as linhas longas demais, o "mobile esticado". */}
      <main className="w-full px-4 pt-5 pb-24 lg:pb-12 lg:pl-72">
        <div className="mx-auto w-full max-w-md lg:max-w-2xl">
          <Outlet />
        </div>
      </main>

      {/* DRAWER */}
      {drawerOpen && (
        <>
          {/* Fundo clicável: fechar tocando fora é o gesto esperado. */}
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

      {/* BARRA INFERIOR — monocromática, sem preenchimento. O item ativo muda só
          a cor do ícone e do rótulo; nada de pílula colorida atrás. Só no mobile:
          no desktop (lg+) a sidebar permanente cobre a navegação. */}
      <nav
        aria-label="Navegação rápida"
        className="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-brand-dark/95 pb-[env(safe-area-inset-bottom)] backdrop-blur lg:hidden"
      >
        {/* Cinco itens iguais, o Zelo no centro sem tratamento especial. */}
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
