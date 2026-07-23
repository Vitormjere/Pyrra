import { ChevronRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

interface PreviewCardProps {
  /** Rota da tela completa do módulo. */
  to: string
  title: string
  icon: LucideIcon
  /** Controle opcional no cabeçalho (ex.: o olho de mostrar/ocultar saldo). */
  action?: ReactNode
  children: ReactNode
}

// Cartão de prévia do dashboard: cabeçalho com módulo e ícone, corpo livre, e o
// card inteiro navegando para a tela completa.
export function PreviewCard({
  to,
  title,
  icon: Icon,
  action,
  children,
}: PreviewCardProps) {
  return (
    <section className="relative rounded-md bg-surface px-5 py-4 ring-1 ring-line transition hover:bg-surface-hi">
      <div className="flex items-center gap-2">
        <Icon size={16} className="text-slate-400" aria-hidden="true" />

        {/*
          Padrão "stretched link": o ::after do link cobre o card inteiro, então
          tocar em qualquer ponto navega — alvo generoso no celular — sem
          aninhar botão dentro de link, que é HTML inválido e confunde leitores
          de tela. O DOM continua com um link e (quando houver) um botão, irmãos.
        */}
        <h3 className="flex-1 text-sm font-medium text-slate-300">
          <Link
            to={to}
            className="rounded after:absolute after:inset-0 after:content-[''] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-green"
          >
            {title}
          </Link>
        </h3>

        {/* z-10 tira o controle de baixo da área clicável do link. */}
        {action && <div className="relative z-10">{action}</div>}

        <ChevronRight size={16} className="text-slate-500" aria-hidden="true" />
      </div>

      <div className="mt-2">{children}</div>
    </section>
  )
}

export default PreviewCard
