import { NavLink, Outlet } from 'react-router-dom'
import {
  Apple,
  Dumbbell,
  Flame,
  ListChecks,
  User,
  Wallet,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

// Ordem = ordem na barra. Flame para "Hoje" conversa com o foguinho do streak,
// que é o que a tela mostra.
const NAV_ITEMS: NavItem[] = [
  { to: '/hoje', label: 'Hoje', icon: Flame },
  { to: '/treino', label: 'Treino', icon: Dumbbell },
  { to: '/tarefas', label: 'Tarefas', icon: ListChecks },
  { to: '/financas', label: 'Finanças', icon: Wallet },
  { to: '/nutricao', label: 'Nutrição', icon: Apple },
  { to: '/perfil', label: 'Perfil', icon: User },
]

// Casca das telas autenticadas: conteúdo rolável em cima, navegação fixa embaixo.
// Renderiza a página filha via <Outlet />, então serve como rota de layout.
export function AppLayout() {
  return (
    <div className="min-h-screen">
      {/*
        pb-28 reserva a altura da nav (~56px) mais folga: sem isso o último item
        de qualquer lista ficaria escondido atrás da barra fixa, que é o defeito
        clássico desse layout.

        max-w-md mantém a leitura confortável no desktop sem abrir mão do
        mobile-first — em 390px a largura máxima nem entra em jogo.
      */}
      <main className="mx-auto w-full max-w-md px-4 pt-6 pb-28">
        <Outlet />
      </main>

      <nav
        aria-label="Navegação principal"
        // bg semitransparente + backdrop-blur: o conteúdo que passa por baixo
        // some de forma suave em vez de cortar seco na borda da barra.
        // O padding do safe-area evita que os rótulos fiquem sob a barra de
        // gestos do iPhone.
        className="fixed inset-x-0 bottom-0 border-t border-white/10 bg-brand-dark/95 pb-[env(safe-area-inset-bottom)] backdrop-blur"
      >
        <ul className="mx-auto flex w-full max-w-md">
          {NAV_ITEMS.map(({ to, label, icon: Icon }) => (
            <li key={to} className="flex-1">
              {/*
                NavLink em vez de comparar useLocation à mão: ele já entrega o
                isActive, marca aria-current="page" sozinho (o leitor de tela
                anuncia qual aba está aberta) e casa por segmento — quando
                existir /treino/novo, "Treino" continua destacado, o que uma
                comparação `pathname === to` perderia.

                min-h-14 (56px) garante alvo de toque acima do mínimo
                recomendado; com flex-1, em 390px cada item fica com ~65px de
                largura, folgado para o polegar.
              */}
              <NavLink
                to={to}
                className={({ isActive }) =>
                  [
                    'flex min-h-14 w-full flex-col items-center justify-center gap-1 transition',
                    isActive
                      ? 'font-semibold text-brand-green'
                      : 'font-medium text-slate-400 hover:text-slate-200',
                  ].join(' ')
                }
              >
                {({ isActive }) => (
                  <>
                    {/* O traço mais grosso reforça o estado ativo para quem não
                        distingue bem a cor. */}
                    <Icon
                      size={22}
                      strokeWidth={isActive ? 2.5 : 2}
                      aria-hidden="true"
                    />
                    <span className="text-[11px] leading-none">{label}</span>
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
