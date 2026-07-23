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
  Plus,
  User,
  Wallet,
  X,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../hooks/useAuth'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

// Todas as seções — este é o índice completo do app, no drawer.
const ALL_SECTIONS: NavItem[] = [
  { to: '/hoje', label: 'Hoje', icon: Flame },
  { to: '/agenda', label: 'Agenda', icon: CalendarDays },
  { to: '/treino', label: 'Treino', icon: Dumbbell },
  { to: '/tarefas', label: 'Tarefas', icon: ListChecks },
  { to: '/financas', label: 'Finanças', icon: Wallet },
  { to: '/nutricao', label: 'Nutrição', icon: Apple },
  { to: '/diario', label: 'Diário', icon: NotebookPen },
  { to: '/perfil', label: 'Perfil', icon: User },
]

// Subconjunto na barra inferior. Tarefas saiu porque a lista do dia agora vive
// na seção "Foco" da tela Hoje — mantê-la aqui daria dois caminhos para a mesma
// informação. A tela completa continua no drawer.
const QUICK_SECTIONS: NavItem[] = ALL_SECTIONS.filter((item) =>
  ['/hoje', '/financas', '/nutricao', '/perfil'].includes(item.to),
)

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
      {/* BARRA SUPERIOR — só o gatilho do menu e o wordmark. */}
      <header className="sticky top-0 z-30 border-b border-line bg-brand-dark/90 backdrop-blur">
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

      <main className="mx-auto w-full max-w-md px-4 pt-5 pb-24">
        <Outlet />
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

            <ul className="flex-1 overflow-y-auto p-3">
              {ALL_SECTIONS.map(({ to, label, icon: Icon }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    // Fecha ao navegar. Feito no clique, e não num efeito sobre
                    // a rota: é reação a uma ação do usuário, não sincronização
                    // com sistema externo.
                    onClick={() => setDrawerOpen(false)}
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
                        {label}
                      </>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>

            {/* Ainda sem ação: por ora define o lugar do atalho de criação. */}
            <div className="px-3 pb-3">
              <button
                type="button"
                className="flex w-full items-center gap-3 rounded-lg border border-dashed border-line px-3 py-2.5 text-sm text-slate-400 transition hover:border-slate-600 hover:text-slate-200"
              >
                <Plus size={18} aria-hidden="true" />
                Adicionar
              </button>
            </div>

            {/* Rodapé com a conta — âncora de "de quem é este app". */}
            <div className="flex items-center gap-3 border-t border-line px-4 py-3">
              <span
                aria-hidden="true"
                className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface text-xs font-semibold text-slate-300 ring-1 ring-line"
              >
                {user?.name.charAt(0).toUpperCase() ?? '?'}
              </span>
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">
                  {user?.name ?? 'Conta'}
                </p>
                <p className="truncate text-xs text-slate-500">{user?.email}</p>
              </div>
            </div>
          </nav>
        </>
      )}

      {/* BARRA INFERIOR — monocromática, sem preenchimento. O item ativo muda só
          a cor do ícone e do rótulo; nada de pílula colorida atrás. */}
      <nav
        aria-label="Navegação rápida"
        className="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-brand-dark/95 pb-[env(safe-area-inset-bottom)] backdrop-blur"
      >
        <ul className="mx-auto flex w-full max-w-md">
          {QUICK_SECTIONS.map(({ to, label, icon: Icon }) => (
            <li key={to} className="flex-1">
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
                      // Glow só no ativo: é o que substitui o preenchimento que
                      // a barra tinha antes, marcando a aba sem caixa colorida.
                      className={isActive ? 'glow-icon' : undefined}
                    />
                    <span className="text-[10px] tracking-wide">{label}</span>
                  </>
                )}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  )
}

export default AppLayout
